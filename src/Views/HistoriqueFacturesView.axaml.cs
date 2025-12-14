using Avalonia.Controls;
using Avalonia.Input;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Views;

public partial class HistoriqueFacturesView : UserControl
{
    public HistoriqueFacturesView()
    {
        InitializeComponent();
    }

    private void OnRechercheKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is HistoriqueFacturesViewModel vm)
        {
            vm.RechercherCommand.Execute(null);
        }
    }
}
