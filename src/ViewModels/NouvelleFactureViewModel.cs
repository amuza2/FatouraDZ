using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
    private DateTimeOffset _dateEcheance = DateTimeOffset.Now.Date.AddDays(AppSettings.Instance.DelaiPaiementDefaut);

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
    private decimal _tauxRetenueSource = AppSettings.Instance.TauxRetenueSourceDefaut;

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

    // Empêche un second enregistrement tant que le premier n'est pas terminé (double-clic).
    [ObservableProperty]
    private bool _estEnregistrementEnCours;

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

    // Client enregistré sur la facture en cours d'édition (lien fiable)
    private int? _clientIdFacture;

    // Journal d'audit de la facture en cours d'édition
    public ObservableCollection<JournalAudit> JournalFacture { get; } = new();

    [ObservableProperty]
    private bool _afficherJournal;

    private async Task ChargerJournalAsync(int factureId)
    {
        try
        {
            var entrees = await _databaseService.GetJournalAsync("Facture", factureId);

            JournalFacture.Clear();
            foreach (var entree in entrees)
                JournalFacture.Add(entree);

            AfficherJournal = JournalFacture.Count > 0;
        }
        catch (Exception ex)
        {
            ServiceLocator.Logger.Warning("Impossible de charger le journal de la facture", ex);
        }
    }

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
    private async Task AnnulerAsync()
    {
        // Ne jamais perdre une saisie sans le demander.
        if (!await ConfirmerAbandonAsync())
            return;

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

        // Toute modification d'un champ réévalue l'indicateur "modifications non enregistrées".
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(EstModifie))
            {
                OnPropertyChanged(nameof(EstModifie));
            }
        };

        MarquerEtatReference();
    }

    #region Garde des modifications non enregistrées

    /// <summary>
    /// Confirmation demandée à l'utilisateur (dialogue). Fournie par la fenêtre principale ;
    /// sans abonné, l'abandon est autorisé (tests, aperçu de design).
    /// </summary>
    public event Func<string, string, Task<bool>>? DemanderConfirmation;

    /// <summary>État de référence du formulaire, utilisé pour détecter les saisies non enregistrées.</summary>
    private string _etatReference = string.Empty;

    /// <summary>Vrai si le formulaire contient des saisies non enregistrées.</summary>
    public bool EstModifie => !string.Equals(CapturerEtat(), _etatReference, StringComparison.Ordinal);

    /// <summary>Prend l'état courant comme référence (chargement, réinitialisation, enregistrement).</summary>
    private void MarquerEtatReference()
    {
        _etatReference = CapturerEtat();
        OnPropertyChanged(nameof(EstModifie));
    }

    /// <summary>
    /// Demande confirmation avant d'abandonner la saisie en cours.
    /// Retourne true s'il n'y a rien à perdre ou si l'utilisateur confirme la perte.
    /// </summary>
    public async Task<bool> ConfirmerAbandonAsync()
    {
        if (!EstModifie || DemanderConfirmation == null)
            return true;

        return await DemanderConfirmation(
            "Modifications non enregistrées",
            "La facture en cours de saisie contient des modifications non enregistrées.\n\n" +
            "Si vous continuez, cette saisie sera définitivement perdue.\n\n" +
            "Voulez-vous continuer sans enregistrer ?");
    }

    /// <summary>
    /// Sérialise les champs modifiables : une comparaison de chaînes suffit à détecter
    /// un changement, sans dépendre de la comparaison d'objets ni d'un horodatage.
    /// </summary>
    private string CapturerEtat()
    {
        var sb = new StringBuilder(512);

        sb.Append(TypeFactureIndex).Append('|')
          .Append(DateFacture).Append('|')
          .Append(DateEcheance).Append('|')
          .Append(DateValidite).Append('|')
          .Append(ModePaiement).Append('|')
          .Append(PaiementReference).Append('|')
          .Append(PaiementNumeroPiece).Append('|')
          .Append(EstPaye).Append('|')
          .Append(NumeroFactureOrigine).Append('|')
          .Append(ClientSelectionne?.Id).Append('|')
          .Append(ClientBusinessTypeIndex).Append('|')
          .Append(ClientNom).Append('|')
          .Append(ClientAdresse).Append('|')
          .Append(ClientTelephone).Append('|')
          .Append(ClientEmail).Append('|')
          .Append(ClientFax).Append('|')
          .Append(ClientFormeJuridique).Append('|')
          .Append(ClientRc).Append('|')
          .Append(ClientNis).Append('|')
          .Append(ClientNif).Append('|')
          .Append(ClientAi).Append('|')
          .Append(ClientNumeroImmatriculation).Append('|')
          .Append(ClientActivite).Append('|')
          .Append(ClientCapitalSocial).Append('|')
          .Append(AppliquerTimbre).Append('|')
          .Append(AppliquerRetenueSource).Append('|')
          .Append(TauxRetenueSource).Append('|')
          .Append(RemiseGlobale).Append('|')
          .Append(TypeRemiseGlobaleIndex).Append('|');

        foreach (var ligne in Lignes)
        {
            sb.Append('#')
              .Append(ligne.Reference).Append('|')
              .Append(ligne.Designation).Append('|')
              .Append(ligne.Quantite).Append('|')
              .Append(ligne.UniteIndex).Append('|')
              .Append(ligne.PrixUnitaire).Append('|')
              .Append(ligne.TauxTVAIndex).Append('|')
              .Append(ligne.Remise).Append('|')
              .Append(ligne.TypeRemiseIndex);
        }

        return sb.ToString();
    }

    #endregion

    public async Task InitialiserAsync()
    {
        // Référence prise sur le formulaire vierge : le numéro généré et la liste des
        // clients ne comptent pas comme des saisies de l'utilisateur.
        MarquerEtatReference();

        NumeroFacture = await _invoiceNumberService.GenererProchainNumeroAsync();
        await ChargerClientsDisponiblesAsync();
    }

    public async Task InitialiserEditionAsync()
    {
        MarquerEtatReference();

        await ChargerClientsDisponiblesAsync();

        if (TousLesClients.Count == 0)
            return;

        // Priorité au lien enregistré sur la facture (fiable), sinon correspondance nom/téléphone.
        var match = _clientIdFacture.HasValue
            ? TousLesClients.FirstOrDefault(c => c.Id == _clientIdFacture.Value)
            : null;

        if (match == null && !string.IsNullOrEmpty(ClientNom))
        {
            // Correspondance exacte d'abord (nom + téléphone)
            match = TousLesClients.FirstOrDefault(c =>
                string.Equals(c.Nom?.Trim(), ClientNom?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.Telephone?.Trim(), ClientTelephone?.Trim(), StringComparison.OrdinalIgnoreCase));

            // Sinon, correspondance par nom uniquement
            match ??= TousLesClients.FirstOrDefault(c =>
                string.Equals(c.Nom?.Trim(), ClientNom?.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (match != null)
        {
            // Différer au frame suivant pour laisser le ComboBox finir de mettre à jour son ItemsSource
            Dispatcher.UIThread.Post(() =>
            {
                ClientSelectionne = match;
                // La fiche client complète les champs : ce n'est pas une saisie utilisateur.
                MarquerEtatReference();
            });
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

            OnPropertyChanged(nameof(EstModifie));
        };
        Lignes.Add(nouvelleLigne);
        OnPropertyChanged(nameof(EstModifie));
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
        MontantEnLettres = ConvertirMontantEnLettres(MontantTotal);
    }

    /// <summary>
    /// Conversion en lettres tolérante aux montants hors limites : un montant saisi par
    /// l'utilisateur ne doit jamais faire planter l'interface.
    /// </summary>
    private string ConvertirMontantEnLettres(decimal montant)
    {
        try
        {
            return _numberToWordsService.ConvertirEnLettres(montant);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            ServiceLocator.Logger.Warning("Montant trop élevé pour la conversion en lettres", ex);
            return "Montant trop élevé pour être converti en lettres";
        }
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

        // Validation du téléphone du client via le service de validation
        if (!string.IsNullOrWhiteSpace(ClientTelephone) && !_validationService.EstTelephoneValide(ClientTelephone))
        {
            ErreurClientTelephone = "Le numéro de téléphone du client est invalide (mobile: 05/06/07XX XX XX XX, fixe: 0XX XX XX XX)";
            estValide = false;
        }

        // Validation de la date d'échéance
        if (DateEcheance < DateFacture)
        {
            ErreurDateEcheance = "La date d'échéance doit être postérieure à la date de facture";
            estValide = false;
        }

        // Validation des lignes renseignées via le service de validation
        var lignesRenseignees = Lignes
            .Where(l => !string.IsNullOrWhiteSpace(l.Designation) || l.Quantite > 0 || l.PrixUnitaire > 0)
            .ToList();

        if (lignesRenseignees.Count == 0)
        {
            ErreurLignes = "Au moins une ligne avec désignation et quantité est requise";
            estValide = false;
        }
        else
        {
            foreach (var ligne in lignesRenseignees)
            {
                var resultatLigne = _validationService.ValiderLigneFacture(ligne.ToModel());
                if (!resultatLigne.EstValide)
                {
                    ErreurLignes = string.Join(" ; ", resultatLigne.Erreurs);
                    estValide = false;
                    break;
                }
            }
        }

        // Le montant doit rester convertible en lettres (évite un dépassement de capacité)
        if (MontantTotal > NumberToWordsService.MontantMaximum)
        {
            ErreurMessage = "Le montant total est trop élevé pour être facturé.";
            estValide = false;
        }

        return estValide;
    }

    [RelayCommand]
    private async Task PrevisualiserAsync()
    {
        if (!ValiderChamps())
        {
            ErreurMessage ??= "Veuillez corriger les erreurs avant de prévisualiser";
            return;
        }

        var facture = CreerFacture();
        ErreurMessage = null;
        DemanderPrevisualisation?.Invoke(facture);
    }

    [RelayCommand]
    private async Task SauvegarderAsync()
    {
        // Un double-clic ne doit pas créer deux factures (et consommer deux numéros).
        if (EstEnregistrementEnCours)
            return;

        EstSauvegarde = false;

        if (!ValiderChamps())
        {
            ErreurMessage ??= "Veuillez corriger les erreurs avant de sauvegarder";
            return;
        }

        var facture = CreerFacture();

        // Contrôle final via le service de validation (règles obligatoires / formats)
        var validation = _validationService.ValiderFacture(facture);
        if (!validation.EstValide)
        {
            ErreurMessage = string.Join(Environment.NewLine, validation.Erreurs);
            return;
        }

        EstEnregistrementEnCours = true;
        try
        {
            // Réserver un numéro unique au moment de la sauvegarde : deux formulaires ouverts
            // en parallèle ne peuvent plus produire le même numéro de facture.
            if (!EstModeEdition)
            {
                facture.NumeroFacture = await _invoiceNumberService.AllouerNumeroFactureAsync();
                NumeroFacture = facture.NumeroFacture;
            }

            await _databaseService.SaveFactureAsync(facture);

            EstSauvegarde = true;
            // La saisie est enregistrée : elle n'est plus considérée comme non sauvegardée.
            MarquerEtatReference();
            FactureSauvegardee?.Invoke();
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur lors de la sauvegarde : {ex.Message}";
        }
        finally
        {
            EstEnregistrementEnCours = false;
        }
    }

    [RelayCommand]
    private async Task ReinitialiserAsync()
    {
        // Réinitialiser efface la saisie : demander confirmation si elle n'est pas enregistrée.
        if (!await ConfirmerAbandonAsync())
            return;

        _factureId = 0;
        _clientIdFacture = null;
        EstModeEdition = false;
        TitreFormulaire = "Nouvelle facture";
        TypeFactureIndex = 0;
        DateFacture = DateTimeOffset.Now.Date;
        DateEcheance = DateTimeOffset.Now.Date.AddDays(AppSettings.Instance.DelaiPaiementDefaut);
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
        TauxRetenueSource = AppSettings.Instance.TauxRetenueSourceDefaut;
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
        MarquerEtatReference();
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
        _clientIdFacture = facture.ClientId;

        // Historique des modifications (audit) pour la facture éditée.
        if (facture.Id > 0)
            _ = ChargerJournalAsync(facture.Id);
        
        // Load available invoices and select the matching one for Avoir
        if (facture.TypeFacture == TypeFacture.Avoir && !string.IsNullOrEmpty(facture.NumeroFactureOrigine))
        {
            _ = ChargerFacturesDisponiblesEtSelectionnerAsync(facture.NumeroFactureOrigine);
        }
        
        AppliquerTimbre = facture.EstTimbreApplique;
        AppliquerRetenueSource = facture.TauxRetenueSource.HasValue;
        TauxRetenueSource = facture.TauxRetenueSource ?? AppSettings.Instance.TauxRetenueSourceDefaut;
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

                OnPropertyChanged(nameof(EstModifie));
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

        // L'état chargé est la référence : tant que rien n'est saisi, le formulaire est propre.
        MarquerEtatReference();
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
            ClientId = ClientSelectionne?.Id,
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
