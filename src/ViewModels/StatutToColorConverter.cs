using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FatouraDZ.Models;

namespace FatouraDZ.ViewModels;

public class StatutToColorConverter : IValueConverter
{
    public static readonly StatutToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is StatutFacture statut)
        {
            return statut switch
            {
                StatutFacture.EnAttente => new SolidColorBrush(Color.Parse("#F59E0B")),
                StatutFacture.Payee => new SolidColorBrush(Color.Parse("#16A34A")),
                StatutFacture.Annulee => new SolidColorBrush(Color.Parse("#EF4444")),
                StatutFacture.Archivee => new SolidColorBrush(Color.Parse("#64748B")),
                _ => new SolidColorBrush(Color.Parse("#94A3B8"))
            };
        }
        return new SolidColorBrush(Color.Parse("#94A3B8"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatutToBackgroundConverter : IValueConverter
{
    public static readonly StatutToBackgroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is StatutFacture statut)
        {
            return statut switch
            {
                StatutFacture.EnAttente => new SolidColorBrush(Color.Parse("#FEF3C7")), // Yellow background
                StatutFacture.Payee => new SolidColorBrush(Color.Parse("#DCFCE7")),     // Green background
                StatutFacture.Annulee => new SolidColorBrush(Color.Parse("#FEE2E2")),   // Red background
                StatutFacture.Archivee => new SolidColorBrush(Color.Parse("#F1F5F9")),  // Gray background
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

public class StatutToForegroundConverter : IValueConverter
{
    public static readonly StatutToForegroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is StatutFacture statut)
        {
            return statut switch
            {
                StatutFacture.EnAttente => new SolidColorBrush(Color.Parse("#92400E")), // Dark yellow
                StatutFacture.Payee => new SolidColorBrush(Color.Parse("#166534")),     // Dark green
                StatutFacture.Annulee => new SolidColorBrush(Color.Parse("#991B1B")),   // Dark red
                StatutFacture.Archivee => new SolidColorBrush(Color.Parse("#475569")),  // Dark gray
                _ => new SolidColorBrush(Color.Parse("#475569"))
            };
        }
        return new SolidColorBrush(Color.Parse("#475569"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatutToIndexConverter : IValueConverter
{
    public static readonly StatutToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is StatutFacture statut)
        {
            return statut switch
            {
                StatutFacture.EnAttente => 0,
                StatutFacture.Payee => 1,
                StatutFacture.Annulee => 2,
                StatutFacture.Archivee => 3,
                _ => 0
            };
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => StatutFacture.EnAttente,
                1 => StatutFacture.Payee,
                2 => StatutFacture.Annulee,
                3 => StatutFacture.Archivee,
                _ => StatutFacture.EnAttente
            };
        }
        return StatutFacture.EnAttente;
    }
}
