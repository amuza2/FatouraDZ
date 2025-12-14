using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FatouraDZ.ViewModels;

namespace FatouraDZ.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.DemanderPrevisualisation += OuvrirPrevisualisation;
            vm.DemanderSauvegardeFichier += OuvrirDialogueSauvegardePdf;
            vm.DemanderConfirmationDialog += AfficherConfirmationAsync;
            vm.DemanderExportFichier += OuvrirDialogueExportDb;
            vm.DemanderImportFichier += OuvrirDialogueImportDb;
            await vm.InitialiserAsync();
        }
    }

    private void OuvrirPrevisualisation(FatouraDZ.Models.Facture facture, FatouraDZ.Models.Entrepreneur entrepreneur)
    {
        var previewVm = new PreviewFactureViewModel(facture, entrepreneur);
        var previewWindow = new PreviewFactureWindow(previewVm);
        previewWindow.ShowDialog(this);
    }

    private async Task<IStorageFile?> OuvrirDialogueSauvegardePdf(string nomSuggere)
    {
        var dossierFactures = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Factures"
        );
        Directory.CreateDirectory(dossierFactures);

        var options = new FilePickerSaveOptions
        {
            Title = "Enregistrer la facture PDF",
            SuggestedFileName = nomSuggere,
            DefaultExtension = "pdf",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("Fichier PDF") { Patterns = new[] { "*.pdf" } }
            },
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(dossierFactures)
        };

        return await StorageProvider.SaveFilePickerAsync(options);
    }

    private async Task<bool> AfficherConfirmationAsync(string titre, string message)
    {
        var dialog = new ConfirmationDialog(titre, message);
        var result = await dialog.ShowDialog<bool?>(this);
        return result == true;
    }

    private async Task<IStorageFile?> OuvrirDialogueExportDb(string nomSuggere, string description)
    {
        var options = new FilePickerSaveOptions
        {
            Title = "Exporter la base de données",
            SuggestedFileName = nomSuggere,
            DefaultExtension = "db",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType(description) { Patterns = new[] { "*.db" } }
            }
        };

        return await StorageProvider.SaveFilePickerAsync(options);
    }

    private async Task<IStorageFile?> OuvrirDialogueImportDb()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Importer une base de données",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Base de données SQLite") { Patterns = new[] { "*.db" } }
            }
        };

        var files = await StorageProvider.OpenFilePickerAsync(options);
        return files.Count > 0 ? files[0] : null;
    }
}