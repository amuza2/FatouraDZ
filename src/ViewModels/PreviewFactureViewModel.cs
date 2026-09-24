using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class PreviewFactureViewModel : ViewModelBase
{
    private readonly IPdfService _pdfService;
    private readonly IDatabaseService _databaseService;
    private readonly IAppLogger _logger;

    [ObservableProperty]
    private Facture _facture;

    [ObservableProperty]
    private Business _business;

    [ObservableProperty]
    private Bitmap? _previewImage;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _erreurMessage;

    [ObservableProperty]
    private bool _estSauvegarde;

    public event Action? DemanderFermeture;
    public event Action? DemanderModification;

    public PreviewFactureViewModel(Facture facture, Business business)
    {
        _pdfService = ServiceLocator.PdfService;
        _databaseService = ServiceLocator.DatabaseService;
        _logger = ServiceLocator.Logger;
        _facture = facture;
        _business = business;
    }

    public async Task GenererPreviewAsync()
    {
        IsLoading = true;
        ErreurMessage = null;

        try
        {
            // Rendu partagé avec le PDF : plus aucune divergence possible entre l'aperçu et le fichier.
            var imageBytes = await _pdfService.GenererApercuPngAsync(Facture, Business);
            using var stream = new MemoryStream(imageBytes);
            PreviewImage = new Bitmap(stream);

            _logger.Debug($"Aperçu généré : {imageBytes.Length / 1024} Ko, "
                + $"facture {Facture.NumeroFacture} ({Facture.Lignes.Count} ligne(s))");
        }
        catch (Exception ex)
        {
            _logger.Error("Échec de la génération de l'aperçu PDF", ex);
            ErreurMessage = $"Erreur lors de la génération de l'aperçu : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }



    [RelayCommand]
    private void Modifier()
    {
        DemanderModification?.Invoke();
        DemanderFermeture?.Invoke();
    }

    [RelayCommand]
    private async Task EnregistrerPdfAsync(IStorageProvider? storageProvider)
    {
        if (storageProvider == null)
        {
            ErreurMessage = "Impossible d'accéder au système de fichiers";
            return;
        }

        ErreurMessage = null;
        EstSauvegarde = false;

        try
        {
            // Créer le dossier Factures par défaut s'il n'existe pas
            var dossierFactures = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "FatouraDZ",
                "Factures"
            );
            Directory.CreateDirectory(dossierFactures);

            // Nom suggéré au format FAC-YYYY-NNN_NomClient.pdf
            var clientNomNettoye = string.Join("_", Facture.ClientNom.Split(Path.GetInvalidFileNameChars()));
            var suggestedFileName = $"{Facture.NumeroFacture}_{clientNomNettoye}.pdf";
            
            // Obtenir le dossier par défaut
            var defaultFolder = await storageProvider.TryGetFolderFromPathAsync(dossierFactures);
            
            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Enregistrer la facture en PDF",
                SuggestedFileName = suggestedFileName,
                SuggestedStartLocation = defaultFolder,
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (file != null)
            {
                var cheminPdf = file.Path.LocalPath;
                await _pdfService.GenererPdfAsync(Facture, Business, cheminPdf);

                _logger.Debug($"PDF enregistré : {new FileInfo(cheminPdf).Length / 1024} Ko pour la facture {Facture.NumeroFacture}.");

                // Mettre à jour le chemin PDF uniquement si la facture existe déjà en base
                Facture.CheminPDF = cheminPdf;
                if (Facture.Id > 0)
                {
                    await _databaseService.UpdateCheminPdfAsync(Facture.Id, cheminPdf);
                }
                
                _ = AfficherSuccesTemporaireAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Échec de l'enregistrement du PDF", ex);
            ErreurMessage = $"Erreur lors de l'enregistrement : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImprimerAsync()
    {
        ErreurMessage = null;

        try
        {
            // Dossier temporaire dédié : permet de nettoyer les impressions précédentes
            // sans toucher aux autres fichiers temporaires du système.
            var dossierTemp = Path.Combine(Path.GetTempPath(), "FatouraDZ");
            Directory.CreateDirectory(dossierTemp);
            NettoyerImpressionsAnciennes(dossierTemp);

            var numeroNettoye = string.Join("_", Facture.NumeroFacture.Split(Path.GetInvalidFileNameChars()));
            var tempPath = Path.Combine(dossierTemp, $"FatouraDZ_{numeroNettoye}.pdf");

            await _pdfService.GenererPdfAsync(Facture, Business, tempPath);
            _logger.Debug($"Impression : PDF temporaire {AppPaths.Abreger(tempPath)} préparé et ouvert.");

            // Ouvrir le PDF avec l'application par défaut (qui permet d'imprimer)
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger.Error("Échec de l'impression", ex);
            ErreurMessage = $"Erreur lors de l'impression : {ex.Message}";
        }
    }

    /// <summary>
    /// Supprime les PDF d'impression des sessions précédentes (plus d'un jour) pour
    /// qu'ils ne s'accumulent pas indéfiniment dans le dossier temporaire.
    /// </summary>
    private void NettoyerImpressionsAnciennes(string dossierTemp)
    {
        try
        {
            var limite = DateTime.Now.AddDays(-1);
            foreach (var fichier in Directory.GetFiles(dossierTemp, "FatouraDZ_*.pdf"))
            {
                if (File.GetLastWriteTime(fichier) < limite)
                    File.Delete(fichier);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning("Nettoyage des impressions temporaires impossible", ex);
        }
    }

    private async Task AfficherSuccesTemporaireAsync()
    {
        EstSauvegarde = true;
        await Task.Delay(3000);
        EstSauvegarde = false;
    }

    [RelayCommand]
    private void Annuler()
    {
        DemanderFermeture?.Invoke();
    }

    [RelayCommand]
    private async Task EnregistrerExcelAsync(IStorageProvider? storageProvider)
    {
        if (storageProvider == null)
        {
            ErreurMessage = "Impossible d'accéder au système de fichiers";
            return;
        }

        ErreurMessage = null;
        EstSauvegarde = false;

        try
        {
            var dossierFactures = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "FatouraDZ",
                "Factures"
            );
            Directory.CreateDirectory(dossierFactures);

            var clientNomNettoye = string.Join("_", Facture.ClientNom.Split(Path.GetInvalidFileNameChars()));
            var suggestedFileName = $"{Facture.NumeroFacture}_{clientNomNettoye}.xlsx";
            
            var defaultFolder = await storageProvider.TryGetFolderFromPathAsync(dossierFactures);
            
            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Enregistrer la facture en Excel",
                SuggestedFileName = suggestedFileName,
                SuggestedStartLocation = defaultFolder,
                DefaultExtension = "xlsx",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx" } }
                }
            });

            if (file != null)
            {
                var cheminExcel = file.Path.LocalPath;
                await ServiceLocator.ExcelService.GenererExcelAsync(Facture, Business, cheminExcel);
                _ = AfficherSuccesTemporaireAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Échec de l'export Excel", ex);
            ErreurMessage = $"Erreur lors de l'enregistrement Excel : {ex.Message}";
        }
    }
}
