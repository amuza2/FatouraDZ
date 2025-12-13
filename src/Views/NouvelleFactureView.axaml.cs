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
}
