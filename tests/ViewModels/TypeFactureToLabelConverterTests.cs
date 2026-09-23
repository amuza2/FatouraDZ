using System.Globalization;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Tests.ViewModels;

public class TypeFactureToLabelConverterTests
{
    private readonly TypeFactureToLabelConverter _converter = TypeFactureToLabelConverter.Instance;

    // 0 = facture normale, 1 = facture d'avoir, 2 = facture proforma
    // (mêmes libellés que ceux imprimés dans le PDF).
    [Theory]
    [InlineData(0, "NET À PAYER")]
    [InlineData(1, "NET À DÉDUIRE")]
    [InlineData(2, "NET À PAYER")]
    public void Convert_ReturnsLabelMatchingInvoiceType(int index, string expected)
    {
        var result = _converter.Convert(index, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_UnknownValue_ReturnsDefaultLabel()
    {
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("MONTANT TOTAL", result);
    }
}
