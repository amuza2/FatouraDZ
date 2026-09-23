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

    // Numéro de version de la dernière requête de filtrage (ignore les réponses obsolètes)
    private int _versionFiltres;

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

    // Pagination
    [ObservableProperty]
    private int _pageActuelle = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalResultats;

    [ObservableProperty]
    private int _taillePage = 15;

    public int[] TaillePageOptions { get; } = [10, 15, 25, 50];

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

            _suppressFilters = false;
            await AppliquerFiltresAsync();
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

    private async Task AppliquerFiltresAsync()
    {
        // Seule la dernière requête lancée peut appliquer son résultat
        // (une réponse plus lente ne doit pas écraser une plus récente).
        var version = ++_versionFiltres;

        try
        {
            var debut = DateDebut.Date;
            var fin = DateFin.Date;

            TypeTransaction? type = TypeFiltre switch
            {
                1 => TypeTransaction.Recette,
                2 => TypeTransaction.Depense,
                _ => null
            };

            var categorie = CategorieFiltre == "Toutes" ? null : CategorieFiltre;

            // Comptage et totaux calculés côté base de données.
            var total = await _databaseService.GetNombreTransactionsFiltreesAsync(
                _businessId, AfficherArchivees, debut, fin, type, categorie);

            if (version != _versionFiltres) return;

            TotalResultats = total;
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)TaillePage));

            if (PageActuelle > TotalPages)
            {
                PageActuelle = TotalPages; // déclenche un nouveau chargement
                return;
            }

            // Une seule page est rapatriée depuis la base.
            var page = await _databaseService.GetTransactionsFiltreesAsync(
                _businessId, AfficherArchivees, debut, fin, type, categorie,
                (PageActuelle - 1) * TaillePage, TaillePage);

            var (recettes, depenses) = await _databaseService.GetTotauxTransactionsAsync(_businessId, debut, fin);

            if (version != _versionFiltres) return;

            Transactions.Clear();
            foreach (var t in page)
                Transactions.Add(t);

            ChiffreAffaires = recettes;
            Depenses = depenses;
            BeneficeNet = recettes - depenses;
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur lors du chargement : {ex.Message}";
        }
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
        _ = AppliquerFiltresAsync();
    }

    partial void OnDateDebutChanged(DateTimeOffset value) { if (!_suppressFilters) { PageActuelle = 1; _ = AppliquerFiltresAsync(); } }
    partial void OnDateFinChanged(DateTimeOffset value) { if (!_suppressFilters) { PageActuelle = 1; _ = AppliquerFiltresAsync(); } }
    partial void OnTypeFiltreChanged(int value) { if (!_suppressFilters) { PageActuelle = 1; _ = AppliquerFiltresAsync(); } }
    partial void OnCategorieFiltreChanged(string value) { if (!_suppressFilters) { PageActuelle = 1; _ = AppliquerFiltresAsync(); } }
    partial void OnAfficherArchiveesChanged(bool value) { if (!_suppressFilters) { PageActuelle = 1; _ = AppliquerFiltresAsync(); } }
    partial void OnPageActuelleChanged(int value) { if (!_suppressFilters) _ = AppliquerFiltresAsync(); }
    partial void OnTaillePageChanged(int value) { if (!_suppressFilters) { PageActuelle = 1; _ = AppliquerFiltresAsync(); } }

    [RelayCommand]
    private void PagePrecedente()
    {
        if (PageActuelle > 1) PageActuelle--;
    }

    [RelayCommand]
    private void PageSuivante()
    {
        if (PageActuelle < TotalPages) PageActuelle++;
    }

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
