using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.Tests.Services;

public class CalculationServiceTests
{
    private readonly CalculationService _service;

    public CalculationServiceTests()
    {
        _service = new CalculationService();
    }

    #region CalculerTotalHTLigne Tests

    [Theory]
    [InlineData(1, 1000, 1000)]
    [InlineData(2, 500, 1000)]
    [InlineData(1.5, 1000, 1500)]
    [InlineData(0.5, 200, 100)]
    [InlineData(10, 99.99, 999.90)]
    public void CalculerTotalHTLigne_ReturnsCorrectTotal(decimal quantite, decimal prixUnitaire, decimal expected)
    {
        var result = _service.CalculerTotalHTLigne(quantite, prixUnitaire);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculerTotalHTLigne_WithZeroQuantity_ReturnsZero()
    {
        var result = _service.CalculerTotalHTLigne(0, 1000);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculerTotalHTLigne_WithZeroPrice_ReturnsZero()
    {
        var result = _service.CalculerTotalHTLigne(5, 0);
        Assert.Equal(0, result);
    }

    #endregion

    #region CalculerTVA Tests

    [Theory]
    [InlineData(1000, TauxTVA.TVA19, 190)]
    [InlineData(1000, TauxTVA.TVA9, 90)]
    [InlineData(1000, TauxTVA.Exonere, 0)]
    [InlineData(10000, TauxTVA.TVA19, 1900)]
    [InlineData(10000, TauxTVA.TVA9, 900)]
    [InlineData(0, TauxTVA.TVA19, 0)]
    public void CalculerTVA_ReturnsCorrectAmount(decimal totalHT, TauxTVA taux, decimal expected)
    {
        var result = _service.CalculerTVA(totalHT, taux);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculerTVA_RoundsToTwoDecimals()
    {
        // 333.33 * 0.19 = 63.3327 -> should round to 63.33
        var result = _service.CalculerTVA(333.33m, TauxTVA.TVA19);
        Assert.Equal(63.33m, result);
    }

    #endregion

    #region CalculerTimbreFiscal Tests

    [Theory]
    [InlineData(0, 0)]           // Zero amount
    [InlineData(100, 5)]         // Small amount, minimum 5 DA
    [InlineData(500, 5)]         // 500 * 1% = 5 DA (minimum)
    [InlineData(1000, 10)]       // 1000 * 1% = 10 DA
    [InlineData(30000, 300)]     // 30000 * 1% = 300 DA
    [InlineData(50000, 500)]     // 50000 * 1% = 500 DA
    [InlineData(100000, 1000)]   // 100000 * 1% = 1000 DA
    [InlineData(250000, 2500)]   // 250000 * 1% = 2500 DA (max limit)
    [InlineData(500000, 2500)]   // 500000 * 1% = 5000 DA but capped at 2500 DA max
    public void CalculerTimbreFiscal_ReturnsCorrectAmount(decimal montantTTC, decimal expected)
    {
        var result = _service.CalculerTimbreFiscal(montantTTC);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculerTimbreFiscal_NegativeAmount_ReturnsZero()
    {
        var result = _service.CalculerTimbreFiscal(-1000);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculerTimbreFiscal_MinimumIs5DA()
    {
        // Very small amount should still return minimum 5 DA
        var result = _service.CalculerTimbreFiscal(100);
        Assert.True(result >= 5);
    }

    #endregion

    #region CalculerTotaux Tests

    [Fact]
    public void CalculerTotaux_SingleLine_TVA19_WithoutTimbre()
    {
        var lignes = new List<LigneFacture>
        {
            new() { Quantite = 1, PrixUnitaire = 1000, TauxTVA = TauxTVA.TVA19 }
        };

        var result = _service.CalculerTotaux(lignes, appliquerTimbre: false);

        Assert.Equal(1000m, result.TotalHT);
        Assert.Equal(190m, result.TVA19);
        Assert.Equal(0m, result.TVA9);
        Assert.Equal(1190m, result.TotalTTC);
        Assert.Equal(0m, result.TimbreFiscal);
        Assert.Equal(1190m, result.MontantTotal);
    }

    [Fact]
    public void CalculerTotaux_SingleLine_TVA19_WithTimbre()
    {
        var lignes = new List<LigneFacture>
        {
            new() { Quantite = 1, PrixUnitaire = 1000, TauxTVA = TauxTVA.TVA19 }
        };

        var result = _service.CalculerTotaux(lignes, appliquerTimbre: true);

        Assert.Equal(1000m, result.TotalHT);
        Assert.Equal(190m, result.TVA19);
        Assert.Equal(1190m, result.TotalTTC);
        // Timbre: 1190 * 1% = 11.90 DA
        Assert.Equal(11.90m, result.TimbreFiscal);
        Assert.Equal(1201.90m, result.MontantTotal);
    }

    [Fact]
    public void CalculerTotaux_MultipleLines_MixedTVA()
    {
        var lignes = new List<LigneFacture>
        {
            new() { Quantite = 2, PrixUnitaire = 1000, TauxTVA = TauxTVA.TVA19 },  // 2000 HT, 380 TVA
            new() { Quantite = 1, PrixUnitaire = 500, TauxTVA = TauxTVA.TVA9 },    // 500 HT, 45 TVA
            new() { Quantite = 3, PrixUnitaire = 200, TauxTVA = TauxTVA.Exonere }  // 600 HT, 0 TVA
        };

        var result = _service.CalculerTotaux(lignes, appliquerTimbre: false);

        Assert.Equal(3100m, result.TotalHT);      // 2000 + 500 + 600
        Assert.Equal(380m, result.TVA19);         // 2000 * 19%
        Assert.Equal(45m, result.TVA9);           // 500 * 9%
        Assert.Equal(3525m, result.TotalTTC);     // 3100 + 380 + 45
        Assert.Equal(0m, result.TimbreFiscal);
        Assert.Equal(3525m, result.MontantTotal);
    }

    [Fact]
    public void CalculerTotaux_EmptyLines_ReturnsZeros()
    {
        var lignes = new List<LigneFacture>();

        var result = _service.CalculerTotaux(lignes, appliquerTimbre: false);

        Assert.Equal(0m, result.TotalHT);
        Assert.Equal(0m, result.TVA19);
        Assert.Equal(0m, result.TVA9);
        Assert.Equal(0m, result.TotalTTC);
        Assert.Equal(0m, result.TimbreFiscal);
        Assert.Equal(0m, result.MontantTotal);
    }

    [Fact]
    public void CalculerTotaux_LargeAmount_CorrectTimbreRate()
    {
        // Test with amount > 250,000 DA to verify max cap at 2500 DA
        var lignes = new List<LigneFacture>
        {
            new() { Quantite = 1, PrixUnitaire = 100000, TauxTVA = TauxTVA.TVA19 }
        };

        var result = _service.CalculerTotaux(lignes, appliquerTimbre: true);

        Assert.Equal(100000m, result.TotalHT);
        Assert.Equal(19000m, result.TVA19);
        Assert.Equal(119000m, result.TotalTTC);
        // Timbre: 119000 * 1% = 1190 DA
        Assert.Equal(1190m, result.TimbreFiscal);
        Assert.Equal(120190m, result.MontantTotal);
    }

    #endregion
}
