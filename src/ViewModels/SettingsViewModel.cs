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

    // General settings
    [ObservableProperty]
    private string _formatNumeroFacture = "FAC-{ANNEE}-{NUM}";

    [ObservableProperty]
    private int _delaiPaiementDefaut = 30;

    // App info
    public string Version => "2.0.0";
    public string Developpeur => "FatouraDZ Team";
    public string Description => "Application de facturation multi-entreprises conforme à la réglementation algérienne.";
    public string Contact => "support@fatouradz.dz";

    public event Action? BackRequested;
    public event Func<string, string, Task<IStorageFile?>>? DemanderExportFichier;
    public event Func<Task<IStorageFile?>>? DemanderImportFichier;

    public SettingsViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
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
}
