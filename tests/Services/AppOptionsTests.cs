using System;
using System.IO;
using FatouraDZ.Services;

namespace FatouraDZ.Tests.Services;

/// <summary>
/// Options de démarrage : le mode verbeux doit s'activer de façon prévisible, et
/// les arguments destinés à la couche graphique ne doivent pas être avalés.
/// </summary>
public class AppOptionsTests
{
    [Theory]
    [InlineData("--verbose")]
    [InlineData("-v")]
    [InlineData("--VERBOSE")]
    [InlineData("/verbose")]
    [InlineData("verbose")]
    public void Verbeux_EstReconnuQuelleQueSoitLaForme(string argument)
    {
        var options = AppOptions.Analyser(new[] { argument });

        Assert.True(options.Verbeux);
    }

    [Fact]
    public void SansArgument_LeModeVerbeuxEstInactif()
    {
        var options = AppOptions.Analyser(Array.Empty<string>());

        Assert.False(options.Verbeux);
        Assert.False(options.AfficherAide);
        Assert.False(options.AfficherVersion);
        Assert.Empty(options.AutresArguments);
    }

    [Fact]
    public void Analyser_SupporteNull()
    {
        // Un point d'entrée peut être appelé sans arguments (tests, hôte externe).
        var options = AppOptions.Analyser(null);

        Assert.False(options.Verbeux);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    [InlineData("--HELP")]
    public void Aide_EstReconnue(string argument)
    {
        Assert.True(AppOptions.Analyser(new[] { argument }).AfficherAide);
    }

    [Fact]
    public void Version_EstReconnue()
    {
        Assert.True(AppOptions.Analyser(new[] { "--version" }).AfficherVersion);
    }

    [Fact]
    public void LesArgumentsInconnus_SontConservesPourLaCoucheGraphique()
    {
        var options = AppOptions.Analyser(new[] { "--verbose", "--fbdev", "--drm-device=/dev/dri/card0" });

        Assert.True(options.Verbeux);
        Assert.Equal(new[] { "--fbdev", "--drm-device=/dev/dri/card0" }, options.AutresArguments);
    }

    [Fact]
    public void LesOptionsReconnues_NeSontPasRetransmises()
    {
        var options = AppOptions.Analyser(new[] { "--verbose", "--version", "--help" });

        Assert.Empty(options.AutresArguments);
        Assert.True(options.Verbeux);
        Assert.True(options.AfficherVersion);
        Assert.True(options.AfficherAide);
    }

    [Fact]
    public void L_aide_MentionneLesOptionsUtiles()
    {
        var aide = AppOptions.Aide;

        Assert.Contains("--verbose", aide);
        Assert.Contains("--version", aide);
        Assert.Contains(AppInfo.Version, aide);
    }
}

/// <summary>
/// Niveau DEBUG du journal : silencieux par défaut, bavard sur demande — c'est
/// tout l'intérêt, un journal livré à un utilisateur ne doit pas l'être.
/// </summary>
public class FileLoggerVerboseTests : IDisposable
{
    private readonly string _dossier;
    private readonly bool _verbeuxInitial = FileLogger.VerbeuxParDefaut;

    public FileLoggerVerboseTests()
    {
        _dossier = Path.Combine(Path.GetTempPath(), $"fatouradz_verbose_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        FileLogger.VerbeuxParDefaut = _verbeuxInitial;

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

    private string Contenu =>
        File.Exists(Path.Combine(_dossier, $"fatouradz-{DateTime.Now:yyyyMMdd}.log"))
            ? File.ReadAllText(Path.Combine(_dossier, $"fatouradz-{DateTime.Now:yyyyMMdd}.log"))
            : string.Empty;

    [Fact]
    public void ParDefaut_LeDetailNEstPasEcrit()
    {
        var logger = new FileLogger { DossierLogs = _dossier, Verbeux = false };

        logger.Debug("détail de diagnostic");
        logger.Info("information");

        Assert.False(logger.EstVerbeux);
        Assert.DoesNotContain("détail de diagnostic", Contenu);
        // Le niveau normal n'est pas affecté.
        Assert.Contains("information", Contenu);
    }

    [Fact]
    public void EnModeVerbeux_LeDetailEstEcrit()
    {
        var logger = new FileLogger { DossierLogs = _dossier, Verbeux = true };

        logger.Debug("détail de diagnostic");

        Assert.True(logger.EstVerbeux);
        Assert.Contains("[DEBUG]", Contenu);
        Assert.Contains("détail de diagnostic", Contenu);
    }

    [Fact]
    public void EnModeVerbeux_UneExceptionPEutEtreJointe()
    {
        var logger = new FileLogger { DossierLogs = _dossier, Verbeux = true };

        logger.Debug("requête lente", new TimeoutException("délai dépassé"));

        Assert.Contains("délai dépassé", Contenu);
    }

    [Fact]
    public void LeModeVerbeuxParDefaut_SAppliqueAuxNouvellesInstances()
    {
        // C'est ce que fait Program.Main avec --verbose : le conteneur de services
        // construit le journal après la lecture des arguments.
        FileLogger.VerbeuxParDefaut = true;

        var logger = new FileLogger { DossierLogs = _dossier };

        Assert.True(logger.EstVerbeux);
    }
}
