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
    public void DefaultTimbreSeuilExoneration_Is300()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(300m, settings.TimbreSeuilExoneration);
    }

    [Fact]
    public void DefaultTimbreSeuil1_Is30000()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(30000m, settings.TimbreSeuil1);
    }

    [Fact]
    public void DefaultTimbreTaux1_Is1()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(1m, settings.TimbreTaux1);
    }

    [Fact]
    public void DefaultTimbreSeuil2_Is100000()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(100000m, settings.TimbreSeuil2);
    }

    [Fact]
    public void DefaultTimbreTaux2_Is1Point5()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(1.5m, settings.TimbreTaux2);
    }

    [Fact]
    public void DefaultTimbreTaux3_Is2()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(2m, settings.TimbreTaux3);
    }

    [Fact]
    public void DefaultTimbreMinimum_Is5()
    {
        // Arrange
        var settings = new AppSettings();

        // Assert
        Assert.Equal(5m, settings.TimbreMinimum);
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
    public void TimbreSeuilsEtTaux_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.TimbreSeuilExoneration = 500m;
        settings.TimbreSeuil1 = 50000m;
        settings.TimbreTaux1 = 1.25m;
        settings.TimbreSeuil2 = 150000m;
        settings.TimbreTaux2 = 1.75m;
        settings.TimbreTaux3 = 2.5m;
        settings.TimbreMinimum = 10m;

        // Assert
        Assert.Equal(500m, settings.TimbreSeuilExoneration);
        Assert.Equal(50000m, settings.TimbreSeuil1);
        Assert.Equal(1.25m, settings.TimbreTaux1);
        Assert.Equal(150000m, settings.TimbreSeuil2);
        Assert.Equal(1.75m, settings.TimbreTaux2);
        Assert.Equal(2.5m, settings.TimbreTaux3);
        Assert.Equal(10m, settings.TimbreMinimum);
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
