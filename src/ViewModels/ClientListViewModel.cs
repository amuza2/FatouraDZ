using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class ClientListViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private Business? _business;

    public ObservableCollection<Client> Clients { get; } = new();

    [ObservableProperty]
    private Client? _clientSelectionne;

    [ObservableProperty]
    private bool _estChargement;

    [ObservableProperty]
    private string? _messageErreur;

    [ObservableProperty]
    private string _recherche = string.Empty;

    [ObservableProperty]
    private bool _afficherFormulaire;

    [ObservableProperty]
    private bool _estModeEdition;

    [ObservableProperty]
    private string _titreFormulaire = "Nouveau client";

    // Form fields
    [ObservableProperty]
    private int _typeClientIndex = 0;

    public BusinessType TypeClient => (BusinessType)TypeClientIndex;

    public bool AfficherNumeroImmatriculation => TypeClient == BusinessType.AutoEntrepreneur;
    public bool AfficherRC => TypeClient != BusinessType.AutoEntrepreneur;
    public bool AfficherCapitalSocial => TypeClient == BusinessType.Reel;

    partial void OnTypeClientIndexChanged(int value)
    {
        OnPropertyChanged(nameof(TypeClient));
        OnPropertyChanged(nameof(AfficherNumeroImmatriculation));
        OnPropertyChanged(nameof(AfficherRC));
        OnPropertyChanged(nameof(AfficherCapitalSocial));
    }

    [ObservableProperty]
    private string _nom = string.Empty;

    [ObservableProperty]
    private string _adresse = string.Empty;

    [ObservableProperty]
    private string _telephone = string.Empty;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _fax;

    [ObservableProperty]
    private string? _formeJuridique;

    [ObservableProperty]
    private string? _rc;

    [ObservableProperty]
    private string? _nis;

    [ObservableProperty]
    private string? _nif;

    [ObservableProperty]
    private string? _ai;

    [ObservableProperty]
    private string? _numeroImmatriculation;

    [ObservableProperty]
    private string? _activite;

    [ObservableProperty]
    private string? _capitalSocial;

    private int _clientIdEnEdition;

    public event Action? BackRequested;

    public ClientListViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
    }

    public void SetBusiness(Business business)
    {
        _business = business;
    }

    public async Task ChargerClientsAsync()
    {
        if (_business == null) return;

        EstChargement = true;
        MessageErreur = null;

        try
        {
            var clients = await _databaseService.GetClientsByBusinessIdAsync(_business.Id);
            
            Clients.Clear();
            foreach (var client in clients)
            {
                Clients.Add(client);
            }
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur lors du chargement des clients : {ex.Message}";
        }
        finally
        {
            EstChargement = false;
        }
    }

    partial void OnRechercheChanged(string value)
    {
        _ = FiltrerClientsAsync();
    }

    private async Task FiltrerClientsAsync()
    {
        if (_business == null) return;

        var clients = await _databaseService.GetClientsByBusinessIdAsync(_business.Id);
        
        if (!string.IsNullOrWhiteSpace(Recherche))
        {
            var searchLower = Recherche.ToLower();
            clients = clients.Where(c => 
                c.Nom.ToLower().Contains(searchLower) ||
                (c.Email?.ToLower().Contains(searchLower) ?? false) ||
                c.Telephone.Contains(searchLower)
            ).ToList();
        }

        Clients.Clear();
        foreach (var client in clients)
        {
            Clients.Add(client);
        }
    }

    [RelayCommand]
    private void NouveauClient()
    {
        _clientIdEnEdition = 0;
        EstModeEdition = false;
        TitreFormulaire = "Nouveau client";
        ReinitialiserFormulaire();
        AfficherFormulaire = true;
    }

    [RelayCommand]
    private void ModifierClient(Client client)
    {
        _clientIdEnEdition = client.Id;
        EstModeEdition = true;
        TitreFormulaire = $"Modifier {client.Nom}";
        
        TypeClientIndex = (int)client.TypeClient;
        Nom = client.Nom;
        Adresse = client.Adresse;
        Telephone = client.Telephone;
        Email = client.Email;
        Fax = client.Fax;
        FormeJuridique = client.FormeJuridique;
        Rc = client.RC;
        Nis = client.NIS;
        Nif = client.NIF;
        Ai = client.AI;
        NumeroImmatriculation = client.NumeroImmatriculation;
        Activite = client.Activite;
        CapitalSocial = client.CapitalSocial;
        
        AfficherFormulaire = true;
    }

    [RelayCommand]
    private async Task SupprimerClientAsync(Client client)
    {
        try
        {
            await _databaseService.DeleteClientAsync(client.Id);
            Clients.Remove(client);
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur lors de la suppression : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task EnregistrerClientAsync()
    {
        if (_business == null) return;

        if (string.IsNullOrWhiteSpace(Nom))
        {
            MessageErreur = "Le nom du client est obligatoire";
            return;
        }

        if (string.IsNullOrWhiteSpace(Adresse))
        {
            MessageErreur = "L'adresse est obligatoire";
            return;
        }

        if (string.IsNullOrWhiteSpace(Telephone))
        {
            MessageErreur = "Le téléphone est obligatoire";
            return;
        }

        try
        {
            var client = new Client
            {
                Id = _clientIdEnEdition,
                BusinessId = _business.Id,
                TypeClient = TypeClient,
                Nom = Nom,
                Adresse = Adresse,
                Telephone = Telephone,
                Email = Email,
                Fax = Fax,
                FormeJuridique = FormeJuridique,
                RC = Rc,
                NIS = Nis,
                NIF = Nif,
                AI = Ai,
                NumeroImmatriculation = NumeroImmatriculation,
                Activite = Activite,
                CapitalSocial = CapitalSocial
            };

            await _databaseService.SaveClientAsync(client);
            
            AfficherFormulaire = false;
            await ChargerClientsAsync();
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur lors de l'enregistrement : {ex.Message}";
        }
    }

    [RelayCommand]
    private void AnnulerFormulaire()
    {
        AfficherFormulaire = false;
        MessageErreur = null;
    }

    private void ReinitialiserFormulaire()
    {
        TypeClientIndex = 0;
        Nom = string.Empty;
        Adresse = string.Empty;
        Telephone = string.Empty;
        Email = null;
        Fax = null;
        FormeJuridique = null;
        Rc = null;
        Nis = null;
        Nif = null;
        Ai = null;
        NumeroImmatriculation = null;
        Activite = null;
        CapitalSocial = null;
        MessageErreur = null;
    }

    [RelayCommand]
    private void Retour()
    {
        BackRequested?.Invoke();
    }
}
