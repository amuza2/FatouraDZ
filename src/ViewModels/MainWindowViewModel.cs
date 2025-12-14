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
    private string _pageActuelle = "Accueil";

    [ObservableProperty]
    private bool _estEntrepreneurConfigure;

    private readonly IDatabaseService _databaseService;

    public event Action<Facture, Entrepreneur>? DemanderPrevisualisation;

    public MainWindowViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
    }

    public async Task InitialiserAsync()
    {
        await _databaseService.InitializeDatabaseAsync();
        
        var entrepreneur = await _databaseService.GetEntrepreneurAsync();
        EstEntrepreneurConfigure = entrepreneur != null;

        if (!EstEntrepreneurConfigure)
        {
            // Première utilisation : afficher la configuration
            AfficherConfiguration();
        }
        else
        {
            AfficherAccueil();
        }
    }

    [RelayCommand]
    private void AfficherAccueil()
    {
        PageActuelle = "Accueil";
        var vm = new AccueilViewModel();
        vm.DemanderNouvelleFacture += () => AfficherNouvelleFacture();
        vm.DemanderHistorique += () => AfficherHistorique();
        _ = vm.ChargerDonneesAsync();
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherConfiguration()
    {
        PageActuelle = "Configuration";
        var vm = new EntrepreneurConfigViewModel();
        vm.ConfigurationSauvegardee += async () =>
        {
            EstEntrepreneurConfigure = true;
            await Task.Delay(1500); // Laisser le message de succès visible
            AfficherAccueil();
        };
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherNouvelleFacture()
    {
        PageActuelle = "Nouvelle facture";
        var vm = new NouvelleFactureViewModel();
        vm.FactureSauvegardee += async () =>
        {
            await Task.Delay(1500);
            AfficherNouvelleFacture(); // Réinitialiser pour une nouvelle facture
        };
        vm.DemanderPrevisualisation += async (facture) =>
        {
            var entrepreneur = await _databaseService.GetEntrepreneurAsync();
            if (entrepreneur != null)
            {
                DemanderPrevisualisation?.Invoke(facture, entrepreneur);
            }
        };
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherHistorique()
    {
        PageActuelle = "Historique";
        var vm = new HistoriqueFacturesViewModel();
        vm.DemanderModification += (facture) =>
        {
            AfficherEditionFacture(facture, estDuplication: false);
        };
        vm.DemanderDuplication += (facture) =>
        {
            AfficherEditionFacture(facture, estDuplication: true);
        };
        vm.DemanderPrevisualisation += (facture, entrepreneur) =>
        {
            DemanderPrevisualisation?.Invoke(facture, entrepreneur);
        };
        vm.DemanderCheminSauvegarde += DemanderCheminSauvegardePdf;
        vm.DemanderConfirmation += DemanderConfirmationAsync;
        _ = vm.ChargerDonneesAsync();
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherArchives()
    {
        PageActuelle = "Archives";
        var vm = new ArchiveFacturesViewModel();
        vm.DemanderConfirmation += DemanderConfirmationAsync;
        vm.DemanderPrevisualisation += (facture, entrepreneur) =>
        {
            DemanderPrevisualisation?.Invoke(facture, entrepreneur);
        };
        _ = vm.ChargerDonneesAsync();
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherAPropos()
    {
        PageActuelle = "À propos";
        ContenuActuel = new AProposViewModel();
    }

    public event Func<string, string, Task<bool>>? DemanderConfirmationDialog;

    private async Task<bool> DemanderConfirmationAsync(string titre, string message)
    {
        return DemanderConfirmationDialog != null 
            ? await DemanderConfirmationDialog.Invoke(titre, message) 
            : true;
    }

    public void AfficherEditionFacture(Facture facture, bool estDuplication)
    {
        PageActuelle = estDuplication ? "Dupliquer facture" : "Modifier facture";
        var vm = new NouvelleFactureViewModel();
        vm.ChargerFacture(facture, estDuplication);
        
        if (estDuplication)
        {
            // Générer un nouveau numéro pour la duplication
            _ = vm.InitialiserAsync();
        }
        
        vm.FactureSauvegardee += () =>
        {
            // Retourner à l'historique après sauvegarde
            AfficherHistorique();
        };
        vm.DemanderPrevisualisation += async (f) =>
        {
            var entrepreneur = await _databaseService.GetEntrepreneurAsync();
            if (entrepreneur != null)
            {
                DemanderPrevisualisation?.Invoke(f, entrepreneur);
            }
        };
        ContenuActuel = vm;
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
