using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FatouraDZ.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    public ConfirmationDialog(string titre, string message) : this()
    {
        TitreText.Text = titre;
        MessageText.Text = message;
        Title = titre;
    }

    private void OnAnnulerClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnConfirmerClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
