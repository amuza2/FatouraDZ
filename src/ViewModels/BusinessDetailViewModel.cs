using System;
using System.Collections.Generic;
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
    private Business _business = new Business();

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

    [ObservableProperty]
    private int _archiveFilterIndex = 0; // 0 = Active, 1 = Archived

    // Year filter
    public ObservableCollection<int> AnneesDisponibles { get; } = new();

    [ObservableProperty]
    private int _anneeSelectionnee = DateTime.Now.Year;

    // Dynamic button properties based on archive filter
    public string ArchiveButtonContent => ArchiveFilterIndex == 0 ? "📦" : "♻️";
    public string ArchiveButtonTooltip => ArchiveFilterIndex == 0 ? "Archiver" : "Restaurer";
    public string ArchiveButtonBackground => ArchiveFilterIndex == 0 ? "#FEF3C7" : "#D1FAE5";

    // Confirmation dialog
    [ObservableProperty]
    private bool _showConfirmDialog;

    [ObservableProperty]
    private string _confirmDialogMessage = string.Empty;

    private Facture? _pendingArchiveFacture;

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
            
            // Update available years from all invoices
            MettreAJourAnneesDisponibles(factures);
            
            // Filter by selected year
            var yearFiltered = factures.Where(f => f.DateFacture.Year == AnneeSelectionnee).ToList();
            
            // Apply archive filter
            var showArchived = ArchiveFilterIndex == 1;
            var filtered = yearFiltered.Where(f => f.IsArchived == showArchived);

            if (TypeFactureIndex > 0)
            {
                var type = TypeFactureIndex switch
                {
                    1 => TypeFacture.Normale,
                    2 => TypeFacture.Avoir,
                    3 => TypeFacture.Proforma,
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

            // Update statistics for selected year (all invoices in that year, not just filtered by type/status)
            NombreFactures = yearFiltered.Count;
            ChiffreAffaires = yearFiltered.Where(f => f.Statut != StatutFacture.Annulee && !f.IsArchived).Sum(f => f.MontantTotal);
            FacturesEnAttente = yearFiltered.Count(f => f.Statut == StatutFacture.EnAttente && !f.IsArchived);
            FacturesPayees = yearFiltered.Count(f => f.Statut == StatutFacture.Payee && !f.IsArchived);
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

    private void MettreAJourAnneesDisponibles(List<Facture> factures)
    {
        var years = factures
            .Select(f => f.DateFacture.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();
        
        // Always include current year
        if (!years.Contains(DateTime.Now.Year))
            years.Insert(0, DateTime.Now.Year);
        
        // Only update if the list changed
        var currentYears = AnneesDisponibles.ToList();
        if (!years.SequenceEqual(currentYears))
        {
            var selectedYear = AnneeSelectionnee;
            AnneesDisponibles.Clear();
            foreach (var year in years.OrderByDescending(y => y))
                AnneesDisponibles.Add(year);
            
            // Restore selection (or default to current year)
            if (AnneesDisponibles.Contains(selectedYear))
                AnneeSelectionnee = selectedYear;
            else
                AnneeSelectionnee = AnneesDisponibles.First();
        }
    }

    partial void OnRechercheChanged(string value) => _ = ChargerFacturesAsync();
    partial void OnTypeFactureIndexChanged(int value) => _ = ChargerFacturesAsync();
    partial void OnStatutIndexChanged(int value) => _ = ChargerFacturesAsync();
    partial void OnAnneeSelectionneeChanged(int value) => _ = ChargerFacturesAsync();
    partial void OnArchiveFilterIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ArchiveButtonContent));
        OnPropertyChanged(nameof(ArchiveButtonTooltip));
        OnPropertyChanged(nameof(ArchiveButtonBackground));
        _ = ChargerFacturesAsync();
    }

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
    private void ToggleArchiveInvoice(Facture facture)
    {
        _pendingArchiveFacture = facture;
        var action = ArchiveFilterIndex == 0 ? "archiver" : "restaurer";
        ConfirmDialogMessage = $"Voulez-vous {action} la facture {facture.NumeroFacture} ?";
        ShowConfirmDialog = true;
    }

    [RelayCommand]
    private async Task ConfirmArchiveAsync()
    {
        if (_pendingArchiveFacture == null) return;
        
        try
        {
            _pendingArchiveFacture.IsArchived = !_pendingArchiveFacture.IsArchived;
            await _databaseService.SaveFactureAsync(_pendingArchiveFacture);
            Factures.Remove(_pendingArchiveFacture);
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur : {ex.Message}";
        }
        finally
        {
            ShowConfirmDialog = false;
            _pendingArchiveFacture = null;
        }
    }

    [RelayCommand]
    private void CancelArchive()
    {
        ShowConfirmDialog = false;
        _pendingArchiveFacture = null;
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
            
            // Auto-create/remove recette transaction
            if (newStatus == StatutFacture.Payee)
            {
                // Create recette transaction for paid invoice
                var existingTransaction = await _databaseService.GetTransactionByFactureIdAsync(facture.Id);
                if (existingTransaction == null)
                {
                    var transaction = new Transaction
                    {
                        BusinessId = Business.Id,
                        Date = facture.DateFacture,
                        Description = $"Facture {facture.NumeroFacture} - {facture.ClientNom}",
                        Montant = facture.MontantTotal,
                        Type = TypeTransaction.Recette,
                        Categorie = "Ventes",
                        FactureId = facture.Id,
                        NumeroFacture = facture.NumeroFacture
                    };
                    await _databaseService.SaveTransactionAsync(transaction);
                }
                
                FacturesPayees++;
                FacturesEnAttente--;
            }
            else
            {
                // Remove auto-created recette when unmarking as paid
                var existingTransaction = await _databaseService.GetTransactionByFactureIdAsync(facture.Id);
                if (existingTransaction != null)
                {
                    await _databaseService.DeleteTransactionAsync(existingTransaction.Id);
                }
                
                FacturesPayees--;
                FacturesEnAttente++;
            }
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur : {ex.Message}";
        }
    }

    public event Action<Facture>? ConvertToInvoiceRequested;

    [RelayCommand]
    private async Task ConvertToInvoiceAsync(Facture proforma)
    {
        try
        {
            // Duplicate the proforma as a normal invoice
            var newInvoice = await _databaseService.DupliquerFactureAsync(proforma.Id);
            if (newInvoice != null)
            {
                // Change type to Normale and clear validity date
                newInvoice.TypeFacture = TypeFacture.Normale;
                newInvoice.DateValidite = null;
                newInvoice.NumeroFactureOrigine = proforma.NumeroFacture;
                newInvoice.DateFacture = DateTime.Today;
                newInvoice.DateEcheance = DateTime.Today.AddDays(30);
                
                await _databaseService.SaveFactureAsync(newInvoice);
                await ChargerFacturesAsync();
            }
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur lors de la conversion : {ex.Message}";
        }
    }
}
