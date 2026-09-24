using System;
using System.IO;
using System.Linq;
using FatouraDZ.Services;

namespace FatouraDZ.Tests.Services;

/// <summary>
/// Le journal est ce qui rend un rapport de plantage exploitable : il doit écrire
/// sans jamais faire échouer l'application, et ne pas s'accumuler sans limite.
/// </summary>
public class FileLoggerTests : IDisposable
{
    private readonly string _dossier;

    public FileLoggerTests()
    {
        _dossier = Path.Combine(Path.GetTempPath(), $"fatouradz_logs_{Guid.NewGuid():N}");
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

    private FileLogger Creer(int joursConserves = FileLogger.JoursConservesParDefaut) =>
        new() { DossierLogs = _dossier, JoursConserves = joursConserves };

    private string FichierDuJour => Path.Combine(_dossier, $"fatouradz-{DateTime.Now:yyyyMMdd}.log");

    [Fact]
    public void Info_EcritDansLeJournalDuJour()
    {
        var logger = Creer();

        logger.Info("Base de données initialisée");

        Assert.True(File.Exists(FichierDuJour));
        var contenu = File.ReadAllText(FichierDuJour);
        Assert.Contains("[INFO]", contenu);
        Assert.Contains("Base de données initialisée", contenu);
    }

    [Theory]
    [InlineData("INFO")]
    [InlineData("WARN")]
    [InlineData("ERROR")]
    public void ChaqueNiveau_EstTraceAvecSonNom(string niveau)
    {
        var logger = Creer();

        switch (niveau)
        {
            case "INFO": logger.Info("message"); break;
            case "WARN": logger.Warning("message"); break;
            default: logger.Error("message"); break;
        }

        Assert.Contains($"[{niveau}]", File.ReadAllText(FichierDuJour));
    }

    [Fact]
    public void UneException_EstJointeAuMessage()
    {
        var logger = Creer();

        logger.Error("Échec de l'export", new InvalidOperationException("disque plein"));

        var contenu = File.ReadAllText(FichierDuJour);
        Assert.Contains("Échec de l'export", contenu);
        Assert.Contains("disque plein", contenu);
    }

    [Fact]
    public void LesMessages_SontAjoutesLesUnsALaSuiteDesAutres()
    {
        var logger = Creer();

        logger.Info("premier");
        logger.Info("deuxième");

        var lignes = File.ReadAllLines(FichierDuJour).Where(l => l.Length > 0).ToList();
        Assert.Equal(2, lignes.Count);
        Assert.Contains("premier", lignes[0]);
        Assert.Contains("deuxième", lignes[1]);
    }

    [Fact]
    public void LesCheminsDuDossierPersonnel_SontAbreges()
    {
        var logger = Creer();
        var personnel = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        logger.Error($"Impossible d'ouvrir {personnel}/Documents/facture.db");

        var contenu = File.ReadAllText(FichierDuJour);
        Assert.DoesNotContain(personnel, contenu);
        Assert.Contains("~/Documents/facture.db", contenu);
    }

    [Fact]
    public void NeLeveJamaisQuandLeDossierEstImpossibleAEcrire()
    {
        // Un fichier à la place du dossier : CreateDirectory échoue, et la
        // journalisation doit rester silencieuse.
        var fichier = Path.Combine(_dossier, "bloquant");
        Directory.CreateDirectory(_dossier);
        File.WriteAllText(fichier, "x");

        var logger = new FileLogger { DossierLogs = Path.Combine(fichier, "logs") };

        logger.Error("ne doit pas lever", new Exception("test"));
    }

    [Fact]
    public void LesJournauxAnciens_SontPurges()
    {
        Directory.CreateDirectory(_dossier);

        // 40 jours d'historique, dont un fichier beaucoup plus vieux que les autres.
        for (var jour = 0; jour < 40; jour++)
        {
            var date = DateTime.Now.Date.AddDays(-jour).ToString("yyyyMMdd");
            File.WriteAllText(Path.Combine(_dossier, $"fatouradz-{date}.log"), "ancien");
        }

        var logger = Creer(joursConserves: 10);

        // Une seule écriture suffit : la purge a lieu au premier message de la session.
        logger.Info("réveil de l'application");

        var restants = Directory.GetFiles(_dossier, "fatouradz-*.log")
            .Select(Path.GetFileName)
            .ToList();

        Assert.Equal(10, restants.Count);
        // Le journal du jour doit survivre à la purge.
        Assert.Contains(Path.GetFileName(FichierDuJour), restants);
        // Les plus anciens doivent avoir disparu.
        Assert.DoesNotContain($"fatouradz-{DateTime.Now.AddDays(-39):yyyyMMdd}.log", restants);
    }

    [Fact]
    public void LaPurge_NeSupprimePasLeJournalDuJour()
    {
        Directory.CreateDirectory(_dossier);
        var logger = Creer(joursConserves: 1);

        logger.Info("démarrage");

        Assert.True(File.Exists(FichierDuJour));
    }
}
