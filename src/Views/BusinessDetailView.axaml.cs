using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FatouraDZ.Models;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Views;

public partial class BusinessDetailView : UserControl
{
    private Facture? _currentContextMenuFacture;
    
    public BusinessDetailView()
    {
        InitializeComponent();
    }
    
    public void OnInvoiceBorderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is Facture facture)
        {
            _currentContextMenuFacture = facture;
            
            // Left click opens preview
            var point = e.GetCurrentPoint(border);
            if (point.Properties.IsLeftButtonPressed && DataContext is BusinessDetailViewModel vm)
            {
                vm.PreviewInvoiceCommand.Execute(facture);
            }
        }
    }
    
    private void OnPreviewInvoiceClick(object? sender, RoutedEventArgs e)
    {
        if (_currentContextMenuFacture != null && DataContext is BusinessDetailViewModel vm)
        {
            vm.PreviewInvoiceCommand.Execute(_currentContextMenuFacture);
        }
    }
    
    private void OnEditInvoiceClick(object? sender, RoutedEventArgs e)
    {
        if (_currentContextMenuFacture != null && DataContext is BusinessDetailViewModel vm)
        {
            vm.EditInvoiceCommand.Execute(_currentContextMenuFacture);
        }
    }
    
    private void OnTogglePaymentClick(object? sender, RoutedEventArgs e)
    {
        if (_currentContextMenuFacture != null && DataContext is BusinessDetailViewModel vm)
        {
            vm.TogglePaymentStatusCommand.Execute(_currentContextMenuFacture);
        }
    }
    
    private void OnToggleArchiveClick(object? sender, RoutedEventArgs e)
    {
        if (_currentContextMenuFacture != null && DataContext is BusinessDetailViewModel vm)
        {
            vm.ToggleArchiveInvoiceCommand.Execute(_currentContextMenuFacture);
        }
    }
}
