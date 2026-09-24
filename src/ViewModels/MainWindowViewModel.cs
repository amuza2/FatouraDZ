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

    /// <summary>Version de l'exécutable installé, affichée en pied de barre latérale.</summary>
    public string VersionApplication => AppInfo.Version;

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

        // Après l'affichage : une vérification de mise à jour ne doit pas retarder
        // l'ouverture de l'application.
        _ = VerifierMiseAJourAuDemarrageAsync();
    }

    #region Mises à jour

    /// <summary>Version plus récente publiée, ou null si l'application est à jour.</summary>
    [ObservableProperty]
    private VersionDisponible? _miseAJourDisponible;

    public bool MiseAJourVisible => MiseAJourDisponible != null;

    partial void OnMiseAJourDisponibleChanged(VersionDisponible? value) =>
        OnPropertyChanged(nameof(MiseAJourVisible));

    /// <summary>
    /// Interroge GitHub au démarrage, au plus une fois par jour et seulement si
    /// l'utilisateur l'a autorisé. Ne bloque jamais l'application : tout échec est
    /// silencieux (il est journalisé).
    /// </summary>
    public async Task VerifierMiseAJourAuDemarrageAsync()
    {
        try
        {
            var parametres = AppSettings.Instance;
            if (!parametres.VerifierMisesAJour)
                return;

            if (parametres.DerniereVerificationMiseAJour is { } derniere &&
                DateTime.Now - derniere < TimeSpan.FromHours(24))
            {
                return;
            }

            var disponible = await ServiceLocator.UpdateService.VerifierAsync();

            parametres.DerniereVerificationMiseAJour = DateTime.Now;
            parametres.Save();

            if (disponible != null &&
                !string.Equals(disponible.Version, parametres.VersionMiseAJourIgnoree, StringComparison.OrdinalIgnoreCase))
            {
                MiseAJourDisponible = disponible;
            }
        }
        catch (Exception ex)
        {
            ServiceLocator.Logger.Warning("Vérification des mises à jour impossible", ex);
        }
    }

    [RelayCommand]
    private void OuvrirPageMiseAJour()
    {
        LiensExternes.Ouvrir(MiseAJourDisponible?.PageHtml ?? AppInfo.PageReleases);
    }

    /// <summary>Masque le bandeau pour cette version : elle ne sera plus proposée.</summary>
    [RelayCommand]
    private void IgnorerCetteVersion()
    {
        if (MiseAJourDisponible == null)
            return;

        try
        {
            var parametres = AppSettings.Instance;
            parametres.VersionMiseAJourIgnoree = MiseAJourDisponible.Version;
            parametres.Save();
        }
        catch (Exception ex)
        {
            ServiceLocator.Logger.Warning("Impossible d'enregistrer la version ignorée", ex);
        }

        MiseAJourDisponible = null;
    }

    #endregion

    [RelayCommand]
    private void AfficherListeEntreprises() => NaviguerAvecGarde(AfficherListeEntreprisesImmediat);

    private void AfficherListeEntreprisesImmediat()
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

        var business = CurrentBusiness;
        NaviguerAvecGarde(() =>
        {
            PageActuelle = "Clients";
            var vm = new ClientListViewModel();
            vm.SetBusiness(business);
            vm.BackRequested += () => AfficherDetailEntreprise(business);
            _ = vm.ChargerClientsAsync();
            ContenuActuel = vm;
        });
    }

    [RelayCommand]
    private void AfficherComptabilite()
    {
        if (CurrentBusiness == null) return;

        var business = CurrentBusiness;
        NaviguerAvecGarde(() =>
        {
            PageActuelle = "Comptabilité";
            var vm = new ComptabiliteViewModel();
            vm.SetBusinessId(business.Id);
            vm.BackRequested += () => AfficherDetailEntreprise(business);
            _ = vm.ChargerDonneesAsync();
            ContenuActuel = vm;
        });
    }

    [RelayCommand]
    private void AfficherInfosEntreprise()
    {
        if (CurrentBusiness == null) return;
        AfficherFormulaireEntreprise(CurrentBusiness);
    }

    private void AfficherDetailEntreprise(Business business) =>
        NaviguerAvecGarde(() => AfficherDetailEntrepriseImmediat(business));

    private void AfficherDetailEntrepriseImmediat(Business business)
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

    private void AfficherFormulaireEntreprise() =>
        NaviguerAvecGarde(AfficherFormulaireEntrepriseImmediat);

    private void AfficherFormulaireEntrepriseImmediat()
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

    private void AfficherFormulaireEntreprise(Business business) =>
        NaviguerAvecGarde(() => AfficherFormulaireEntrepriseImmediat(business));

    private void AfficherFormulaireEntrepriseImmediat(Business business)
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

    private void AfficherNouvelleFacture(Business business) =>
        NaviguerAvecGarde(() => AfficherNouvelleFactureImmediat(business));

    private void AfficherNouvelleFactureImmediat(Business business)
    {
        PageActuelle = "Nouvelle facture";
        var vm = new NouvelleFactureViewModel();
        vm.SetBusiness(business);
        vm.DemanderConfirmation += DemanderConfirmationAsync;
        _ = vm.InitialiserAsync();
        vm.FactureSauvegardee += () =>
        {
            AfficherDetailEntreprise(business);
        };
        vm.DemanderPrevisualisation += (facture) =>
        {
            DemanderPrevisualisation?.Invoke(facture, business);
        };
        // Le formulaire a déjà demandé confirmation avant d'émettre AnnulerDemande.
        vm.AnnulerDemande += () => AfficherDetailEntrepriseImmediat(business);
        ContenuActuel = vm;
    }

    private void AfficherEditionFacture(Facture facture, Business business, bool estDuplication) =>
        NaviguerAvecGarde(() => AfficherEditionFactureImmediat(facture, business, estDuplication));

    private void AfficherEditionFactureImmediat(Facture facture, Business business, bool estDuplication)
    {
        PageActuelle = estDuplication ? "Dupliquer facture" : "Modifier facture";
        var vm = new NouvelleFactureViewModel();
        vm.SetBusiness(business);
        vm.DemanderConfirmation += DemanderConfirmationAsync;
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
        // Le formulaire a déjà demandé confirmation avant d'émettre AnnulerDemande.
        vm.AnnulerDemande += () => AfficherDetailEntrepriseImmediat(business);
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherAPropos() => NaviguerAvecGarde(AfficherAProposImmediat);

    private void AfficherAProposImmediat()
    {
        PageActuelle = "À propos";
        ContenuActuel = new AProposViewModel();
    }

    [RelayCommand]
    private void AfficherAide() => NaviguerAvecGarde(AfficherAideImmediat);

    private void AfficherAideImmediat()
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
    private void AfficherParametres() => NaviguerAvecGarde(AfficherParametresImmediat);

    private void AfficherParametresImmediat()
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

    /// <summary>
    /// Vrai si le contenu affiché est un formulaire de facture contenant des saisies non enregistrées.
    /// </summary>
    public bool EstFormulaireFactureModifie => ContenuActuel is NouvelleFactureViewModel { EstModifie: true };

    /// <summary>
    /// Demande confirmation avant d'abandonner la facture en cours de saisie.
    /// Retourne true s'il n'y a rien à perdre ou si l'utilisateur confirme la perte.
    /// Utilisé également par la fenêtre principale avant fermeture.
    /// </summary>
    public async Task<bool> ConfirmerAbandonSaisieAsync()
    {
        if (ContenuActuel is not NouvelleFactureViewModel facture || !facture.EstModifie)
            return true;

        return await facture.ConfirmerAbandonAsync();
    }

    /// <summary>
    /// Exécute une navigation après vérification des modifications non enregistrées :
    /// une saisie de facture ne doit jamais être perdue silencieusement.
    /// </summary>
    private void NaviguerAvecGarde(Action navigation) => _ = NaviguerAvecGardeAsync(navigation);

    private async Task NaviguerAvecGardeAsync(Action navigation)
    {
        if (!await ConfirmerAbandonSaisieAsync())
            return;

        navigation();
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
