using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class EntrepreneurConfigViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly IValidationService _validationService;

    [ObservableProperty]
    private string _nomComplet = string.Empty;

    [ObservableProperty]
    private string? _raisonSociale;

    [ObservableProperty]
    private string? _formeJuridique;

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
    private bool _estCapitalApplicable;

    [ObservableProperty]
    private string? _cheminLogo;

    [ObservableProperty]
    private string? _erreurMessage;

    [ObservableProperty]
    private bool _estSauvegarde;

    [ObservableProperty]
    private bool _estChargement;

    // Erreurs de validation par champ
    [ObservableProperty]
    private string? _erreurNomComplet;

    [ObservableProperty]
    private string? _erreurAdresse;

    [ObservableProperty]
    private string? _erreurVille;

    [ObservableProperty]
    private string? _erreurWilaya;

    [ObservableProperty]
    private string? _erreurTelephone;

    [ObservableProperty]
    private string? _erreurEmail;

    [ObservableProperty]
    private string? _erreurRC;

    [ObservableProperty]
    private string? _erreurNIS;

    [ObservableProperty]
    private string? _erreurNIF;

    [ObservableProperty]
    private string? _erreurAI;

    [ObservableProperty]
    private string? _erreurNumeroImmatriculation;

    public event Action? ConfigurationSauvegardee;

    public EntrepreneurConfigViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
        _validationService = ServiceLocator.ValidationService;
    }

    public async Task ChargerDonneesAsync()
    {
        EstChargement = true;
        try
        {
            var entrepreneur = await _databaseService.GetEntrepreneurAsync();
            if (entrepreneur != null)
            {
                NomComplet = entrepreneur.NomComplet;
                RaisonSociale = entrepreneur.RaisonSociale;
                FormeJuridique = entrepreneur.FormeJuridique;
                Activite = entrepreneur.Activite;
                Adresse = entrepreneur.Adresse;
                Ville = entrepreneur.Ville;
                Wilaya = entrepreneur.Wilaya;
                CodePostal = entrepreneur.CodePostal;
                Telephone = entrepreneur.Telephone;
                Email = entrepreneur.Email;
                Rc = entrepreneur.RC;
                Nis = entrepreneur.NIS;
                Nif = entrepreneur.NIF;
                Ai = entrepreneur.AI;
                NumeroImmatriculation = entrepreneur.NumeroImmatriculation;
                CapitalSocial = entrepreneur.CapitalSocial;
                EstCapitalApplicable = entrepreneur.EstCapitalApplicable;
                CheminLogo = entrepreneur.CheminLogo;
            }
        }
        finally
        {
            EstChargement = false;
        }
    }

    [RelayCommand]
    private async Task SauvegarderAsync()
    {
        ErreurMessage = null;
        EstSauvegarde = false;

        // Créer l'objet entrepreneur
        var entrepreneur = new Entrepreneur
        {
            NomComplet = NomComplet,
            RaisonSociale = RaisonSociale,
            FormeJuridique = FormeJuridique,
            Activite = Activite,
            Adresse = Adresse,
            Ville = Ville,
            Wilaya = Wilaya,
            CodePostal = CodePostal,
            Telephone = Telephone,
            Email = Email,
            RC = Rc,
            NIS = Nis,
            NIF = Nif,
            AI = Ai,
            NumeroImmatriculation = NumeroImmatriculation,
            CapitalSocial = CapitalSocial,
            EstCapitalApplicable = EstCapitalApplicable,
            CheminLogo = CheminLogo
        };

        // Valider
        var validation = _validationService.ValiderEntrepreneur(entrepreneur);
        if (!validation.EstValide)
        {
            ErreurMessage = string.Join("\n", validation.Erreurs);
            MettreAJourErreursChamps(validation);
            return;
        }

        // Effacer les erreurs
        EffacerErreursChamps();

        try
        {
            await _databaseService.SaveEntrepreneurAsync(entrepreneur);
            EstSauvegarde = true;
            ConfigurationSauvegardee?.Invoke();
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur lors de la sauvegarde : {ex.Message}";
        }
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

            // Vérifier la taille (max 2MB)
            var fileInfo = new FileInfo(cheminSource);
            if (fileInfo.Length > 2 * 1024 * 1024)
            {
                ErreurMessage = "Le fichier logo ne doit pas dépasser 2 Mo";
                return;
            }

            // Copier le logo dans le dossier de l'application
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FatouraDZ"
            );
            Directory.CreateDirectory(appDataPath);

            var extension = Path.GetExtension(cheminSource);
            var nouveauChemin = Path.Combine(appDataPath, $"logo{extension}");

            File.Copy(cheminSource, nouveauChemin, true);
            CheminLogo = nouveauChemin;
        }
    }

    [RelayCommand]
    private void SupprimerLogo()
    {
        if (!string.IsNullOrEmpty(CheminLogo) && File.Exists(CheminLogo))
        {
            try
            {
                File.Delete(CheminLogo);
            }
            catch
            {
                // Ignorer les erreurs de suppression
            }
        }
        CheminLogo = null;
    }

    partial void OnNomCompletChanged(string value)
    {
        ErreurNomComplet = string.IsNullOrWhiteSpace(value) ? "Le nom complet est obligatoire" : null;
    }

    partial void OnAdresseChanged(string value)
    {
        ErreurAdresse = string.IsNullOrWhiteSpace(value) ? "L'adresse est obligatoire" : null;
    }

    partial void OnVilleChanged(string value)
    {
        ErreurVille = string.IsNullOrWhiteSpace(value) ? "La ville est obligatoire" : null;
    }

    partial void OnWilayaChanged(string value)
    {
        ErreurWilaya = string.IsNullOrWhiteSpace(value) ? "La wilaya est obligatoire" : null;
    }

    partial void OnTelephoneChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            ErreurTelephone = "Le téléphone est obligatoire";
        else if (!_validationService.EstTelephoneValide(value))
            ErreurTelephone = "Format: 05XX XX XX XX ou 07XX XX XX XX";
        else
            ErreurTelephone = null;
    }

    partial void OnEmailChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !value.Contains("@"))
            ErreurEmail = "Format email invalide";
        else
            ErreurEmail = null;
    }

    partial void OnRcChanged(string value)
    {
        ErreurRC = string.IsNullOrWhiteSpace(value) ? "Le RC est obligatoire" : null;
    }

    partial void OnNisChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            ErreurNIS = "Le NIS est obligatoire";
        else if (!_validationService.EstNISValide(value))
            ErreurNIS = "Le NIS doit contenir 15 chiffres";
        else
            ErreurNIS = null;
    }

    partial void OnNifChanged(string value)
    {
        ErreurNIF = string.IsNullOrWhiteSpace(value) ? "Le NIF est obligatoire" : null;
    }

    partial void OnAiChanged(string value)
    {
        ErreurAI = string.IsNullOrWhiteSpace(value) ? "L'AI est obligatoire" : null;
    }

    partial void OnNumeroImmatriculationChanged(string value)
    {
        ErreurNumeroImmatriculation = string.IsNullOrWhiteSpace(value) ? "Le numéro d'immatriculation est obligatoire" : null;
    }

    private void MettreAJourErreursChamps(ValidationResult validation)
    {
        foreach (var erreur in validation.Erreurs)
        {
            if (erreur.Contains("nom complet", StringComparison.OrdinalIgnoreCase))
                ErreurNomComplet = erreur;
            else if (erreur.Contains("adresse", StringComparison.OrdinalIgnoreCase) && !erreur.Contains("email"))
                ErreurAdresse = erreur;
            else if (erreur.Contains("ville", StringComparison.OrdinalIgnoreCase))
                ErreurVille = erreur;
            else if (erreur.Contains("wilaya", StringComparison.OrdinalIgnoreCase))
                ErreurWilaya = erreur;
            else if (erreur.Contains("téléphone", StringComparison.OrdinalIgnoreCase))
                ErreurTelephone = erreur;
            else if (erreur.Contains("email", StringComparison.OrdinalIgnoreCase))
                ErreurEmail = erreur;
            else if (erreur.Contains("RC", StringComparison.OrdinalIgnoreCase))
                ErreurRC = erreur;
            else if (erreur.Contains("NIS", StringComparison.OrdinalIgnoreCase))
                ErreurNIS = erreur;
            else if (erreur.Contains("NIF", StringComparison.OrdinalIgnoreCase))
                ErreurNIF = erreur;
            else if (erreur.Contains("AI", StringComparison.OrdinalIgnoreCase))
                ErreurAI = erreur;
            else if (erreur.Contains("immatriculation", StringComparison.OrdinalIgnoreCase))
                ErreurNumeroImmatriculation = erreur;
        }
    }

    private void EffacerErreursChamps()
    {
        ErreurNomComplet = null;
        ErreurAdresse = null;
        ErreurVille = null;
        ErreurWilaya = null;
        ErreurTelephone = null;
        ErreurEmail = null;
        ErreurRC = null;
        ErreurNIS = null;
        ErreurNIF = null;
        ErreurAI = null;
        ErreurNumeroImmatriculation = null;
    }
}
