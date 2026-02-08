using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
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

    private int _businessId;

    public void SetBusiness(Business business)
    {
        _businessId = business.Id;
    }

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
    private DateTimeOffset _dateValidite = DateTimeOffset.Now.Date.AddDays(30);

    // Show validity date only for Proforma invoices
    public bool AfficherDateValidite => TypeFacture == TypeFacture.Proforma;
    
    // Show original invoice reference only for Avoir
    public bool AfficherReferenceOrigine => TypeFacture == TypeFacture.Avoir;

    partial void OnTypeFactureIndexChanged(int value)
    {
        OnPropertyChanged(nameof(TypeFacture));
        OnPropertyChanged(nameof(AfficherDateValidite));
        OnPropertyChanged(nameof(AfficherReferenceOrigine));
        
        // Load available invoices when switching to Avoir
        if (TypeFacture == TypeFacture.Avoir)
        {
            _ = ChargerFacturesDisponiblesAsync();
        }
    }

    private async Task ChargerFacturesDisponiblesAsync()
    {
        FacturesDisponibles.Clear();
        
        if (_businessId <= 0) return;
        
        var factures = await _databaseService.GetFacturesByBusinessIdAsync(_businessId);
        
        // Only show regular invoices (not Avoir or Proforma) that are not cancelled
        var facturesValides = factures
            .Where(f => f.TypeFacture == TypeFacture.Normale && f.Statut != StatutFacture.Annulee)
            .OrderByDescending(f => f.DateFacture)
            .ToList();
        
        foreach (var facture in facturesValides)
        {
            FacturesDisponibles.Add(facture);
        }
    }

    private async Task ChargerFacturesDisponiblesEtSelectionnerAsync(string numeroFactureOrigine)
    {
        await ChargerFacturesDisponiblesAsync();
        
        // Select the matching invoice
        FactureOrigineSelectionnee = FacturesDisponibles
            .FirstOrDefault(f => f.NumeroFacture == numeroFactureOrigine);
    }

    [ObservableProperty]
    private string _modePaiement = "Espèces";

    // Liste des clients existants
    public ObservableCollection<Client> ClientsDisponibles { get; } = new();
    public ObservableCollection<Client> TousLesClients { get; } = new();

    [ObservableProperty]
    private string _rechercheClient = string.Empty;

    // Show search results dropdown when there's search text and results
    public bool RechercheClientResultatsVisible => 
        !string.IsNullOrWhiteSpace(RechercheClient) && ClientsDisponibles.Count > 0;

    partial void OnRechercheClientChanged(string value)
    {
        FiltrerClients();
        OnPropertyChanged(nameof(RechercheClientResultatsVisible));
    }

    private void FiltrerClients()
    {
        ClientsDisponibles.Clear();
        
        if (string.IsNullOrWhiteSpace(RechercheClient))
        {
            return; // Don't show results when search is empty
        }
        
        var clientsFiltres = TousLesClients.Where(c => 
            c.Nom.Contains(RechercheClient, StringComparison.OrdinalIgnoreCase) ||
            c.Telephone.Contains(RechercheClient, StringComparison.OrdinalIgnoreCase) ||
            (c.Email?.Contains(RechercheClient, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();
        
        foreach (var client in clientsFiltres)
        {
            ClientsDisponibles.Add(client);
        }
        
        OnPropertyChanged(nameof(RechercheClientResultatsVisible));
    }

    [RelayCommand]
    private void SelectionnerClient(Client client)
    {
        ClientSelectionne = client;
        RechercheClient = string.Empty; // Clear search after selection
    }

    [ObservableProperty]
    private Client? _clientSelectionne;

    // Computed property to check if a client is selected
    public bool ClientEstSelectionne => ClientSelectionne != null;

    partial void OnClientSelectionneChanged(Client? value)
    {
        OnPropertyChanged(nameof(ClientEstSelectionne));
        
        if (value != null)
        {
            // Auto-fill client fields from selected client
            ClientBusinessTypeIndex = (int)value.TypeClient;
            ClientNom = value.Nom;
            ClientAdresse = value.Adresse;
            ClientTelephone = value.Telephone;
            ClientEmail = value.Email;
            ClientFax = value.Fax;
            ClientFormeJuridique = value.FormeJuridique;
            ClientRc = value.RC;
            ClientNis = value.NIS;
            ClientNif = value.NIF;
            ClientAi = value.AI;
            ClientNumeroImmatriculation = value.NumeroImmatriculation;
            ClientActivite = value.Activite;
            ClientCapitalSocial = value.CapitalSocial;
        }
    }

    // Informations client
    [ObservableProperty]
    private int _clientBusinessTypeIndex = 0;

    public BusinessType ClientBusinessType => (BusinessType)ClientBusinessTypeIndex;

    // Computed properties for client fields visibility
    public bool ClientAfficherNumeroImmatriculation => ClientBusinessType == BusinessType.AutoEntrepreneur;
    public bool ClientAfficherRC => ClientBusinessType != BusinessType.AutoEntrepreneur;

    partial void OnClientBusinessTypeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ClientBusinessType));
        OnPropertyChanged(nameof(ClientAfficherNumeroImmatriculation));
        OnPropertyChanged(nameof(ClientAfficherRC));
        OnPropertyChanged(nameof(ClientAfficherCapitalSocial));
    }

    [ObservableProperty]
    private string _clientNom = string.Empty;

    [ObservableProperty]
    private string _clientAdresse = string.Empty;

    [ObservableProperty]
    private string _clientTelephone = string.Empty;

    [ObservableProperty]
    private string? _clientEmail;

    [ObservableProperty]
    private string? _clientFormeJuridique;

    [ObservableProperty]
    private string? _clientRc;

    [ObservableProperty]
    private string? _clientNis;

    [ObservableProperty]
    private string? _clientNif;

    [ObservableProperty]
    private string? _clientAi;

    [ObservableProperty]
    private string? _clientNumeroImmatriculation;

    [ObservableProperty]
    private string? _clientActivite;

    [ObservableProperty]
    private string? _clientFax;

    [ObservableProperty]
    private string? _clientCapitalSocial;

    // Show Capital Social only for Reel client type
    public bool ClientAfficherCapitalSocial => ClientBusinessType == BusinessType.Reel;

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

    // Retenue à la source (optionnel)
    [ObservableProperty]
    private bool _appliquerRetenueSource;

    [ObservableProperty]
    private decimal _tauxRetenueSource = 30m;

    [ObservableProperty]
    private decimal _retenueSource;

    // Remise globale sur Total H.T
    [ObservableProperty]
    private decimal _remiseGlobale;

    [ObservableProperty]
    private int _typeRemiseGlobaleIndex = 0;

    public TypeRemise TypeRemiseGlobale => (TypeRemise)TypeRemiseGlobaleIndex;

    // Label for global discount showing percentage if applicable
    public string LibelleRemiseGlobale => TypeRemiseGlobale == TypeRemise.Pourcentage && RemiseGlobale > 0
        ? $"Remise globale ({RemiseGlobale}%)"
        : "Remise globale";

    [ObservableProperty]
    private decimal _montantRemiseGlobale;

    // Référence facture originale (pour avoir/annulation)
    [ObservableProperty]
    private string? _numeroFactureOrigine;

    // Liste des factures disponibles pour sélection (pour avoir)
    public ObservableCollection<Facture> FacturesDisponibles { get; } = new();

    [ObservableProperty]
    private Facture? _factureOrigineSelectionnee;

    partial void OnFactureOrigineSelectionneeChanged(Facture? value)
    {
        if (value != null)
        {
            NumeroFactureOrigine = value.NumeroFacture;
        }
    }

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
        "À terme",
        "Chèque",
        "Virement",
        "Espèces",
        "Versement"
    };

    // Détails du paiement - Mode règlement table
    [ObservableProperty]
    private string? _paiementReference;

    [ObservableProperty]
    private string? _paiementNumeroPiece;

    [ObservableProperty]
    private bool _estPaye = false;

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
    public event Action? AnnulerDemande;

    [RelayCommand]
    private void Annuler()
    {
        AnnulerDemande?.Invoke();
    }

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
        await ChargerClientsDisponiblesAsync();
    }

    public async Task InitialiserEditionAsync()
    {
        Console.WriteLine($"[DEBUG] InitialiserEditionAsync called. BusinessId={_businessId}, ClientNom='{ClientNom}', ClientTelephone='{ClientTelephone}'");
        
        await ChargerClientsDisponiblesAsync();
        
        Console.WriteLine($"[DEBUG] TousLesClients count: {TousLesClients.Count}");
        foreach (var c in TousLesClients)
        {
            Console.WriteLine($"[DEBUG]   Client: '{c.Nom}' / '{c.Telephone}'");
        }
        
        // Try to match the client from invoice data
        if (!string.IsNullOrEmpty(ClientNom))
        {
            // Try exact match first (name + phone)
            var match = TousLesClients.FirstOrDefault(c => 
                string.Equals(c.Nom?.Trim(), ClientNom?.Trim(), StringComparison.OrdinalIgnoreCase) && 
                string.Equals(c.Telephone?.Trim(), ClientTelephone?.Trim(), StringComparison.OrdinalIgnoreCase));
            
            Console.WriteLine($"[DEBUG] Name+Phone match: {match != null}");
            
            // Fallback: match by name only
            match ??= TousLesClients.FirstOrDefault(c => 
                string.Equals(c.Nom?.Trim(), ClientNom?.Trim(), StringComparison.OrdinalIgnoreCase));
            
            Console.WriteLine($"[DEBUG] Final match: {match != null} -> {match?.Nom}");
            
            if (match != null)
            {
                // Defer to next UI frame so ComboBox finishes updating ItemsSource
                Dispatcher.UIThread.Post(() =>
                {
                    ClientSelectionne = match;
                    Console.WriteLine($"[DEBUG] ClientSelectionne set to: {ClientSelectionne?.Nom}");
                });
            }
        }
        else
        {
            Console.WriteLine("[DEBUG] ClientNom is empty - no matching attempted");
        }
    }

    private async Task ChargerClientsDisponiblesAsync()
    {
        TousLesClients.Clear();
        ClientsDisponibles.Clear();
        
        if (_businessId <= 0) return;
        
        var clients = await _databaseService.GetClientsByBusinessIdAsync(_businessId);
        
        foreach (var client in clients)
        {
            TousLesClients.Add(client);
        }
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
        var totaux = _calculationService.CalculerTotaux(lignesModel, AppliquerTimbre, RemiseGlobale, TypeRemiseGlobale);

        TotalHT = totaux.TotalHT;
        TotalTVA19 = totaux.TVA19;
        TotalTVA9 = totaux.TVA9;
        TotalTTC = totaux.TotalTTC;
        TimbreFiscal = totaux.TimbreFiscal;
        MontantRemiseGlobale = totaux.MontantRemiseGlobale;

        // Calculer la retenue à la source si applicable (sur le Total HT, pas sur le TTC)
        if (AppliquerRetenueSource && TauxRetenueSource > 0)
        {
            RetenueSource = Math.Round(totaux.TotalHT * TauxRetenueSource / 100m, 2);
        }
        else
        {
            RetenueSource = 0;
        }

        // Net à payer = Total TTC + Timbre - Retenue source
        MontantTotal = totaux.MontantTotal - RetenueSource;
        MontantEnLettres = _numberToWordsService.ConvertirEnLettres(MontantTotal);
    }

    partial void OnAppliquerTimbreChanged(bool value)
    {
        RecalculerTotaux();
    }

    partial void OnAppliquerRetenueSourceChanged(bool value)
    {
        RecalculerTotaux();
    }

    partial void OnTauxRetenueSourceChanged(decimal value)
    {
        RecalculerTotaux();
    }

    partial void OnRemiseGlobaleChanged(decimal value)
    {
        OnPropertyChanged(nameof(LibelleRemiseGlobale));
        RecalculerTotaux();
    }

    partial void OnTypeRemiseGlobaleIndexChanged(int value)
    {
        OnPropertyChanged(nameof(TypeRemiseGlobale));
        OnPropertyChanged(nameof(LibelleRemiseGlobale));
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

        // Validation du client sélectionné
        if (ClientSelectionne == null)
        {
            ErreurClientNom = "Veuillez sélectionner un client";
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
            
            // Incrémenter le numéro de facture seulement après sauvegarde réussie
            if (!EstModeEdition)
            {
                await _invoiceNumberService.ConfirmerNumeroFactureAsync();
            }
            
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
        ClientBusinessTypeIndex = 0;
        ClientNom = string.Empty;
        ClientAdresse = string.Empty;
        ClientTelephone = string.Empty;
        ClientEmail = null;
        ClientFax = null;
        ClientFormeJuridique = null;
        ClientRc = null;
        ClientNis = null;
        ClientNif = null;
        ClientAi = null;
        ClientNumeroImmatriculation = null;
        ClientActivite = null;
        ClientCapitalSocial = null;
        AppliquerTimbre = true;
        AppliquerRetenueSource = false;
        TauxRetenueSource = 30m;
        RemiseGlobale = 0;
        TypeRemiseGlobaleIndex = 0;
        NumeroFactureOrigine = null;
        FactureOrigineSelectionnee = null;
        FacturesDisponibles.Clear();
        ClientSelectionne = null;
        ClientsDisponibles.Clear();
        PaiementReference = null;
        PaiementNumeroPiece = null;
        EstPaye = false;
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
        PaiementNumeroPiece = facture.PaiementNumeroPiece;
        ClientBusinessTypeIndex = (int)facture.ClientBusinessType;
        ClientNom = facture.ClientNom;
        ClientAdresse = facture.ClientAdresse;
        ClientTelephone = facture.ClientTelephone;
        ClientEmail = facture.ClientEmail;
        ClientFax = facture.ClientFax;
        ClientFormeJuridique = facture.ClientFormeJuridique;
        ClientRc = facture.ClientRC;
        ClientNis = facture.ClientNIS;
        ClientNif = facture.ClientNIF;
        ClientAi = facture.ClientAI;
        ClientNumeroImmatriculation = facture.ClientNumeroImmatriculation;
        ClientActivite = facture.ClientActivite;
        ClientCapitalSocial = facture.ClientCapitalSocial;
        EstPaye = facture.Statut == StatutFacture.Payee;
        NumeroFactureOrigine = facture.NumeroFactureOrigine;
        
        // Load available invoices and select the matching one for Avoir
        if (facture.TypeFacture == TypeFacture.Avoir && !string.IsNullOrEmpty(facture.NumeroFactureOrigine))
        {
            _ = ChargerFacturesDisponiblesEtSelectionnerAsync(facture.NumeroFactureOrigine);
        }
        
        AppliquerTimbre = facture.EstTimbreApplique;
        AppliquerRetenueSource = facture.TauxRetenueSource.HasValue;
        TauxRetenueSource = facture.TauxRetenueSource ?? 30m;
        RemiseGlobale = facture.RemiseGlobale;
        TypeRemiseGlobaleIndex = (int)facture.TypeRemiseGlobale;

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
            BusinessId = _businessId,
            NumeroFacture = NumeroFacture,
            DateFacture = DateFacture.DateTime,
            DateEcheance = DateEcheance.DateTime,
            DateValidite = TypeFacture == TypeFacture.Proforma ? DateValidite.DateTime : null,
            TypeFacture = (TypeFacture)TypeFactureIndex,
            ModePaiement = ModePaiement,
            PaiementReference = RequiertDetailsPaiement ? PaiementReference : null,
            PaiementValeur = MontantTotal,
            PaiementNumeroPiece = RequiertDetailsPaiement ? PaiementNumeroPiece : null,
            Statut = EstPaye ? StatutFacture.Payee : StatutFacture.EnAttente,
            NumeroFactureOrigine = NumeroFactureOrigine,
            ClientBusinessType = (BusinessType)ClientBusinessTypeIndex,
            ClientNom = ClientNom,
            ClientAdresse = ClientAdresse,
            ClientTelephone = ClientTelephone,
            ClientEmail = ClientEmail,
            ClientFax = ClientFax,
            ClientFormeJuridique = ClientFormeJuridique,
            ClientRC = ClientRc,
            ClientNIS = ClientNis,
            ClientNIF = ClientNif,
            ClientAI = ClientAi,
            ClientNumeroImmatriculation = ClientNumeroImmatriculation,
            ClientActivite = ClientActivite,
            ClientCapitalSocial = ClientCapitalSocial,
            TauxRetenueSource = AppliquerRetenueSource ? TauxRetenueSource : null,
            RetenueSource = RetenueSource,
            RemiseGlobale = RemiseGlobale,
            TypeRemiseGlobale = TypeRemiseGlobale,
            MontantRemiseGlobale = MontantRemiseGlobale,
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
