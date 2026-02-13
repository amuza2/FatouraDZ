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

public class TransactionTypeToLabelConverter : IValueConverter
{
    public static readonly TransactionTypeToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TypeTransaction type)
        {
            return type == TypeTransaction.Recette ? "Recette" : "Dépense";
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TransactionTypeToBackgroundConverter : IValueConverter
{
    public static readonly TransactionTypeToBackgroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TypeTransaction type)
        {
            return type switch
            {
                TypeTransaction.Recette => new SolidColorBrush(Color.Parse("#DCFCE7")),
                TypeTransaction.Depense => new SolidColorBrush(Color.Parse("#FEE2E2")),
                _ => new SolidColorBrush(Color.Parse("#F1F5F9"))
            };
        }
        return new SolidColorBrush(Color.Parse("#F1F5F9"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
