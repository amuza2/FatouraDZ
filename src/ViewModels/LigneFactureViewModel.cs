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
    private string? _reference;

    [ObservableProperty]
    private string _designation = string.Empty;

    [ObservableProperty]
    private decimal _quantite = 1;

    [ObservableProperty]
    private int _uniteIndex = 0;

    public Unite Unite => (Unite)UniteIndex;

    [ObservableProperty]
    private decimal _prixUnitaire;

    [ObservableProperty]
    private int _tauxTVAIndex = 0;

    public TauxTVA TauxTVA => (TauxTVA)TauxTVAIndex;

    // Discount fields
    [ObservableProperty]
    private decimal _remise;

    [ObservableProperty]
    private int _typeRemiseIndex = 0;

    public TypeRemise TypeRemise => (TypeRemise)TypeRemiseIndex;

    [ObservableProperty]
    private decimal _montantRemise;

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

    partial void OnRemiseChanged(decimal value)
    {
        CalculerTotalHT();
    }

    partial void OnTypeRemiseIndexChanged(int value)
    {
        OnPropertyChanged(nameof(TypeRemise));
        CalculerTotalHT();
    }

    partial void OnTauxTVAIndexChanged(int value)
    {
        OnPropertyChanged(nameof(TauxTVA));
    }

    private void CalculerTotalHT()
    {
        var (totalHT, montantRemise) = _calculationService.CalculerTotalHTLigneAvecRemise(Quantite, PrixUnitaire, Remise, TypeRemise);
        MontantRemise = montantRemise;
        TotalHT = totalHT;
    }

    public LigneFacture ToModel()
    {
        return new LigneFacture
        {
            NumeroLigne = NumeroLigne,
            Reference = Reference,
            Designation = Designation,
            Quantite = Quantite,
            Unite = (Unite)UniteIndex,
            PrixUnitaire = PrixUnitaire,
            TauxTVA = (TauxTVA)TauxTVAIndex,
            Remise = Remise,
            TypeRemise = (TypeRemise)TypeRemiseIndex,
            MontantRemise = MontantRemise,
            TotalHT = TotalHT
        };
    }

    public static LigneFactureViewModel FromModel(LigneFacture ligne)
    {
        return new LigneFactureViewModel
        {
            NumeroLigne = ligne.NumeroLigne,
            Reference = ligne.Reference,
            Designation = ligne.Designation,
            Quantite = ligne.Quantite,
            UniteIndex = (int)ligne.Unite,
            PrixUnitaire = ligne.PrixUnitaire,
            TauxTVAIndex = (int)ligne.TauxTVA,
            Remise = ligne.Remise,
            TypeRemiseIndex = (int)ligne.TypeRemise,
            MontantRemise = ligne.MontantRemise,
            TotalHT = ligne.TotalHT
        };
    }
}
