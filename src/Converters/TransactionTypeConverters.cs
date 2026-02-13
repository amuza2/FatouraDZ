using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FatouraDZ.Models;

namespace FatouraDZ.Converters;

public class TransactionTypeToColorConverter : IValueConverter
{
    public static readonly TransactionTypeToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TypeTransaction type)
        {
            return type switch
            {
                TypeTransaction.Recette => new SolidColorBrush(Color.Parse("#16A34A")),
                TypeTransaction.Depense => new SolidColorBrush(Color.Parse("#DC2626")),
                _ => new SolidColorBrush(Color.Parse("#64748B"))
            };
        }
        return new SolidColorBrush(Color.Parse("#64748B"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TransactionTypeToSignConverter : IValueConverter
{
    public static readonly TransactionTypeToSignConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TypeTransaction type)
        {
            return type == TypeTransaction.Recette ? "+" : "-";
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
