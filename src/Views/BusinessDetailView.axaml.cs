using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FatouraDZ.Models;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Views;

public partial class BusinessDetailView : UserControl
{
    public BusinessDetailView()
    {
        InitializeComponent();
    }
    
    public void OnInvoiceBorderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is Facture facture)
        {
            var point = e.GetCurrentPoint(border);
            if (point.Properties.IsLeftButtonPressed && DataContext is BusinessDetailViewModel vm)
            {
                vm.PreviewInvoiceCommand.Execute(facture);
            }
        }
    }
    
    private void OnPreviewInvoiceClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Facture facture && DataContext is BusinessDetailViewModel vm)
            vm.PreviewInvoiceCommand.Execute(facture);
    }
    
    private void OnEditInvoiceClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Facture facture && DataContext is BusinessDetailViewModel vm)
            vm.EditInvoiceCommand.Execute(facture);
    }
    
    private void OnTogglePaymentClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Facture facture && DataContext is BusinessDetailViewModel vm)
            vm.TogglePaymentStatusCommand.Execute(facture);
    }
    
    private void OnToggleArchiveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Facture facture && DataContext is BusinessDetailViewModel vm)
            vm.ToggleArchiveInvoiceCommand.Execute(facture);
    }
}
