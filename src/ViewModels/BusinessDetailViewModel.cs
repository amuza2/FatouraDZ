using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class BusinessDetailViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private Business _business = null!;

    [ObservableProperty]
    private bool _estChargement;

    [ObservableProperty]
    private string? _messageErreur;

    [ObservableProperty]
    private string _recherche = string.Empty;

    [ObservableProperty]
    private int _typeFactureIndex;

    [ObservableProperty]
    private int _statutIndex;

    public ObservableCollection<Facture> Factures { get; } = new();

    // Statistics
    [ObservableProperty]
    private int _nombreFactures;

    [ObservableProperty]
    private decimal _chiffreAffaires;

    [ObservableProperty]
    private int _facturesEnAttente;

    [ObservableProperty]
    private int _facturesPayees;

    // Events
    public event Action? BackRequested;
    public event Action? EditBusinessRequested;
    public event Action? CreateInvoiceRequested;
    public event Action<Facture>? EditInvoiceRequested;
    public event Action<Facture>? PreviewInvoiceRequested;

    public BusinessDetailViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
    }

    public void SetBusiness(Business business)
    {
        Business = business;
    }

    public async Task ChargerFacturesAsync()
    {
        if (Business == null) return;

        EstChargement = true;
        MessageErreur = null;

        try
        {
            var factures = await _databaseService.GetFacturesByBusinessIdAsync(Business.Id);
            
            // Apply filters
            var filtered = factures.AsEnumerable();

            if (TypeFactureIndex > 0)
            {
                var type = TypeFactureIndex switch
                {
                    1 => TypeFacture.Normale,
                    2 => TypeFacture.Avoir,
                    3 => TypeFacture.Proformat,
                    _ => (TypeFacture?)null
                };
                if (type.HasValue)
                    filtered = filtered.Where(f => f.TypeFacture == type.Value);
            }

            if (StatutIndex > 0)
            {
                var statut = StatutIndex switch
                {
                    1 => StatutFacture.EnAttente,
                    2 => StatutFacture.Payee,
                    3 => StatutFacture.Annulee,
                    _ => (StatutFacture?)null
                };
                if (statut.HasValue)
                    filtered = filtered.Where(f => f.Statut == statut.Value);
            }

            if (!string.IsNullOrWhiteSpace(Recherche))
            {
                var search = Recherche.ToLower();
                filtered = filtered.Where(f =>
                    f.NumeroFacture.ToLower().Contains(search) ||
                    f.ClientNom.ToLower().Contains(search));
            }

            Factures.Clear();
            foreach (var facture in filtered)
            {
                Factures.Add(facture);
            }

            // Update statistics
            NombreFactures = factures.Count;
            ChiffreAffaires = factures.Where(f => f.Statut != StatutFacture.Annulee).Sum(f => f.MontantTotal);
            FacturesEnAttente = factures.Count(f => f.Statut == StatutFacture.EnAttente);
            FacturesPayees = factures.Count(f => f.Statut == StatutFacture.Payee);
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur lors du chargement : {ex.Message}";
        }
        finally
        {
            EstChargement = false;
        }
    }

    partial void OnRechercheChanged(string value) => _ = ChargerFacturesAsync();
    partial void OnTypeFactureIndexChanged(int value) => _ = ChargerFacturesAsync();
    partial void OnStatutIndexChanged(int value) => _ = ChargerFacturesAsync();

    [RelayCommand]
    private void GoBack()
    {
        BackRequested?.Invoke();
    }

    [RelayCommand]
    private void EditBusiness()
    {
        EditBusinessRequested?.Invoke();
    }

    [RelayCommand]
    private void CreateInvoice()
    {
        CreateInvoiceRequested?.Invoke();
    }

    [RelayCommand]
    private void EditInvoice(Facture facture)
    {
        EditInvoiceRequested?.Invoke(facture);
    }

    [RelayCommand]
    private void PreviewInvoice(Facture facture)
    {
        PreviewInvoiceRequested?.Invoke(facture);
    }

    [RelayCommand]
    private async Task DeleteInvoiceAsync(Facture facture)
    {
        try
        {
            await _databaseService.DeleteFactureAsync(facture.Id);
            Factures.Remove(facture);
            NombreFactures--;
            if (facture.Statut == StatutFacture.EnAttente) FacturesEnAttente--;
            if (facture.Statut == StatutFacture.Payee) FacturesPayees--;
            if (facture.Statut != StatutFacture.Annulee) ChiffreAffaires -= facture.MontantTotal;
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur lors de la suppression : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TogglePaymentStatusAsync(Facture facture)
    {
        try
        {
            var newStatus = facture.Statut == StatutFacture.Payee 
                ? StatutFacture.EnAttente 
                : StatutFacture.Payee;
            
            await _databaseService.UpdateStatutFactureAsync(facture.Id, newStatus);
            facture.Statut = newStatus;
            
            // Update statistics
            if (newStatus == StatutFacture.Payee)
            {
                FacturesPayees++;
                FacturesEnAttente--;
            }
            else
            {
                FacturesPayees--;
                FacturesEnAttente++;
            }
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur : {ex.Message}";
        }
    }
}
