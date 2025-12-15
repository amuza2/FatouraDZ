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
                0 => "NET À PAYER",
                1 => "NET À DÉDUIRE",
                2 => "MONTANT ANNULÉ",
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
