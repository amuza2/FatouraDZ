using System;
using Avalonia.Controls;
using Avalonia.Input;
using FatouraDZ.Models;
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

    private void OnStatutSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Ignore if no items were added (initial load or programmatic change)
        if (e.AddedItems.Count == 0) return;
        
        if (sender is ComboBox comboBox && 
            comboBox.DataContext is Facture facture &&
            DataContext is HistoriqueFacturesViewModel vm &&
            comboBox.SelectedIndex >= 0 &&
            facture.Id > 0)
        {
            var nouveauStatut = comboBox.SelectedIndex switch
            {
                0 => StatutFacture.EnAttente,
                1 => StatutFacture.Payee,
                2 => StatutFacture.Annulee,
                _ => StatutFacture.EnAttente
            };

            // Only update if status actually changed
            if (facture.Statut != nouveauStatut)
            {
                Console.WriteLine($"[DEBUG] Changing status for {facture.NumeroFacture} (Id={facture.Id}) from {facture.Statut} to {nouveauStatut}");
                _ = vm.ChangerStatutDirectCommand.ExecuteAsync((facture, nouveauStatut));
            }
        }
    }
}
