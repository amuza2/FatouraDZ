using Avalonia.Controls;
using Avalonia.Interactivity;
using FatouraDZ.Models;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Views;

public partial class ComptabiliteView : UserControl
{
    public ComptabiliteView()
    {
        InitializeComponent();
    }

    private void OnPeriodeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out var index))
        {
            if (DataContext is ComptabiliteViewModel vm)
                vm.PeriodeIndex = index;
        }
    }

    private void OnTypeFiltreClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out var index))
        {
            if (DataContext is ComptabiliteViewModel vm)
                vm.TypeFiltre = index;
        }
    }

    private void OnEditTransactionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Transaction transaction)
        {
            if (this.DataContext is ComptabiliteViewModel vm)
                vm.ModifierTransactionCommand.Execute(transaction);
        }
    }

    private void OnArchiveTransactionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Transaction transaction)
        {
            if (this.DataContext is ComptabiliteViewModel vm)
                vm.ArchiverTransactionCommand.Execute(transaction);
        }
    }
}
