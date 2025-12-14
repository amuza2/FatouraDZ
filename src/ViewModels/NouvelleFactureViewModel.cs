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

    [ObservableProperty]
    private string? _clientActivite;

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

    // Erreurs de validation par champ
    [ObservableProperty]
    private string? _erreurClientNom;

    [ObservableProperty]
    private string? _erreurClientAdresse;

    [ObservableProperty]
    private string? _erreurClientTelephone;

    [ObservableProperty]
    private string? _erreurClientEmail;

    [ObservableProperty]
    private string? _erreurDateEcheance;

    [ObservableProperty]
    private string? _erreurLignes;

    [ObservableProperty]
    private bool _estModeEdition;

    [ObservableProperty]
    private string _titreFormulaire = "Nouvelle facture";

    // ID de la facture en cours d'édition (0 = nouvelle facture)
    private int _factureId;

    // Modes de paiement disponibles
    public string[] ModesPaiement { get; } = new[]
    {
        "Espèces",
        "Chèque",
        "Virement bancaire",
        "Carte bancaire",
        "CCP",
        "BaridiMob",
        "À terme"
    };

    // Détails du paiement
    [ObservableProperty]
    private string? _paiementReference;

    // Indique si les détails de paiement sont requis pour le mode sélectionné
    public bool RequiertDetailsPaiement => ModePaiement != "Espèces" && ModePaiement != "À terme";

    partial void OnModePaiementChanged(string value)
    {
        OnPropertyChanged(nameof(RequiertDetailsPaiement));
        if (!RequiertDetailsPaiement)
        {
            PaiementReference = null;
        }
        
        // Timbre fiscal uniquement pour paiement en espèces
        AppliquerTimbre = value == "Espèces";
    }

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

    private void EffacerErreursValidation()
    {
        ErreurClientNom = null;
        ErreurClientAdresse = null;
        ErreurClientTelephone = null;
        ErreurClientEmail = null;
        ErreurDateEcheance = null;
        ErreurLignes = null;
        ErreurMessage = null;
    }

    private bool ValiderChamps()
    {
        EffacerErreursValidation();
        bool estValide = true;

        // Validation du nom client
        if (string.IsNullOrWhiteSpace(ClientNom))
        {
            ErreurClientNom = "Le nom du client est obligatoire";
            estValide = false;
        }

        // Validation de l'adresse client
        if (string.IsNullOrWhiteSpace(ClientAdresse))
        {
            ErreurClientAdresse = "L'adresse du client est obligatoire";
            estValide = false;
        }

        // Validation du téléphone client
        if (string.IsNullOrWhiteSpace(ClientTelephone))
        {
            ErreurClientTelephone = "Le téléphone du client est obligatoire";
            estValide = false;
        }
        else if (!_validationService.EstTelephoneValide(ClientTelephone))
        {
            ErreurClientTelephone = "Format invalide (mobile: 05/06/07XX XX XX XX)";
            estValide = false;
        }

        // Validation de l'email (optionnel mais doit être valide si renseigné)
        if (!string.IsNullOrWhiteSpace(ClientEmail) && !ClientEmail.Contains("@"))
        {
            ErreurClientEmail = "Format d'email invalide";
            estValide = false;
        }

        // Validation de la date d'échéance
        if (DateEcheance < DateFacture)
        {
            ErreurDateEcheance = "La date d'échéance doit être postérieure à la date de facture";
            estValide = false;
        }

        // Validation des lignes
        var lignesValides = Lignes.Any(l => !string.IsNullOrWhiteSpace(l.Designation) && l.Quantite > 0);
        if (!lignesValides)
        {
            ErreurLignes = "Au moins une ligne avec désignation et quantité est requise";
            estValide = false;
        }

        return estValide;
    }

    [RelayCommand]
    private async Task PrevisualiserAsync()
    {
        if (!ValiderChamps())
        {
            ErreurMessage = "Veuillez corriger les erreurs avant de prévisualiser";
            return;
        }

        var facture = CreerFacture();
        ErreurMessage = null;
        DemanderPrevisualisation?.Invoke(facture);
    }

    [RelayCommand]
    private async Task SauvegarderAsync()
    {
        EstSauvegarde = false;

        if (!ValiderChamps())
        {
            ErreurMessage = "Veuillez corriger les erreurs avant de sauvegarder";
            return;
        }

        var facture = CreerFacture();

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
        _factureId = 0;
        EstModeEdition = false;
        TitreFormulaire = "Nouvelle facture";
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
        ClientActivite = null;
        AppliquerTimbre = true;
        PaiementReference = null;
        ErreurMessage = null;
        EstSauvegarde = false;

        Lignes.Clear();
        AjouterLigne();
        RecalculerTotaux();
    }

    public void ChargerFacture(Facture facture, bool estDuplication = false)
    {
        if (estDuplication)
        {
            // Duplication: nouvelle facture avec données copiées
            _factureId = 0;
            EstModeEdition = false;
            TitreFormulaire = "Dupliquer la facture";
            // Le numéro sera généré automatiquement via InitialiserAsync
        }
        else
        {
            // Édition: modifier la facture existante
            _factureId = facture.Id;
            EstModeEdition = true;
            TitreFormulaire = $"Modifier la facture {facture.NumeroFacture}";
            NumeroFacture = facture.NumeroFacture;
        }

        // Charger les données de la facture
        TypeFactureIndex = (int)facture.TypeFacture;
        DateFacture = new DateTimeOffset(facture.DateFacture);
        DateEcheance = new DateTimeOffset(facture.DateEcheance);
        ModePaiement = facture.ModePaiement;
        PaiementReference = facture.PaiementReference;
        ClientNom = facture.ClientNom;
        ClientAdresse = facture.ClientAdresse;
        ClientTelephone = facture.ClientTelephone;
        ClientEmail = facture.ClientEmail;
        ClientRc = facture.ClientRC;
        ClientNis = facture.ClientNIS;
        ClientAi = facture.ClientAI;
        ClientNumeroImmatriculation = facture.ClientNumeroImmatriculation;
        ClientActivite = facture.ClientActivite;
        AppliquerTimbre = facture.EstTimbreApplique;

        // Charger les lignes
        Lignes.Clear();
        foreach (var ligne in facture.Lignes.OrderBy(l => l.NumeroLigne))
        {
            var ligneVm = LigneFactureViewModel.FromModel(ligne);
            ligneVm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LigneFactureViewModel.TotalHT) ||
                    e.PropertyName == nameof(LigneFactureViewModel.TauxTVA))
                {
                    RecalculerTotaux();
                }
            };
            Lignes.Add(ligneVm);
        }

        // S'assurer qu'il y a au moins une ligne
        if (Lignes.Count == 0)
        {
            AjouterLigne();
        }

        RecalculerTotaux();
        ErreurMessage = null;
        EstSauvegarde = false;
    }

    private Facture CreerFacture()
    {
        RecalculerTotaux();

        var facture = new Facture
        {
            Id = _factureId,
            NumeroFacture = NumeroFacture,
            DateFacture = DateFacture.DateTime,
            DateEcheance = DateEcheance.DateTime,
            TypeFacture = (TypeFacture)TypeFactureIndex,
            ModePaiement = ModePaiement,
            PaiementReference = RequiertDetailsPaiement ? PaiementReference : null,
            ClientNom = ClientNom,
            ClientAdresse = ClientAdresse,
            ClientTelephone = ClientTelephone,
            ClientEmail = ClientEmail,
            ClientRC = ClientRc,
            ClientNIS = ClientNis,
            ClientAI = ClientAi,
            ClientNumeroImmatriculation = ClientNumeroImmatriculation,
            ClientActivite = ClientActivite,
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
