using Avalonia.Controls;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Views;

public partial class NouvelleFactureView : UserControl
{
    public NouvelleFactureView()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        if (DataContext is NouvelleFactureViewModel vm)
        {
            await vm.InitialiserAsync();
        }
    }
    
    private void OnRechercheClientTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is NouvelleFactureViewModel vm && sender is TextBox textBox)
        {
            vm.RechercheClient = textBox.Text ?? string.Empty;
        }
    }
}
