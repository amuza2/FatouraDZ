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
            await vm.InitialiserAsync();
        }
    }
}