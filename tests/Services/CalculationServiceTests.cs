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

    // Barème progressif (Loi de Finances 2025, art. 100 du code du timbre) :
    //   <= 300 DA             : exonéré
    //   300 DA  -> 30 000 DA  : 1 %
    //   30 000  -> 100 000 DA : 1,5 %
    //   > 100 000 DA          : 2 %
    // Minimum de perception : 5 DA. Aucun plafond.
    [Theory]
    [InlineData(0, 0)]            // Montant nul
    [InlineData(100, 0)]          // <= 300 DA : exonéré
    [InlineData(300, 0)]          // Au seuil d'exonération
    [InlineData(350, 5)]          // 1 % de 350 = 3,50 DA -> minimum 5 DA
    [InlineData(500, 5)]          // 1 % = 5 DA
    [InlineData(1000, 10)]        // 1 % = 10 DA
    [InlineData(30000, 300)]      // 1 % = 300 DA (borne haute tranche 1)
    [InlineData(50000, 750)]      // 1,5 % = 750 DA
    [InlineData(100000, 1500)]    // 1,5 % = 1500 DA (borne haute tranche 2)
    [InlineData(200000, 4000)]    // 2 % = 4000 DA
    [InlineData(250000, 5000)]    // 2 % = 5000 DA (plus de plafond)
    [InlineData(500000, 10000)]   // 2 % = 10 000 DA (plus de plafond)
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
    public void CalculerTimbreFiscal_BelowExonerationThreshold_ReturnsZero()
    {
        // Les montants <= 300 DA ne donnent pas lieu au droit de timbre
        var result = _service.CalculerTimbreFiscal(250);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculerTimbreFiscal_MinimumIs5DA()
    {
        // Montant juste au-dessus du seuil : 1 % < 5 DA -> minimum 5 DA
        var result = _service.CalculerTimbreFiscal(350);
        Assert.Equal(5m, result);
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
    public void CalculerTotaux_HighAmount_UsesTopTimbreBracket()
    {
        // Montant TTC > 100 000 DA : tranche à 2 % (plus de plafond depuis la LF 2025)
        var lignes = new List<LigneFacture>
        {
            new() { Quantite = 1, PrixUnitaire = 100000, TauxTVA = TauxTVA.TVA19 }
        };

        var result = _service.CalculerTotaux(lignes, appliquerTimbre: true);

        Assert.Equal(100000m, result.TotalHT);
        Assert.Equal(19000m, result.TVA19);
        Assert.Equal(119000m, result.TotalTTC);
        // Timbre : 119000 * 2 % = 2380 DA
        Assert.Equal(2380m, result.TimbreFiscal);
        Assert.Equal(121380m, result.MontantTotal);
    }

    #endregion
}
