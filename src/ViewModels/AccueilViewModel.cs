using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class AccueilViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private string _nomEntrepreneur = string.Empty;

    [ObservableProperty]
    private int _nombreFactures;

    [ObservableProperty]
    private decimal _chiffreAffairesTotal;

    [ObservableProperty]
    private decimal _chiffreAffairesMois;

    [ObservableProperty]
    private int _nombreFacturesMois;

    [ObservableProperty]
    private bool _estChargement = true;

    public ObservableCollection<Facture> DernieresFactures { get; } = new();

    public event Action? DemanderNouvelleFacture;
    public event Action? DemanderHistorique;

    public AccueilViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
    }

    public async Task ChargerDonneesAsync()
    {
        EstChargement = true;

        try
        {
            // Charger les informations de l'entrepreneur
            var entrepreneur = await _databaseService.GetEntrepreneurAsync();
            if (entrepreneur != null)
            {
                NomEntrepreneur = !string.IsNullOrEmpty(entrepreneur.RaisonSociale) 
                    ? entrepreneur.RaisonSociale 
                    : entrepreneur.NomComplet;
            }

            // Statistiques globales
            NombreFactures = await _databaseService.GetNombreFacturesAsync();
            ChiffreAffairesTotal = await _databaseService.GetChiffreAffairesTotalAsync();

            // Statistiques du mois en cours
            var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var facturesMois = await _databaseService.GetFacturesAsync(debutMois, null, null, null, null);
            var facturesMoisActives = facturesMois.Where(f => f.Statut != StatutFacture.Archivee).ToList();

            NombreFacturesMois = facturesMoisActives.Count;
            ChiffreAffairesMois = facturesMoisActives.Sum(f => f.MontantTotal);

            // 5 dernières factures
            DernieresFactures.Clear();
            var dernieres = await _databaseService.GetDernieresFacturesAsync(5);

            foreach (var facture in dernieres)
            {
                DernieresFactures.Add(facture);
            }
        }
        finally
        {
            EstChargement = false;
        }
    }

    [RelayCommand]
    private void NouvelleFacture()
    {
        DemanderNouvelleFacture?.Invoke();
    }

    [RelayCommand]
    private void VoirHistorique()
    {
        DemanderHistorique?.Invoke();
    }
}
