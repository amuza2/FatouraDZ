using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        ContenuActuel = vm;
    }

    [RelayCommand]
    private void AfficherHistorique()
    {
        PageActuelle = "Historique";
        // ContenuActuel = new HistoriqueViewModel(); // À implémenter
    }
}
