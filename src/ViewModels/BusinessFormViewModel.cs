using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class BusinessFormViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private int _businessId;

    [ObservableProperty]
    private bool _estModeEdition;

    [ObservableProperty]
    private string _titreFormulaire = "Nouvelle entreprise";

    [ObservableProperty]
    private int _typeEntrepriseIndex;

    public BusinessType TypeEntreprise => (BusinessType)TypeEntrepriseIndex;

    partial void OnTypeEntrepriseIndexChanged(int value)
    {
        OnPropertyChanged(nameof(TypeEntreprise));
        OnPropertyChanged(nameof(EstAutoEntrepreneur));
        OnPropertyChanged(nameof(EstForfait));
        OnPropertyChanged(nameof(EstReel));
        OnPropertyChanged(nameof(AfficherNomComplet));
        OnPropertyChanged(nameof(AfficherNumeroImmatriculation));
        OnPropertyChanged(nameof(AfficherRC));
        OnPropertyChanged(nameof(AfficherCapitalSocial));
    }

    // Computed properties for UI visibility based on business type
    public bool EstAutoEntrepreneur => TypeEntreprise == BusinessType.AutoEntrepreneur;
    public bool EstForfait => TypeEntreprise == BusinessType.Forfait;
    public bool EstReel => TypeEntreprise == BusinessType.Reel;
    public bool AfficherNomComplet => TypeEntreprise != BusinessType.Reel;
    public bool AfficherNumeroImmatriculation => TypeEntreprise == BusinessType.AutoEntrepreneur;
    public bool AfficherRC => TypeEntreprise != BusinessType.AutoEntrepreneur;
    public bool AfficherCapitalSocial => TypeEntreprise == BusinessType.Reel;

    [ObservableProperty]
    private string _nom = string.Empty;

    [ObservableProperty]
    private string _nomComplet = string.Empty;

    [ObservableProperty]
    private string? _raisonSociale;

    [ObservableProperty]
    private string? _activite;

    [ObservableProperty]
    private string _adresse = string.Empty;

    [ObservableProperty]
    private string _ville = string.Empty;

    [ObservableProperty]
    private string _wilaya = string.Empty;

    [ObservableProperty]
    private string? _codePostal;

    [ObservableProperty]
    private string _telephone = string.Empty;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _fax;

    [ObservableProperty]
    private string _rc = string.Empty;

    [ObservableProperty]
    private string _nis = string.Empty;

    [ObservableProperty]
    private string _nif = string.Empty;

    [ObservableProperty]
    private string _ai = string.Empty;

    [ObservableProperty]
    private string _numeroImmatriculation = string.Empty;

    [ObservableProperty]
    private string? _capitalSocial;

    [ObservableProperty]
    private string? _cheminLogo;

    [ObservableProperty]
    private string? _erreurMessage;

    [ObservableProperty]
    private bool _estSauvegarde;

    public event Action? BusinessSaved;
    public event Action? CancelRequested;

    public BusinessFormViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
    }

    public void ChargerBusiness(Business business)
    {
        _businessId = business.Id;
        EstModeEdition = true;
        TitreFormulaire = $"Modifier {business.Nom}";

        TypeEntrepriseIndex = (int)business.TypeEntreprise;
        Nom = business.Nom;
        NomComplet = business.NomComplet;
        RaisonSociale = business.RaisonSociale;
        Activite = business.Activite;
        Adresse = business.Adresse;
        Ville = business.Ville;
        Wilaya = business.Wilaya;
        CodePostal = business.CodePostal;
        Telephone = business.Telephone;
        Email = business.Email;
        Fax = business.Fax;
        Rc = business.RC;
        Nis = business.NIS;
        Nif = business.NIF;
        Ai = business.AI;
        NumeroImmatriculation = business.NumeroImmatriculation;
        CapitalSocial = business.CapitalSocial;
        CheminLogo = business.CheminLogo;
    }

    [RelayCommand]
    private async Task SauvegarderAsync()
    {
        ErreurMessage = null;
        EstSauvegarde = false;

        // Basic validation
        if (string.IsNullOrWhiteSpace(Nom))
        {
            ErreurMessage = "Le nom de l'entreprise est obligatoire";
            return;
        }

        if (string.IsNullOrWhiteSpace(Adresse))
        {
            ErreurMessage = "L'adresse est obligatoire";
            return;
        }

        if (string.IsNullOrWhiteSpace(Ville))
        {
            ErreurMessage = "La ville est obligatoire";
            return;
        }

        if (string.IsNullOrWhiteSpace(Wilaya))
        {
            ErreurMessage = "La wilaya est obligatoire";
            return;
        }

        if (string.IsNullOrWhiteSpace(Telephone))
        {
            ErreurMessage = "Le téléphone est obligatoire";
            return;
        }

        var business = new Business
        {
            Id = _businessId,
            TypeEntreprise = TypeEntreprise,
            Nom = Nom,
            NomComplet = NomComplet,
            RaisonSociale = RaisonSociale,
            Activite = Activite,
            Adresse = Adresse,
            Ville = Ville,
            Wilaya = Wilaya,
            CodePostal = CodePostal,
            Telephone = Telephone,
            Email = Email,
            Fax = Fax,
            RC = Rc,
            NIS = Nis,
            NIF = Nif,
            AI = Ai,
            NumeroImmatriculation = NumeroImmatriculation,
            CapitalSocial = CapitalSocial,
            CheminLogo = CheminLogo
        };

        try
        {
            await _databaseService.SaveBusinessAsync(business);
            EstSauvegarde = true;
            BusinessSaved?.Invoke();
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur lors de la sauvegarde : {ex.Message}";
        }
    }

    [RelayCommand]
    private void Annuler()
    {
        CancelRequested?.Invoke();
    }

    [RelayCommand]
    private async Task SelectionnerLogoAsync(IStorageProvider storageProvider)
    {
        var fichiers = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionner un logo",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg" },
                    MimeTypes = new[] { "image/png", "image/jpeg" }
                }
            }
        });

        if (fichiers.Count > 0)
        {
            var fichier = fichiers[0];
            var cheminSource = fichier.Path.LocalPath;

            var fileInfo = new FileInfo(cheminSource);
            if (fileInfo.Length > 2 * 1024 * 1024)
            {
                ErreurMessage = "Le fichier logo ne doit pas dépasser 2 Mo";
                return;
            }

            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FatouraDZ", "logos"
            );
            Directory.CreateDirectory(appDataPath);

            var extension = Path.GetExtension(cheminSource);
            var nouveauChemin = Path.Combine(appDataPath, $"logo_{Guid.NewGuid()}{extension}");

            File.Copy(cheminSource, nouveauChemin, true);
            CheminLogo = nouveauChemin;
        }
    }

    [RelayCommand]
    private void SupprimerLogo()
    {
        CheminLogo = null;
    }
}
