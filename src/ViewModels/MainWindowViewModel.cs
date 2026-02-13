using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _contenuActuel;

    [ObservableProperty]
    private string _pageActuelle = "Entreprises";

    private readonly IDatabaseService _databaseService;
    
    // Current business context for dynamic sidebar
    [ObservableProperty]
    private Business? _currentBusiness;

    // Sidebar visibility based on context
    public bool EstDansContexteEntreprise => CurrentBusiness != null;
    public bool EstHorsContexteEntreprise => CurrentBusiness == null;

    partial void OnCurrentBusinessChanged(Business? value)
    {
        OnPropertyChanged(nameof(EstDansContexteEntreprise));
        OnPropertyChanged(nameof(EstHorsContexteEntreprise));
    }

    public event Action<Facture, Business>? DemanderPrevisualisation;

    public MainWindowViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
    }

    public async Task InitialiserAsync()
    {
        await _databaseService.InitializeDatabaseAsync();
        AfficherListeEntreprises();
    }

    [RelayCommand]
    private void AfficherListeEntreprises()
    {
        PageActuelle = "Entreprises";
        CurrentBusiness = null;
        var vm = new BusinessListViewModel();
        vm.BusinessSelected += AfficherDetailEntreprise;
        vm.CreateBusinessRequested += AfficherFormulaireEntreprise;
        _ = vm.ChargerBusinessesAsync();
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherTableauDeBord()
    {
        if (CurrentBusiness == null) return;
        AfficherDetailEntreprise(CurrentBusiness);
    }

    [RelayCommand]
    private void AfficherFactures()
    {
        if (CurrentBusiness == null) return;
        AfficherDetailEntreprise(CurrentBusiness);
    }

    [RelayCommand]
    private void AfficherClients()
    {
        if (CurrentBusiness == null) return;
        PageActuelle = "Clients";
        var vm = new ClientListViewModel();
        vm.SetBusiness(CurrentBusiness);
        vm.BackRequested += () => AfficherDetailEntreprise(CurrentBusiness);
        _ = vm.ChargerClientsAsync();
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherComptabilite()
    {
        if (CurrentBusiness == null) return;
        PageActuelle = "Comptabilité";
        var vm = new ComptabiliteViewModel();
        vm.SetBusinessId(CurrentBusiness.Id);
        vm.BackRequested += () => AfficherDetailEntreprise(CurrentBusiness);
        _ = vm.ChargerDonneesAsync();
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherInfosEntreprise()
    {
        if (CurrentBusiness == null) return;
        AfficherFormulaireEntreprise(CurrentBusiness);
    }

    private void AfficherDetailEntreprise(Business business)
    {
        CurrentBusiness = business;
        PageActuelle = business.Nom;
        var vm = new BusinessDetailViewModel();
        vm.SetBusiness(business);
        vm.BackRequested += AfficherListeEntreprises;
        vm.EditBusinessRequested += () => AfficherFormulaireEntreprise(business);
        vm.CreateInvoiceRequested += () => AfficherNouvelleFacture(business);
        vm.EditInvoiceRequested += (facture) => AfficherEditionFacture(facture, business, false);
        vm.PreviewInvoiceRequested += (facture) => DemanderPrevisualisation?.Invoke(facture, business);
        _ = vm.ChargerFacturesAsync();
        ContenuActuel = vm;
    }

    private void AfficherFormulaireEntreprise()
    {
        PageActuelle = "Nouvelle entreprise";
        var vm = new BusinessFormViewModel();
        vm.BusinessSaved += async () =>
        {
            await Task.Delay(1000);
            AfficherListeEntreprises();
        };
        vm.CancelRequested += AfficherListeEntreprises;
        ContenuActuel = vm;
    }

    private void AfficherFormulaireEntreprise(Business business)
    {
        PageActuelle = $"Modifier {business.Nom}";
        var vm = new BusinessFormViewModel();
        vm.ChargerBusiness(business);
        vm.BusinessSaved += async () =>
        {
            await Task.Delay(1000);
            // Reload business and show detail
            var updated = await _databaseService.GetBusinessByIdAsync(business.Id);
            if (updated != null)
                AfficherDetailEntreprise(updated);
            else
                AfficherListeEntreprises();
        };
        vm.CancelRequested += () => AfficherDetailEntreprise(business);
        ContenuActuel = vm;
    }

    private void AfficherNouvelleFacture(Business business)
    {
        PageActuelle = "Nouvelle facture";
        var vm = new NouvelleFactureViewModel();
        vm.SetBusiness(business);
        _ = vm.InitialiserAsync();
        vm.FactureSauvegardee += () =>
        {
            AfficherDetailEntreprise(business);
        };
        vm.DemanderPrevisualisation += (facture) =>
        {
            DemanderPrevisualisation?.Invoke(facture, business);
        };
        vm.AnnulerDemande += () => AfficherDetailEntreprise(business);
        ContenuActuel = vm;
    }

    private void AfficherEditionFacture(Facture facture, Business business, bool estDuplication)
    {
        PageActuelle = estDuplication ? "Dupliquer facture" : "Modifier facture";
        var vm = new NouvelleFactureViewModel();
        vm.SetBusiness(business);
        vm.ChargerFacture(facture, estDuplication);
        
        if (estDuplication)
        {
            _ = vm.InitialiserAsync();
        }
        else
        {
            _ = vm.InitialiserEditionAsync();
        }
        
        vm.FactureSauvegardee += () =>
        {
            AfficherDetailEntreprise(business);
        };
        vm.DemanderPrevisualisation += (f) =>
        {
            DemanderPrevisualisation?.Invoke(f, business);
        };
        vm.AnnulerDemande += () => AfficherDetailEntreprise(business);
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherAPropos()
    {
        PageActuelle = "À propos";
        ContenuActuel = new AProposViewModel();
    }

    [RelayCommand]
    private void AfficherAide()
    {
        PageActuelle = "Aide";
        var vm = new SettingsViewModel();
        vm.SelectedTabIndex = 2; // Go to "À propos" tab which contains help info
        vm.BackRequested += AfficherListeEntreprises;
        vm.DemanderExportFichier += async (nomFichier, description) =>
        {
            return DemanderExportFichier != null 
                ? await DemanderExportFichier.Invoke(nomFichier, description) 
                : null;
        };
        vm.DemanderImportFichier += async () =>
        {
            return DemanderImportFichier != null 
                ? await DemanderImportFichier.Invoke() 
                : null;
        };
        vm.DemanderDossier += async () =>
        {
            return DemanderDossier != null 
                ? await DemanderDossier.Invoke() 
                : null;
        };
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherParametres()
    {
        PageActuelle = "Paramètres";
        var vm = new SettingsViewModel();
        vm.BackRequested += AfficherListeEntreprises;
        vm.DemanderExportFichier += async (nomFichier, description) =>
        {
            return DemanderExportFichier != null 
                ? await DemanderExportFichier.Invoke(nomFichier, description) 
                : null;
        };
        vm.DemanderImportFichier += async () =>
        {
            return DemanderImportFichier != null 
                ? await DemanderImportFichier.Invoke() 
                : null;
        };
        vm.DemanderDossier += async () =>
        {
            return DemanderDossier != null 
                ? await DemanderDossier.Invoke() 
                : null;
        };
        ContenuActuel = vm;
    }

    public event Func<string, string, Task<bool>>? DemanderConfirmationDialog;

    private async Task<bool> DemanderConfirmationAsync(string titre, string message)
    {
        return DemanderConfirmationDialog != null 
            ? await DemanderConfirmationDialog.Invoke(titre, message) 
            : true;
    }

    public event Func<string, Task<Avalonia.Platform.Storage.IStorageFile?>>? DemanderSauvegardeFichier;

    private async Task<Avalonia.Platform.Storage.IStorageFile?> DemanderCheminSauvegardePdf(string nomFichier)
    {
        return DemanderSauvegardeFichier != null ? await DemanderSauvegardeFichier.Invoke(nomFichier) : null;
    }

    // Gestion des données - Export/Import
    [ObservableProperty]
    private string? _messageGestionDonnees;

    public event Func<string, string, Task<Avalonia.Platform.Storage.IStorageFile?>>? DemanderExportFichier;
    public event Func<Task<Avalonia.Platform.Storage.IStorageFile?>>? DemanderImportFichier;
    public event Func<Task<Avalonia.Platform.Storage.IStorageFolder?>>? DemanderDossier;

    [RelayCommand]
    private async Task ExporterBaseDeDonneesAsync()
    {
        try
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FatouraDZ", "fatouradz.db");

            if (!File.Exists(dbPath))
            {
                MessageGestionDonnees = "Aucune base de données à exporter.";
                return;
            }

            var nomFichier = $"fatouradz_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var fichier = DemanderExportFichier != null 
                ? await DemanderExportFichier.Invoke(nomFichier, "Base de données SQLite") 
                : null;

            if (fichier != null)
            {
                var destPath = fichier.Path.LocalPath;
                File.Copy(dbPath, destPath, overwrite: true);
                MessageGestionDonnees = $"Base de données exportée avec succès vers {destPath}";
            }
        }
        catch (Exception ex)
        {
            MessageGestionDonnees = $"Erreur lors de l'export : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImporterBaseDeDonneesAsync()
    {
        try
        {
            // Demander confirmation avant import
            var confirme = await DemanderConfirmationAsync(
                "Importer une base de données",
                "Attention : cette opération remplacera toutes vos données actuelles.\n\nÊtes-vous sûr de vouloir continuer ?");

            if (!confirme) return;

            var fichier = DemanderImportFichier != null 
                ? await DemanderImportFichier.Invoke() 
                : null;

            if (fichier != null)
            {
                var sourcePath = fichier.Path.LocalPath;
                var dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FatouraDZ", "fatouradz.db");

                // Créer une sauvegarde avant import
                var backupPath = dbPath + ".backup";
                if (File.Exists(dbPath))
                {
                    File.Copy(dbPath, backupPath, overwrite: true);
                }

                File.Copy(sourcePath, dbPath, overwrite: true);
                MessageGestionDonnees = "Base de données importée avec succès. Redémarrez l'application pour appliquer les changements.";
                
                // Recharger les données
                await InitialiserAsync();
            }
        }
        catch (Exception ex)
        {
            MessageGestionDonnees = $"Erreur lors de l'import : {ex.Message}";
        }
    }
}
