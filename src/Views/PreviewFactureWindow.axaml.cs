using Avalonia.Controls;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Views;

public partial class PreviewFactureWindow : Window
{
    public PreviewFactureWindow()
    {
        InitializeComponent();
    }

    public PreviewFactureWindow(PreviewFactureViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.DemanderFermeture += () => Close();
    }

    protected override async void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is PreviewFactureViewModel vm)
        {
            await vm.GenererPreviewAsync();
        }
    }
}
