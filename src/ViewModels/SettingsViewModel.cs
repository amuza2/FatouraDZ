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

    // Fiscal settings - Timbre
    [ObservableProperty]
    private decimal _tauxTimbreFiscal;

    [ObservableProperty]
    private decimal _montantMaxTimbre;

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
    public string Version => "1.0.0";
    public string Developpeur => "FatouraDZ Team";
    public string Description => "Application de facturation multi-entreprises conforme à la réglementation algérienne.";
    public string Contact => "info@dzdevelopers.com";

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
        TauxTimbreFiscal = settings.TauxTimbreFiscal;
        MontantMaxTimbre = settings.MontantMaxTimbre;
        TauxRetenueSourceDefaut = settings.TauxRetenueSourceDefaut;
        FormatNumeroFacture = settings.FormatNumeroFacture;
        DelaiPaiementDefaut = settings.DelaiPaiementDefaut;
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
            settings.TauxTimbreFiscal = TauxTimbreFiscal;
            settings.MontantMaxTimbre = MontantMaxTimbre;
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
        TauxTimbreFiscal = 1m;
        MontantMaxTimbre = 2500m;
        TauxRetenueSourceDefaut = 5m;
        FormatNumeroFacture = "FAC-{ANNEE}-{NUM}";
        DelaiPaiementDefaut = 30;

        EnregistrerParametresFiscaux();
        MessageSucces = "Paramètres fiscaux réinitialisés aux valeurs par défaut.";
    }
}
