using CommunityToolkit.Mvvm.ComponentModel;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class LigneFactureViewModel : ObservableObject
{
    private readonly ICalculationService _calculationService;

    [ObservableProperty]
    private int _numeroLigne;

    [ObservableProperty]
    private string _designation = string.Empty;

    [ObservableProperty]
    private decimal _quantite = 1;

    [ObservableProperty]
    private decimal _prixUnitaire;

    [ObservableProperty]
    private int _tauxTVAIndex = 0;

    public TauxTVA TauxTVA => (TauxTVA)TauxTVAIndex;

    [ObservableProperty]
    private decimal _totalHT;

    public LigneFactureViewModel()
    {
        _calculationService = ServiceLocator.CalculationService;
    }

    public LigneFactureViewModel(int numeroLigne) : this()
    {
        NumeroLigne = numeroLigne;
    }

    partial void OnQuantiteChanged(decimal value)
    {
        CalculerTotalHT();
    }

    partial void OnPrixUnitaireChanged(decimal value)
    {
        CalculerTotalHT();
    }

    partial void OnTauxTVAIndexChanged(int value)
    {
        OnPropertyChanged(nameof(TauxTVA));
    }

    private void CalculerTotalHT()
    {
        TotalHT = _calculationService.CalculerTotalHTLigne(Quantite, PrixUnitaire);
    }

    public LigneFacture ToModel()
    {
        return new LigneFacture
        {
            NumeroLigne = NumeroLigne,
            Designation = Designation,
            Quantite = Quantite,
            PrixUnitaire = PrixUnitaire,
            TauxTVA = (TauxTVA)TauxTVAIndex,
            TotalHT = TotalHT
        };
    }

    public static LigneFactureViewModel FromModel(LigneFacture ligne)
    {
        return new LigneFactureViewModel
        {
            NumeroLigne = ligne.NumeroLigne,
            Designation = ligne.Designation,
            Quantite = ligne.Quantite,
            PrixUnitaire = ligne.PrixUnitaire,
            TauxTVAIndex = (int)ligne.TauxTVA,
            TotalHT = ligne.TotalHT
        };
    }
}
