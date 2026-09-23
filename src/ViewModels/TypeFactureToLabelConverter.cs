using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FatouraDZ.ViewModels;

public class TypeFactureToLabelConverter : IValueConverter
{
    public static readonly TypeFactureToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                1 => "NET À DÉDUIRE",
                // 0 = facture normale, 2 = facture proforma : même libellé que celui du PDF.
                _ => "NET À PAYER"
            };
        }
        return "MONTANT TOTAL";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
