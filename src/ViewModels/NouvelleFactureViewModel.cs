using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class NouvelleFactureViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ICalculationService _calculationService;
    private readonly IInvoiceNumberService _invoiceNumberService;
    private readonly INumberToWordsService _numberToWordsService;
    private readonly IValidationService _validationService;

    // Type de facture
    [ObservableProperty]
    private int _typeFactureIndex = 0;

    public TypeFacture TypeFacture => (TypeFacture)TypeFactureIndex;

    // Informations facture
    [ObservableProperty]
    private string _numeroFacture = string.Empty;

    [ObservableProperty]
    private DateTimeOffset _dateFacture = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private DateTimeOffset _dateEcheance = DateTimeOffset.Now.Date.AddDays(30);

    [ObservableProperty]
    private string _modePaiement = "Espèces";

    // Informations client
    [ObservableProperty]
    private string _clientNom = string.Empty;

    [ObservableProperty]
    private string _clientAdresse = string.Empty;

    [ObservableProperty]
    private string _clientTelephone = string.Empty;

    [ObservableProperty]
    private string? _clientEmail;

    [ObservableProperty]
    private string? _clientRc;

    [ObservableProperty]
    private string? _clientNis;

    [ObservableProperty]
    private string? _clientAi;

    [ObservableProperty]
    private string? _clientNumeroImmatriculation;

    // Lignes de facture
    public ObservableCollection<LigneFactureViewModel> Lignes { get; } = new();

    // Totaux
    [ObservableProperty]
    private decimal _totalHT;

    [ObservableProperty]
    private decimal _totalTVA19;

    [ObservableProperty]
    private decimal _totalTVA9;

    [ObservableProperty]
    private decimal _totalTTC;

    [ObservableProperty]
    private decimal _timbreFiscal;

    [ObservableProperty]
    private decimal _montantTotal;

    [ObservableProperty]
    private string _montantEnLettres = string.Empty;

    // Options
    [ObservableProperty]
    private bool _appliquerTimbre = true;

    // États
    [ObservableProperty]
    private string? _erreurMessage;

    [ObservableProperty]
    private bool _estSauvegarde;

    // Modes de paiement disponibles
    public string[] ModesPaiement { get; } = new[]
    {
        "Espèces",
        "Chèque",
        "Virement bancaire",
        "Carte bancaire"
    };

    public event Action? FactureSauvegardee;
    public event Action<Facture>? DemanderPrevisualisation;

    public NouvelleFactureViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
        _calculationService = ServiceLocator.CalculationService;
        _invoiceNumberService = ServiceLocator.InvoiceNumberService;
        _numberToWordsService = ServiceLocator.NumberToWordsService;
        _validationService = ServiceLocator.ValidationService;

        // Ajouter une première ligne vide
        AjouterLigne();
    }

    public async Task InitialiserAsync()
    {
        NumeroFacture = await _invoiceNumberService.GenererProchainNumeroAsync();
    }

    [RelayCommand]
    private void AjouterLigne()
    {
        var nouvelleLigne = new LigneFactureViewModel(Lignes.Count + 1);
        nouvelleLigne.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LigneFactureViewModel.TotalHT) ||
                e.PropertyName == nameof(LigneFactureViewModel.TauxTVA))
            {
                RecalculerTotaux();
            }
        };
        Lignes.Add(nouvelleLigne);
    }

    [RelayCommand]
    private void SupprimerLigne(LigneFactureViewModel ligne)
    {
        if (Lignes.Count > 1)
        {
            Lignes.Remove(ligne);
            // Renuméroter les lignes
            for (int i = 0; i < Lignes.Count; i++)
            {
                Lignes[i].NumeroLigne = i + 1;
            }
            RecalculerTotaux();
        }
    }

    private void RecalculerTotaux()
    {
        var lignesModel = Lignes.Select(l => l.ToModel()).ToList();
        var totaux = _calculationService.CalculerTotaux(lignesModel, AppliquerTimbre);

        TotalHT = totaux.TotalHT;
        TotalTVA19 = totaux.TVA19;
        TotalTVA9 = totaux.TVA9;
        TotalTTC = totaux.TotalTTC;
        TimbreFiscal = totaux.TimbreFiscal;
        MontantTotal = totaux.MontantTotal;
        MontantEnLettres = _numberToWordsService.ConvertirEnLettres(MontantTotal);
    }

    partial void OnAppliquerTimbreChanged(bool value)
    {
        RecalculerTotaux();
    }

    [RelayCommand]
    private async Task PrevisualiserAsync()
    {
        var facture = CreerFacture();
        var validation = _validationService.ValiderFacture(facture);

        if (!validation.EstValide)
        {
            ErreurMessage = string.Join("\n", validation.Erreurs);
            return;
        }

        ErreurMessage = null;
        DemanderPrevisualisation?.Invoke(facture);
    }

    [RelayCommand]
    private async Task SauvegarderAsync()
    {
        ErreurMessage = null;
        EstSauvegarde = false;

        var facture = CreerFacture();
        var validation = _validationService.ValiderFacture(facture);

        if (!validation.EstValide)
        {
            ErreurMessage = string.Join("\n", validation.Erreurs);
            return;
        }

        try
        {
            await _databaseService.SaveFactureAsync(facture);
            EstSauvegarde = true;
            FactureSauvegardee?.Invoke();
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur lors de la sauvegarde : {ex.Message}";
        }
    }

    [RelayCommand]
    private void Reinitialiser()
    {
        TypeFactureIndex = 0;
        DateFacture = DateTimeOffset.Now.Date;
        DateEcheance = DateTimeOffset.Now.Date.AddDays(30);
        ModePaiement = "Espèces";
        ClientNom = string.Empty;
        ClientAdresse = string.Empty;
        ClientTelephone = string.Empty;
        ClientEmail = null;
        ClientRc = null;
        ClientNis = null;
        ClientAi = null;
        ClientNumeroImmatriculation = null;
        AppliquerTimbre = true;
        ErreurMessage = null;
        EstSauvegarde = false;

        Lignes.Clear();
        AjouterLigne();
        RecalculerTotaux();
    }

    private Facture CreerFacture()
    {
        RecalculerTotaux();

        var facture = new Facture
        {
            NumeroFacture = NumeroFacture,
            DateFacture = DateFacture.DateTime,
            DateEcheance = DateEcheance.DateTime,
            TypeFacture = (TypeFacture)TypeFactureIndex,
            ModePaiement = ModePaiement,
            ClientNom = ClientNom,
            ClientAdresse = ClientAdresse,
            ClientTelephone = ClientTelephone,
            ClientEmail = ClientEmail,
            ClientRC = ClientRc,
            ClientNIS = ClientNis,
            ClientAI = ClientAi,
            ClientNumeroImmatriculation = ClientNumeroImmatriculation,
            TotalHT = TotalHT,
            TotalTVA19 = TotalTVA19,
            TotalTVA9 = TotalTVA9,
            TotalTTC = TotalTTC,
            TimbreFiscal = TimbreFiscal,
            EstTimbreApplique = AppliquerTimbre,
            MontantTotal = MontantTotal,
            MontantEnLettres = MontantEnLettres,
            Lignes = Lignes.Select(l => l.ToModel()).ToList()
        };

        return facture;
    }
}
