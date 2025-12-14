using System;
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
        ContenuActuel = null; // Sera remplacé par AccueilViewModel plus tard
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
}
