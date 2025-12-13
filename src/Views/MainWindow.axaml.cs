using Avalonia.Controls;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.DemanderPrevisualisation += OuvrirPrevisualisation;
            await vm.InitialiserAsync();
        }
    }

    private void OuvrirPrevisualisation(FatouraDZ.Models.Facture facture, FatouraDZ.Models.Entrepreneur entrepreneur)
    {
        var previewVm = new PreviewFactureViewModel(facture, entrepreneur);
        var previewWindow = new PreviewFactureWindow(previewVm);
        previewWindow.ShowDialog(this);
    }
}