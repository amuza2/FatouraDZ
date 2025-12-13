using Avalonia.Controls;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Views;

public partial class EntrepreneurConfigView : UserControl
{
    public EntrepreneurConfigView()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is EntrepreneurConfigViewModel vm)
        {
            await vm.ChargerDonneesAsync();
        }
    }
}
