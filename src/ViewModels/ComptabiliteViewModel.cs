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

public partial class ComptabiliteViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private int _businessId;
    private bool _suppressFilters;

    // All transactions (unfiltered)
    private List<Transaction> _toutesTransactions = new();

    // Displayed transactions
    public ObservableCollection<Transaction> Transactions { get; } = new();

    // Categories
    public ObservableCollection<CategorieTransaction> Categories { get; } = new();
    public ObservableCollection<string> CategoriesFiltre { get; } = new();
    public ObservableCollection<string> CategoriesNoms { get; } = new();

    // Date filters
    [ObservableProperty]
    private DateTimeOffset _dateDebut = new DateTimeOffset(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));

    [ObservableProperty]
    private DateTimeOffset _dateFin = new DateTimeOffset(DateTime.Now);

    // Quick filter: 0=Ce mois, 1=Mois dernier, 2=Cette année, 3=Personnalisé
    [ObservableProperty]
    private int _periodeIndex;

    // Journal filter: 0=Tous, 1=Recettes, 2=Dépenses
    [ObservableProperty]
    private int _typeFiltre;

    // Category filter
    [ObservableProperty]
    private string _categorieFiltre = "Toutes";

    // Show archived toggle
    [ObservableProperty]
    private bool _afficherArchivees;

    // Statistics
    [ObservableProperty]
    private decimal _chiffreAffaires;

    [ObservableProperty]
    private decimal _depenses;

    [ObservableProperty]
    private decimal _beneficeNet;

    // Transaction form
    [ObservableProperty]
    private bool _afficherFormulaire;

    [ObservableProperty]
    private bool _estModeEdition;

    [ObservableProperty]
    private string _titreFormulaire = "Nouvelle transaction";

    [ObservableProperty]
    private DateTimeOffset _formDate = new DateTimeOffset(DateTime.Now);

    [ObservableProperty]
    private string _formDescription = string.Empty;

    [ObservableProperty]
    private decimal _formMontant;

    [ObservableProperty]
    private int _formTypeIndex; // 0=Recette, 1=Dépense

    [ObservableProperty]
    private string _formCategorie = string.Empty;

    partial void OnFormTypeIndexChanged(int value)
    {
        MettreAJourCategoriesNoms();
        FormCategorie = string.Empty;
    }

    private void MettreAJourCategoriesNoms()
    {
        var type = (TypeTransaction)FormTypeIndex;
        CategoriesNoms.Clear();
        foreach (var cat in Categories.Where(c => c.Type == type))
            CategoriesNoms.Add(cat.Nom);
    }

    [ObservableProperty]
    private string? _erreurMessage;

    [ObservableProperty]
    private bool _estChargement;

    // Category form
    [ObservableProperty]
    private bool _afficherFormulaireCategorie;

    [ObservableProperty]
    private string _nouvelleCategorieNom = string.Empty;

    [ObservableProperty]
    private int _nouvelleCategorieTypeIndex; // 0=Recette, 1=Dépense

    private int _editingTransactionId;

    // Navigation
    public event Action? BackRequested;

    public ComptabiliteViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
    }

    public void SetBusinessId(int businessId)
    {
        _businessId = businessId;
    }

    public async Task ChargerDonneesAsync()
    {
        EstChargement = true;
        ErreurMessage = null;

        try
        {
            _suppressFilters = true;

            // Load categories
            var categories = await _databaseService.GetCategoriesByBusinessIdAsync(_businessId);
            Categories.Clear();
            CategoriesFiltre.Clear();
            CategoriesNoms.Clear();
            CategoriesFiltre.Add("Toutes");
            foreach (var cat in categories)
            {
                Categories.Add(cat);
                CategoriesFiltre.Add(cat.Nom);
            }
            CategorieFiltre = "Toutes";
            MettreAJourCategoriesNoms();

            // Load all transactions
            _toutesTransactions = await _databaseService.GetTransactionsByBusinessIdAsync(_businessId);

            _suppressFilters = false;
            AppliquerFiltres();
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur lors du chargement : {ex.Message}";
        }
        finally
        {
            EstChargement = false;
        }
    }

    private void AppliquerFiltres()
    {
        var filtered = _toutesTransactions.AsEnumerable();

        // Show archived or active
        if (AfficherArchivees)
            filtered = filtered.Where(t => t.IsArchived);
        else
            filtered = filtered.Where(t => !t.IsArchived);

        // Date range filter
        var debut = DateDebut.Date;
        var fin = DateFin.Date;
        filtered = filtered.Where(t => t.Date.Date >= debut && t.Date.Date <= fin);

        // Type filter
        if (TypeFiltre == 1)
            filtered = filtered.Where(t => t.Type == TypeTransaction.Recette);
        else if (TypeFiltre == 2)
            filtered = filtered.Where(t => t.Type == TypeTransaction.Depense);

        // Category filter
        if (CategorieFiltre != "Toutes")
            filtered = filtered.Where(t => t.Categorie == CategorieFiltre);

        var list = filtered.ToList();

        Transactions.Clear();
        foreach (var t in list)
            Transactions.Add(t);

        // Statistics based on date range — always exclude archived (canceled)
        var dateFiltered = _toutesTransactions
            .Where(t => !t.IsArchived && t.Date.Date >= debut && t.Date.Date <= fin)
            .ToList();

        ChiffreAffaires = dateFiltered
            .Where(t => t.Type == TypeTransaction.Recette)
            .Sum(t => t.Montant);
        Depenses = dateFiltered
            .Where(t => t.Type == TypeTransaction.Depense)
            .Sum(t => t.Montant);
        BeneficeNet = ChiffreAffaires - Depenses;
    }

    // Quick period filters
    partial void OnPeriodeIndexChanged(int value)
    {
        var now = DateTime.Now;
        switch (value)
        {
            case 0: // Ce mois
                DateDebut = new DateTimeOffset(new DateTime(now.Year, now.Month, 1));
                DateFin = new DateTimeOffset(now);
                break;
            case 1: // Mois dernier
                var moisDernier = now.AddMonths(-1);
                DateDebut = new DateTimeOffset(new DateTime(moisDernier.Year, moisDernier.Month, 1));
                DateFin = new DateTimeOffset(new DateTime(moisDernier.Year, moisDernier.Month, DateTime.DaysInMonth(moisDernier.Year, moisDernier.Month)));
                break;
            case 2: // Cette année
                DateDebut = new DateTimeOffset(new DateTime(now.Year, 1, 1));
                DateFin = new DateTimeOffset(now);
                break;
            case 3: // Personnalisé - don't change dates
                break;
        }
        AppliquerFiltres();
    }

    partial void OnDateDebutChanged(DateTimeOffset value) { if (!_suppressFilters) AppliquerFiltres(); }
    partial void OnDateFinChanged(DateTimeOffset value) { if (!_suppressFilters) AppliquerFiltres(); }
    partial void OnTypeFiltreChanged(int value) { if (!_suppressFilters) AppliquerFiltres(); }
    partial void OnCategorieFiltreChanged(string value) { if (!_suppressFilters) AppliquerFiltres(); }
    partial void OnAfficherArchiveesChanged(bool value) { if (!_suppressFilters) AppliquerFiltres(); }

    // Commands
    [RelayCommand]
    private void GoBack()
    {
        BackRequested?.Invoke();
    }

    [RelayCommand]
    private void NouvelleTransaction()
    {
        _editingTransactionId = 0;
        EstModeEdition = false;
        TitreFormulaire = "Nouvelle transaction";
        FormDate = new DateTimeOffset(DateTime.Now);
        FormDescription = string.Empty;
        FormMontant = 0;
        FormTypeIndex = 0;
        FormCategorie = string.Empty;
        AfficherFormulaire = true;
    }

    [RelayCommand]
    private void ModifierTransaction(Transaction transaction)
    {
        _editingTransactionId = transaction.Id;
        EstModeEdition = true;
        TitreFormulaire = "Modifier la transaction";
        FormDate = new DateTimeOffset(transaction.Date);
        FormDescription = transaction.Description;
        FormMontant = transaction.Montant;
        FormTypeIndex = (int)transaction.Type;
        FormCategorie = transaction.Categorie;
        AfficherFormulaire = true;
    }

    [RelayCommand]
    private async Task EnregistrerTransactionAsync()
    {
        if (string.IsNullOrWhiteSpace(FormDescription))
        {
            ErreurMessage = "La description est obligatoire";
            return;
        }
        if (FormMontant <= 0)
        {
            ErreurMessage = "Le montant doit être supérieur à 0";
            return;
        }
        if (string.IsNullOrWhiteSpace(FormCategorie))
        {
            ErreurMessage = "La catégorie est obligatoire";
            return;
        }

        try
        {
            var transaction = new Transaction
            {
                Id = _editingTransactionId,
                BusinessId = _businessId,
                Date = FormDate.DateTime,
                Description = FormDescription,
                Montant = FormMontant,
                Type = (TypeTransaction)FormTypeIndex,
                Categorie = FormCategorie,
            };

            await _databaseService.SaveTransactionAsync(transaction);
            AfficherFormulaire = false;
            ErreurMessage = null;
            
            _suppressFilters = true;
            // Ensure date range includes the new transaction
            var txDate = new DateTimeOffset(transaction.Date);
            if (txDate < DateDebut) DateDebut = txDate;
            if (txDate > DateFin) DateFin = txDate;
            
            // Reset journal filters to show all
            TypeFiltre = 0;
            CategorieFiltre = "Toutes";
            _suppressFilters = false;
            
            await ChargerDonneesAsync();
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    private void AnnulerFormulaire()
    {
        AfficherFormulaire = false;
        ErreurMessage = null;
    }

    [RelayCommand]
    private async Task ArchiverTransactionAsync(Transaction transaction)
    {
        try
        {
            await _databaseService.ArchiveTransactionAsync(transaction.Id);
            await ChargerDonneesAsync();
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur : {ex.Message}";
        }
    }

    // Category management
    [RelayCommand]
    private void NouvelleCategorie()
    {
        NouvelleCategorieNom = string.Empty;
        NouvelleCategorieTypeIndex = 0;
        AfficherFormulaireCategorie = true;
    }

    [RelayCommand]
    private async Task EnregistrerCategorieAsync()
    {
        if (string.IsNullOrWhiteSpace(NouvelleCategorieNom))
        {
            ErreurMessage = "Le nom de la catégorie est obligatoire";
            return;
        }

        try
        {
            var categorie = new CategorieTransaction
            {
                BusinessId = _businessId,
                Nom = NouvelleCategorieNom.Trim(),
                Type = (TypeTransaction)NouvelleCategorieTypeIndex
            };

            await _databaseService.SaveCategorieAsync(categorie);
            AfficherFormulaireCategorie = false;
            ErreurMessage = null;
            await ChargerDonneesAsync();
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    private void AnnulerCategorie()
    {
        AfficherFormulaireCategorie = false;
    }

    [RelayCommand]
    private async Task SupprimerCategorieAsync(CategorieTransaction categorie)
    {
        try
        {
            await _databaseService.DeleteCategorieAsync(categorie.Id);
            await ChargerDonneesAsync();
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur : {ex.Message}";
        }
    }
}
