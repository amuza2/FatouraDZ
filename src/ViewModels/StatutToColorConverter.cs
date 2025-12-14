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
