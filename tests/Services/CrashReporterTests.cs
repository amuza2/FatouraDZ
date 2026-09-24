using System;
using System.IO;
using FatouraDZ.Services;
using Moq;

namespace FatouraDZ.Tests.Services;

/// <summary>
/// Les rapports de plantage sont la seule trace qu'un utilisateur peut
/// transmettre : ils doivent être écrits, lisibles, bornés en nombre, et ne
/// jamais échouer bruyamment.
/// </summary>
public class CrashReporterTests : IDisposable
{
    private readonly string _dossier;
    private readonly Mock<IAppLogger> _logger = new();

    public CrashReporterTests()
    {
        _dossier = Path.Combine(Path.GetTempPath(), $"fatouradz_crash_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dossier))
                Directory.Delete(_dossier, recursive: true);
        }
        catch
        {
            // Nettoyage au mieux.
        }
    }

    private CrashReporter Creer(int nombreMax = 20) =>
        new(_logger.Object) { DossierRapports = _dossier, NombreMaxRapports = nombreMax };

    [Fact]
    public void Enregistrer_EcritUnRapportExploitable()
    {
        var reporter = Creer();

        // L'exception doit avoir été levée pour porter une pile d'appels : c'est
        // précisément ce qu'on veut retrouver dans le rapport.
        Exception exception;
        try
        {
            throw new InvalidOperationException("Le calcul du timbre a échoué");
        }
        catch (Exception levee)
        {
            exception = levee;
        }

        // Act
        var rapport = reporter.Enregistrer(exception, "Exception UI non gérée");

        // Assert
        Assert.NotNull(rapport);
        Assert.True(File.Exists(rapport!.CheminFichier));

        var contenu = File.ReadAllText(rapport.CheminFichier);
        Assert.Contains("rapport de plantage", contenu, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(AppInfo.Version, contenu);
        Assert.Contains("Exception UI non gérée", contenu);
        Assert.Contains("InvalidOperationException", contenu);
        Assert.Contains("Le calcul du timbre a échoué", contenu);
        // Sans pile d'appels, le rapport ne sert à rien.
        Assert.Contains("CrashReporterTests", contenu);
    }

    [Fact]
    public void Enregistrer_InclutLesExceptionsInternes()
    {
        var reporter = Creer();
        var exception = new InvalidOperationException(
            "Erreur d'enregistrement",
            new IOException("Base de données verrouillée"));

        var rapport = reporter.Enregistrer(exception, "Test");

        Assert.NotNull(rapport);
        var contenu = File.ReadAllText(rapport!.CheminFichier);
        Assert.Contains("Base de données verrouillée", contenu);
    }

    [Fact]
    public void Enregistrer_SansException_EcritQuandMemeUnRapport()
    {
        var reporter = Creer();

        var rapport = reporter.Enregistrer(null, "Arrêt inattendu");

        Assert.NotNull(rapport);
        Assert.Equal("Exception inconnue", rapport!.Resume);
        Assert.True(File.Exists(rapport.CheminFichier));
    }

    [Fact]
    public void Enregistrer_OrigineAvecEspacesEtAccents_ProduitUnNomDeFichierValide()
    {
        var reporter = Creer();

        var rapport = reporter.Enregistrer(new Exception("Test"), "Exception UI non gérée / démarrage");

        Assert.NotNull(rapport);
        var nom = Path.GetFileName(rapport!.CheminFichier);
        Assert.StartsWith("crash-", nom);
        Assert.EndsWith(".txt", nom);
        Assert.DoesNotContain(" ", nom);
        Assert.DoesNotContain("/", nom);
        Assert.True(File.Exists(rapport.CheminFichier));
    }

    [Fact]
    public void Enregistrer_LimiteLeNombreDeRapportsConserves()
    {
        var reporter = Creer(nombreMax: 3);

        for (var i = 0; i < 6; i++)
        {
            // Horodatage explicite : les noms doivent rester ordonnables même si
            // plusieurs rapports tombent dans la même milliseconde.
            reporter.Enregistrer(new Exception($"Panne {i}"), $"Origine {i}");
        }

        var fichiers = Directory.GetFiles(_dossier, "crash-*.txt");
        Assert.Equal(3, fichiers.Length);
    }

    [Fact]
    public void Enregistrer_NeLeveJamaisMemeSiLeDossierEstImpossibleAEcrire()
    {
        // Un chemin dont le parent est un fichier ne peut pas être créé.
        var fichier = Path.Combine(_dossier, "bloquant");
        Directory.CreateDirectory(_dossier);
        File.WriteAllText(fichier, "x");

        var reporter = new CrashReporter(_logger.Object)
        {
            DossierRapports = Path.Combine(fichier, "crashes")
        };

        var rapport = reporter.Enregistrer(new Exception("Test"), "Test");

        Assert.Null(rapport);
    }

    [Fact]
    public void Enregistrer_NotifieLInterface()
    {
        var reporter = Creer();
        RapportCrash? notifie = null;
        reporter.RapportEnregistre += r => notifie = r;

        var rapport = reporter.Enregistrer(new Exception("Test"), "Test");

        Assert.NotNull(notifie);
        Assert.Equal(rapport!.CheminFichier, notifie!.CheminFichier);
    }

    [Fact]
    public void Enregistrer_JournaliseLEchec()
    {
        var reporter = Creer();

        reporter.Enregistrer(new Exception("Test"), "Test");

        _logger.Verify(l => l.Error(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Resume_FormateLeMessage()
    {
        Assert.Equal("InvalidOperationException : Message court",
            CrashReporter.Resume(new InvalidOperationException("Message court")));

        // Un message vide reste un message : le type suffit à orienter le diagnostic.
        Assert.Equal("InvalidOperationException : ",
            CrashReporter.Resume(new InvalidOperationException(string.Empty)));
    }

    [Fact]
    public void Resume_TronqueEtAplatitLesMessagesLongs()
    {
        var exception = new Exception(new string('a', 500) + "\nligne 2");

        var resume = CrashReporter.Resume(exception);

        Assert.True(resume.Length <= 200);
        Assert.EndsWith("…", resume);
        Assert.DoesNotContain("\n", resume);
    }

    [Fact]
    public void Abreger_RemplaceLeDossierPersonnel()
    {
        var personnel = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var chemin = Path.Combine(personnel, "Documents", "facture.pdf");

        var abrégé = AppPaths.Abreger(chemin);

        Assert.StartsWith("~", abrégé);
        Assert.DoesNotContain(personnel, abrégé);
    }

    [Fact]
    public void Abreger_LaisseLesAutresCheminsIntacts()
    {
        Assert.Equal("/etc/hosts", AppPaths.Abreger("/etc/hosts"));
    }

    [Fact]
    public void AbregerDansLeTexte_RemplaceToutesLesOccurrences()
    {
        var personnel = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var texte = $"Sauvegarde : {personnel}/FatouraDZ/fatouradz.db puis {personnel}/Bureau/facture.pdf";

        var abrege = AppPaths.AbregerDansLeTexte(texte);

        Assert.DoesNotContain(personnel, abrege);
        Assert.Contains("~/FatouraDZ/fatouradz.db", abrege);
        Assert.Contains("~/Bureau/facture.pdf", abrege);
    }

    [Fact]
    public void AbregerDansLeTexte_LaisseUnTexteSansCheminIntact()
    {
        Assert.Equal("Échec de l'aperçu PDF", AppPaths.AbregerDansLeTexte("Échec de l'aperçu PDF"));
    }
}
