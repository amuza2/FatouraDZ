using FatouraDZ.Services;

namespace FatouraDZ.Tests.Services;

public class AppSettingsTests
{
    #region Default Values Tests

    [Fact]
    public void Instance_ReturnsNonNull()
    {
        // Act
        var settings = AppSettings.Instance;

        // Assert
        Assert.NotNull(settings);
    }

    [Fact]
    public void DefaultTauxTVAStandard_Is19()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(19m, settings.TauxTVAStandard);
    }

    [Fact]
    public void DefaultTauxTVAReduit_Is9()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(9m, settings.TauxTVAReduit);
    }

    [Fact]
    public void DefaultTauxTimbreFiscal_Is1()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(1m, settings.TauxTimbreFiscal);
    }

    [Fact]
    public void DefaultMontantMaxTimbre_Is2500()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(2500m, settings.MontantMaxTimbre);
    }

    [Fact]
    public void DefaultTauxRetenueSourceDefaut_Is5()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(5m, settings.TauxRetenueSourceDefaut);
    }

    [Fact]
    public void DefaultDelaiPaiementDefaut_Is30()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(30, settings.DelaiPaiementDefaut);
    }

    [Fact]
    public void DefaultFormatNumeroFacture_IsCorrect()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal("FAC-{ANNEE}-{NUM}", settings.FormatNumeroFacture);
    }

    #endregion

    #region Property Modification Tests

    [Fact]
    public void TauxTVAStandard_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.TauxTVAStandard = 20m;

        // Assert
        Assert.Equal(20m, settings.TauxTVAStandard);
    }

    [Fact]
    public void TauxTVAReduit_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.TauxTVAReduit = 10m;

        // Assert
        Assert.Equal(10m, settings.TauxTVAReduit);
    }

    [Fact]
    public void TauxTimbreFiscal_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.TauxTimbreFiscal = 1.5m;

        // Assert
        Assert.Equal(1.5m, settings.TauxTimbreFiscal);
    }

    [Fact]
    public void MontantMaxTimbre_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.MontantMaxTimbre = 3000m;

        // Assert
        Assert.Equal(3000m, settings.MontantMaxTimbre);
    }

    [Fact]
    public void FormatNumeroFacture_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.FormatNumeroFacture = "INV-{ANNEE}-{NUM}";

        // Assert
        Assert.Equal("INV-{ANNEE}-{NUM}", settings.FormatNumeroFacture);
    }

    [Fact]
    public void DelaiPaiementDefaut_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.DelaiPaiementDefaut = 60;

        // Assert
        Assert.Equal(60, settings.DelaiPaiementDefaut);
    }

    #endregion
}
