using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    [ObservableProperty]
    private string? _messageSucces;

    [ObservableProperty]
    private string? _messageErreur;

    [ObservableProperty]
    private bool _estChargement;

    // Fiscal settings - TVA
    [ObservableProperty]
    private decimal _tauxTVAStandard;

    [ObservableProperty]
    private decimal _tauxTVAReduit;

    // Fiscal settings - Timbre (barème progressif - LF 2025, art. 100 code du timbre)
    [ObservableProperty]
    private decimal _timbreSeuilExoneration;

    [ObservableProperty]
    private decimal _timbreSeuil1;

    [ObservableProperty]
    private decimal _timbreTaux1;

    [ObservableProperty]
    private decimal _timbreSeuil2;

    [ObservableProperty]
    private decimal _timbreTaux2;

    [ObservableProperty]
    private decimal _timbreTaux3;

    [ObservableProperty]
    private decimal _timbreMinimum;

    // Arrondi de l'assiette « par tranche de 100 DA ou fraction de tranche ».
    [ObservableProperty]
    private bool _timbreArrondiTrancheCent;

    // Fiscal settings - Retenue
    [ObservableProperty]
    private decimal _tauxRetenueSourceDefaut;

    // Invoice settings
    [ObservableProperty]
    private string _formatNumeroFacture = "FAC-{ANNEE}-{NUM}";

    [ObservableProperty]
    private int _delaiPaiementDefaut = 30;

    // Database path
    [ObservableProperty]
    private string _cheminBaseDeDonnees = string.Empty;

    public string CheminParDefaut => AppSettings.GetDefaultDatabasePath();

    // App info
    public string Version => AppInfo.Version;
    public string Developpeur => "FatouraDZ Team";
    public string Description => "Application de facturation multi-entreprises conforme à la réglementation algérienne.";
    public string Contact => "info@dzdevelopers.com";

    // Mises à jour
    [ObservableProperty]
    private bool _verifierMisesAJour;

    [ObservableProperty]
    private string? _messageMiseAJour;

    [ObservableProperty]
    private bool _verificationMiseAJourEnCours;

    /// <summary>
    /// Interrogation lancée par l'utilisateur : on peut donc ouvrir la page de
    /// téléchargement quand une version existe (rien n'est fait en arrière-plan).
    /// </summary>
    [RelayCommand]
    private async Task VerifierMisesAJourAsync()
    {
        if (VerificationMiseAJourEnCours)
            return;

        VerificationMiseAJourEnCours = true;
        MessageMiseAJour = "Vérification en cours…";

        try
        {
            var disponible = await ServiceLocator.UpdateService.VerifierAsync();

            if (disponible == null)
            {
                MessageMiseAJour = $"FatouraDZ {AppInfo.Version} est à jour.";
                return;
            }

            MessageMiseAJour = $"La version {disponible.Version} est disponible.";
            if (LiensExternes.Ouvrir(disponible.PageHtml))
                MessageMiseAJour += " La page de téléchargement s'est ouverte dans votre navigateur.";
        }
        catch (Exception ex)
        {
            MessageMiseAJour = $"Vérification impossible : {ex.Message}";
        }
        finally
        {
            VerificationMiseAJourEnCours = false;
        }
    }

    /// <summary>Persiste immédiatement le choix : c'est une préférence, pas un champ de formulaire.</summary>
    partial void OnVerifierMisesAJourChanged(bool value)
    {
        try
        {
            var parametres = AppSettings.Instance;
            if (parametres.VerifierMisesAJour == value)
                return;

            parametres.VerifierMisesAJour = value;
            parametres.Save();
        }
        catch (Exception ex)
        {
            MessageErreur = $"Impossible d'enregistrer la préférence : {ex.Message}";
        }
    }

    public event Action? BackRequested;
    public event Func<string, string, Task<IStorageFile?>>? DemanderExportFichier;
    public event Func<Task<IStorageFile?>>? DemanderImportFichier;
    public event Func<Task<IStorageFolder?>>? DemanderDossier;

    public SettingsViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = AppSettings.Instance;
        CheminBaseDeDonnees = settings.DatabasePath;
        TauxTVAStandard = settings.TauxTVAStandard;
        TauxTVAReduit = settings.TauxTVAReduit;
        TimbreSeuilExoneration = settings.TimbreSeuilExoneration;
        TimbreSeuil1 = settings.TimbreSeuil1;
        TimbreTaux1 = settings.TimbreTaux1;
        TimbreSeuil2 = settings.TimbreSeuil2;
        TimbreTaux2 = settings.TimbreTaux2;
        TimbreTaux3 = settings.TimbreTaux3;
        TimbreMinimum = settings.TimbreMinimum;
        TimbreArrondiTrancheCent = settings.TimbreArrondiTrancheCent;
        TauxRetenueSourceDefaut = settings.TauxRetenueSourceDefaut;
        FormatNumeroFacture = settings.FormatNumeroFacture;
        DelaiPaiementDefaut = settings.DelaiPaiementDefaut;
        VerifierMisesAJour = settings.VerifierMisesAJour;
    }

    [RelayCommand]
    private void GoBack()
    {
        BackRequested?.Invoke();
    }

    [RelayCommand]
    private async Task ExporterBaseDeDonneesAsync()
    {
        EstChargement = true;
        MessageSucces = null;
        MessageErreur = null;

        try
        {
            var sourcePath = _databaseService.GetDatabasePath();
            
            if (!File.Exists(sourcePath))
            {
                MessageErreur = "Base de données introuvable.";
                return;
            }

            var nomFichier = $"fatouradz_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var fichier = DemanderExportFichier != null 
                ? await DemanderExportFichier.Invoke(nomFichier, "Base de données SQLite") 
                : null;

            if (fichier != null)
            {
                var destPath = fichier.Path.LocalPath;
                File.Copy(sourcePath, destPath, overwrite: true);
                MessageSucces = $"Base de données exportée vers :\n{destPath}";
            }
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur lors de l'export : {ex.Message}";
        }
        finally
        {
            EstChargement = false;
        }
    }

    [RelayCommand]
    private async Task ImporterBaseDeDonneesAsync()
    {
        EstChargement = true;
        MessageSucces = null;
        MessageErreur = null;

        try
        {
            var fichier = DemanderImportFichier != null 
                ? await DemanderImportFichier.Invoke() 
                : null;

            if (fichier != null)
            {
                var sourcePath = fichier.Path.LocalPath;
                var destinationPath = _databaseService.GetDatabasePath();
                
                // Create backup of current database before import
                var backupPath = destinationPath + ".backup";
                if (File.Exists(destinationPath))
                {
                    File.Copy(destinationPath, backupPath, true);
                }

                File.Copy(sourcePath, destinationPath, true);
                MessageSucces = $"Base de données importée depuis :\n{Path.GetFileName(sourcePath)}\n\nRedémarrez l'application pour voir les changements.";
            }
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur lors de l'import : {ex.Message}";
        }
        finally
        {
            EstChargement = false;
        }
    }

    [RelayCommand]
    private void OuvrirDossierExport()
    {
        try
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var exportPath = Path.Combine(documentsPath, "FatouraDZ_Exports");
            Directory.CreateDirectory(exportPath);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exportPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageErreur = $"Impossible d'ouvrir le dossier : {ex.Message}";
        }
    }

    /// <summary>Emplacement des journaux et des rapports de plantage, affiché à l'utilisateur.</summary>
    public string CheminDonneesTechniques => AppPaths.Abreger(AppPaths.DossierDonnees);

    /// <summary>
    /// Ouvre le dossier de données : c'est là que se trouvent les journaux et les
    /// rapports de plantage, et c'est ce qu'on demande à un utilisateur qui signale
    /// un problème. Sans ce bouton, le seul moyen serait de lui décrire un chemin
    /// caché — autant dire qu'aucun rapport ne serait jamais envoyé.
    /// </summary>
    [RelayCommand]
    private void OuvrirDossierDonnees()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DossierDonnees);
            LiensExternes.Ouvrir(AppPaths.DossierDonnees);
        }
        catch (Exception ex)
        {
            MessageErreur = $"Impossible d'ouvrir le dossier : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ChangerEmplacementBaseDeDonneesAsync()
    {
        MessageSucces = null;
        MessageErreur = null;

        try
        {
            var dossier = DemanderDossier != null 
                ? await DemanderDossier.Invoke() 
                : null;

            if (dossier != null)
            {
                var nouveauChemin = Path.Combine(dossier.Path.LocalPath, "fatouradz.db");
                var ancienChemin = AppSettings.Instance.DatabasePath;

                // Copy existing database to new location if it exists
                if (File.Exists(ancienChemin) && ancienChemin != nouveauChemin)
                {
                    File.Copy(ancienChemin, nouveauChemin, overwrite: true);
                }

                // Update settings
                AppSettings.Instance.DatabasePath = nouveauChemin;
                AppSettings.Instance.Save();
                CheminBaseDeDonnees = nouveauChemin;

                MessageSucces = $"Emplacement de la base de données changé.\nRedémarrez l'application pour appliquer les changements.";
            }
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur lors du changement d'emplacement : {ex.Message}";
        }
    }

    [RelayCommand]
    private void ReinitialiserEmplacement()
    {
        MessageSucces = null;
        MessageErreur = null;

        try
        {
            var cheminParDefaut = AppSettings.GetDefaultDatabasePath();
            AppSettings.Instance.DatabasePath = cheminParDefaut;
            AppSettings.Instance.Save();
            CheminBaseDeDonnees = cheminParDefaut;

            MessageSucces = "Emplacement réinitialisé à la valeur par défaut.\nRedémarrez l'application pour appliquer les changements.";
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    private void EnregistrerParametresFiscaux()
    {
        MessageSucces = null;
        MessageErreur = null;

        try
        {
            var settings = AppSettings.Instance;
            settings.TauxTVAStandard = TauxTVAStandard;
            settings.TauxTVAReduit = TauxTVAReduit;
            settings.TimbreSeuilExoneration = TimbreSeuilExoneration;
            settings.TimbreSeuil1 = TimbreSeuil1;
            settings.TimbreTaux1 = TimbreTaux1;
            settings.TimbreSeuil2 = TimbreSeuil2;
            settings.TimbreTaux2 = TimbreTaux2;
            settings.TimbreTaux3 = TimbreTaux3;
            settings.TimbreMinimum = TimbreMinimum;
            settings.TimbreArrondiTrancheCent = TimbreArrondiTrancheCent;
            settings.TauxRetenueSourceDefaut = TauxRetenueSourceDefaut;
            settings.FormatNumeroFacture = FormatNumeroFacture;
            settings.DelaiPaiementDefaut = DelaiPaiementDefaut;
            settings.Save();

            MessageSucces = "Paramètres fiscaux enregistrés avec succès.";
        }
        catch (Exception ex)
        {
            MessageErreur = $"Erreur lors de l'enregistrement : {ex.Message}";
        }
    }

    [RelayCommand]
    private void ReinitialiserParametresFiscaux()
    {
        MessageSucces = null;
        MessageErreur = null;

        TauxTVAStandard = 19m;
        TauxTVAReduit = 9m;
        TimbreSeuilExoneration = 300m;
        TimbreSeuil1 = 30000m;
        TimbreTaux1 = 1m;
        TimbreSeuil2 = 100000m;
        TimbreTaux2 = 1.5m;
        TimbreTaux3 = 2m;
        TimbreMinimum = 5m;
        TimbreArrondiTrancheCent = false;
        TauxRetenueSourceDefaut = 5m;
        FormatNumeroFacture = "FAC-{ANNEE}-{NUM}";
        DelaiPaiementDefaut = 30;

        EnregistrerParametresFiscaux();
        MessageSucces = "Paramètres fiscaux réinitialisés aux valeurs par défaut.";
    }
}
