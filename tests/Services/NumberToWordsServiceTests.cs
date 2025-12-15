using FatouraDZ.Services;

namespace FatouraDZ.Tests.Services;

public class NumberToWordsServiceTests
{
    private readonly NumberToWordsService _service;

    public NumberToWordsServiceTests()
    {
        _service = new NumberToWordsService();
    }

    #region Basic Numbers Tests

    [Fact]
    public void ConvertirEnLettres_Zero_ReturnsCorrectText()
    {
        var result = _service.ConvertirEnLettres(0);
        // Zero is a special case that returns lowercase
        Assert.Equal("zéro dinar algérien", result);
    }

    [Fact]
    public void ConvertirEnLettres_One_ReturnsSingular()
    {
        var result = _service.ConvertirEnLettres(1);
        Assert.Equal("Un dinar algérien", result);
    }

    [Theory]
    [InlineData(2, "Deux dinars algériens")]
    [InlineData(5, "Cinq dinars algériens")]
    [InlineData(10, "Dix dinars algériens")]
    [InlineData(11, "Onze dinars algériens")]
    [InlineData(15, "Quinze dinars algériens")]
    [InlineData(19, "Dix-neuf dinars algériens")]
    public void ConvertirEnLettres_SmallNumbers_ReturnsCorrectText(decimal amount, string expected)
    {
        var result = _service.ConvertirEnLettres(amount);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Tens Tests

    [Theory]
    [InlineData(20, "Vingt dinars algériens")]
    [InlineData(21, "Vingt-et-un dinars algériens")]
    [InlineData(22, "Vingt-deux dinars algériens")]
    [InlineData(30, "Trente dinars algériens")]
    [InlineData(31, "Trente-et-un dinars algériens")]
    [InlineData(40, "Quarante dinars algériens")]
    [InlineData(50, "Cinquante dinars algériens")]
    [InlineData(60, "Soixante dinars algériens")]
    public void ConvertirEnLettres_Tens_ReturnsCorrectText(decimal amount, string expected)
    {
        var result = _service.ConvertirEnLettres(amount);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Special French Numbers (70-99) Tests

    [Theory]
    [InlineData(70, "Soixante-dix dinars algériens")]
    [InlineData(71, "Soixante-et-onze dinars algériens")]
    [InlineData(72, "Soixante-douze dinars algériens")]
    [InlineData(79, "Soixante-dix-neuf dinars algériens")]
    [InlineData(80, "Quatre-vingts dinars algériens")]
    [InlineData(81, "Quatre-vingt-un dinars algériens")]
    [InlineData(90, "Quatre-vingt-dix dinars algériens")]
    [InlineData(91, "Quatre-vingt-onze dinars algériens")]
    [InlineData(99, "Quatre-vingt-dix-neuf dinars algériens")]
    public void ConvertirEnLettres_FrenchSpecialNumbers_ReturnsCorrectText(decimal amount, string expected)
    {
        var result = _service.ConvertirEnLettres(amount);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Hundreds Tests

    [Theory]
    [InlineData(100, "Cent dinars algériens")]
    [InlineData(101, "Cent un dinars algériens")]
    [InlineData(200, "Deux cents dinars algériens")]
    [InlineData(201, "Deux cent un dinars algériens")]
    [InlineData(500, "Cinq cents dinars algériens")]
    [InlineData(999, "Neuf cent quatre-vingt-dix-neuf dinars algériens")]
    public void ConvertirEnLettres_Hundreds_ReturnsCorrectText(decimal amount, string expected)
    {
        var result = _service.ConvertirEnLettres(amount);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Thousands Tests

    [Theory]
    [InlineData(1000, "Mille dinars algériens")]
    [InlineData(1001, "Mille un dinars algériens")]
    [InlineData(2000, "Deux mille dinars algériens")]
    [InlineData(10000, "Dix mille dinars algériens")]
    [InlineData(12500, "Douze mille cinq cents dinars algériens")]
    public void ConvertirEnLettres_Thousands_ReturnsCorrectText(decimal amount, string expected)
    {
        var result = _service.ConvertirEnLettres(amount);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Millions Tests

    [Theory]
    [InlineData(1000000, "Un million dinars algériens")]
    [InlineData(2000000, "Deux millions dinars algériens")]
    [InlineData(1500000, "Un million cinq cents mille dinars algériens")]
    public void ConvertirEnLettres_Millions_ReturnsCorrectText(decimal amount, string expected)
    {
        var result = _service.ConvertirEnLettres(amount);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Decimal (Centimes) Tests

    [Fact]
    public void ConvertirEnLettres_WithCentimes_ReturnsCorrectText()
    {
        var result = _service.ConvertirEnLettres(100.50m);
        Assert.Contains("cent dinars algériens", result.ToLower());
        Assert.Contains("centime", result.ToLower());
    }

    [Fact]
    public void ConvertirEnLettres_OneCentime_ReturnsSingular()
    {
        var result = _service.ConvertirEnLettres(1.01m);
        Assert.Contains("un centime", result.ToLower());
    }

    [Fact]
    public void ConvertirEnLettres_MultipleCentimes_ReturnsPlural()
    {
        var result = _service.ConvertirEnLettres(1.50m);
        Assert.Contains("centimes", result.ToLower());
    }

    #endregion

    #region Real Invoice Amounts Tests

    [Fact]
    public void ConvertirEnLettres_TypicalInvoiceAmount_ReturnsCorrectText()
    {
        // Typical invoice: 15,000 DA
        var result = _service.ConvertirEnLettres(15000);
        Assert.Equal("Quinze mille dinars algériens", result);
    }

    [Fact]
    public void ConvertirEnLettres_LargeInvoiceAmount_ReturnsCorrectText()
    {
        // Large invoice: 150,000 DA
        var result = _service.ConvertirEnLettres(150000);
        Assert.Equal("Cent cinquante mille dinars algériens", result);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void ConvertirEnLettres_StartsWithCapitalLetter()
    {
        var result = _service.ConvertirEnLettres(100);
        Assert.True(char.IsUpper(result[0]));
    }

    [Fact]
    public void ConvertirEnLettres_ContainsDinarsAlgeriens()
    {
        var result = _service.ConvertirEnLettres(1000);
        Assert.Contains("dinars algériens", result.ToLower());
    }

    #endregion
}
