using Avalonia.Controls;
using Avalonia.Input;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Views;

public partial class ArchiveFacturesView : UserControl
{
    public ArchiveFacturesView()
    {
        InitializeComponent();
    }

    private void OnRechercheKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ArchiveFacturesViewModel vm)
        {
            vm.RechercherCommand.Execute(null);
        }
    }
}
