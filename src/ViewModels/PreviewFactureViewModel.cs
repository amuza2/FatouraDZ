using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Models;
using FatouraDZ.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FatouraDZ.ViewModels;

public partial class PreviewFactureViewModel : ViewModelBase
{
    private readonly IPdfService _pdfService;
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private Facture _facture;

    [ObservableProperty]
    private Entrepreneur _entrepreneur;

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

    public PreviewFactureViewModel(Facture facture, Entrepreneur entrepreneur)
    {
        _pdfService = ServiceLocator.PdfService;
        _databaseService = ServiceLocator.DatabaseService;
        _facture = facture;
        _entrepreneur = entrepreneur;
    }

    public async Task GenererPreviewAsync()
    {
        IsLoading = true;
        ErreurMessage = null;

        try
        {
            await Task.Run(() =>
            {
                var imageBytes = GeneratePdfPreviewImage();
                using var stream = new MemoryStream(imageBytes);
                PreviewImage = new Bitmap(stream);
            });
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur lors de la génération de l'aperçu : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private byte[] GeneratePdfPreviewImage()
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c));
                page.Content().Element(c => ComposeContent(c));
                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GenerateImages(new ImageGenerationSettings
        {
            ImageFormat = ImageFormat.Png,
            RasterDpi = 150
        }).First();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                if (!string.IsNullOrEmpty(Entrepreneur.CheminLogo) && File.Exists(Entrepreneur.CheminLogo))
                {
                    row.RelativeItem(1).Height(60).Image(Entrepreneur.CheminLogo);
                }
                else
                {
                    row.RelativeItem(1);
                }

                row.RelativeItem(2).AlignCenter().Column(col =>
                {
                    col.Item().Text("FACTURE").Bold().FontSize(24);
                    if (Facture.TypeFacture != TypeFacture.Normale)
                    {
                        col.Item().Text($"({Facture.TypeFacture})").FontSize(12).Italic();
                    }
                });

                row.RelativeItem(1).AlignRight().Column(col =>
                {
                    col.Item().Text($"N° {Facture.NumeroFacture}").Bold();
                    col.Item().Text($"Date : {Facture.DateFacture:dd/MM/yyyy}");
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            // Blocs Vendeur et Client
            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).Padding(10).Column(col =>
                {
                    col.Item().Text("VENDEUR / ÉMETTEUR").Bold().FontSize(11);
                    col.Item().PaddingTop(5);
                    if (!string.IsNullOrEmpty(Entrepreneur.RaisonSociale))
                    {
                        col.Item().Text(Entrepreneur.RaisonSociale).Bold();
                        col.Item().Text($"Représenté par : {Entrepreneur.NomComplet}");
                    }
                    else
                    {
                        col.Item().Text(Entrepreneur.NomComplet).Bold();
                    }
                    col.Item().Text(Entrepreneur.Adresse);
                    col.Item().Text($"{Entrepreneur.CodePostal} {Entrepreneur.Ville}, {Entrepreneur.Wilaya}");
                    col.Item().Text($"Tél : {Entrepreneur.Telephone}");
                    if (!string.IsNullOrEmpty(Entrepreneur.Email))
                        col.Item().Text($"Email : {Entrepreneur.Email}");
                    col.Item().PaddingTop(5);
                    col.Item().Text($"RC : {Entrepreneur.RC}");
                    col.Item().Text($"NIS : {Entrepreneur.NIS}");
                    col.Item().Text($"NIF : {Entrepreneur.NIF}");
                    col.Item().Text($"AI : {Entrepreneur.AI}");
                    col.Item().Text($"N° Immatriculation : {Entrepreneur.NumeroImmatriculation}");
                    if (Entrepreneur.EstCapitalApplicable && !string.IsNullOrEmpty(Entrepreneur.CapitalSocial))
                        col.Item().Text($"Capital social : {Entrepreneur.CapitalSocial}");
                });

                row.ConstantItem(20);

                row.RelativeItem().Border(1).Padding(10).Column(col =>
                {
                    col.Item().Text("CLIENT / DESTINATAIRE").Bold().FontSize(11);
                    col.Item().PaddingTop(5);
                    col.Item().Text(Facture.ClientNom);
                    col.Item().Text(Facture.ClientAdresse);
                    col.Item().Text($"Tél : {Facture.ClientTelephone}");
                    if (!string.IsNullOrEmpty(Facture.ClientEmail))
                        col.Item().Text($"Email : {Facture.ClientEmail}");
                    if (!string.IsNullOrEmpty(Facture.ClientRC))
                        col.Item().Text($"RC : {Facture.ClientRC}");
                    if (!string.IsNullOrEmpty(Facture.ClientNIS))
                        col.Item().Text($"NIS : {Facture.ClientNIS}");
                    if (!string.IsNullOrEmpty(Facture.ClientAI))
                        col.Item().Text($"AI : {Facture.ClientAI}");
                    if (!string.IsNullOrEmpty(Facture.ClientNumeroImmatriculation))
                        col.Item().Text($"N° Immatriculation : {Facture.ClientNumeroImmatriculation}");
                    if (!string.IsNullOrEmpty(Facture.ClientActivite))
                        col.Item().Text($"Activité : {Facture.ClientActivite}");
                });
            });

            column.Item().PaddingVertical(15);

            // Tableau des lignes
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("N°").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Désignation").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Qté").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Prix unit.").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text("TVA").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Total HT").Bold();
                });

                var lignesOrdonnees = Facture.Lignes.OrderBy(l => l.NumeroLigne).ToList();
                for (int i = 0; i < lignesOrdonnees.Count; i++)
                {
                    var ligne = lignesOrdonnees[i];
                    var bgColor = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                    table.Cell().Background(bgColor).Padding(5).Text((i + 1).ToString());
                    table.Cell().Background(bgColor).Padding(5).Text(ligne.Designation);
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text(ligne.Quantite.ToString("N2"));
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{ligne.PrixUnitaire:N2} DZD");
                    table.Cell().Background(bgColor).Padding(5).AlignCenter().Text(FormatTauxTVA(ligne.TauxTVA));
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{ligne.TotalHT:N2} DZD");
                }
            });

            column.Item().PaddingVertical(15);

            // Récapitulatif
            column.Item().AlignRight().Width(250).Border(1).Padding(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total HT :");
                    row.RelativeItem().AlignRight().Text($"{Facture.TotalHT:N2} DZD");
                });
                if (Facture.TotalTVA19 > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TVA 19% :");
                        row.RelativeItem().AlignRight().Text($"{Facture.TotalTVA19:N2} DZD");
                    });
                }
                if (Facture.TotalTVA9 > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TVA 9% :");
                        row.RelativeItem().AlignRight().Text($"{Facture.TotalTVA9:N2} DZD");
                    });
                }
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total TTC :");
                    row.RelativeItem().AlignRight().Text($"{Facture.TotalTTC:N2} DZD");
                });
                if (Facture.EstTimbreApplique)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Timbre fiscal :");
                        row.RelativeItem().AlignRight().Text($"{Facture.TimbreFiscal:N2} DZD");
                    });
                }
                col.Item().PaddingTop(5).LineHorizontal(1);
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("MONTANT TOTAL :").Bold();
                    row.RelativeItem().AlignRight().Text($"{Facture.MontantTotal:N2} DZD").Bold();
                });
            });

            column.Item().PaddingTop(10);
            column.Item().Text($"Montant en lettres : {Facture.MontantEnLettres}").Italic();
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Mode de paiement : {Facture.ModePaiement}");
                row.RelativeItem().AlignRight().Text($"Date d'échéance : {Facture.DateEcheance:dd/MM/yyyy}");
            });
            
            // Afficher les détails du paiement si présents
            if (!string.IsNullOrEmpty(Facture.PaiementReference))
            {
                column.Item().PaddingTop(5).Text($"Référence : {Facture.PaiementReference}");
            }
            column.Item().PaddingTop(10).AlignCenter()
                .Text($"Facture générée le {DateTime.Now:dd/MM/yyyy à HH:mm} par FatouraDZ")
                .FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    private static string FormatTauxTVA(TauxTVA taux) => taux switch
    {
        TauxTVA.TVA19 => "19%",
        TauxTVA.TVA9 => "9%",
        TauxTVA.Exonere => "Exonéré",
        _ => ""
    };

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
                await _pdfService.GenererPdfAsync(Facture, Entrepreneur, cheminPdf);
                
                // Mettre à jour la facture avec le chemin PDF
                Facture.CheminPDF = cheminPdf;
                await _databaseService.SaveFactureAsync(Facture);
                
                EstSauvegarde = true;
            }
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur lors de l'enregistrement : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImprimerAsync()
    {
        ErreurMessage = null;

        try
        {
            // Générer un PDF temporaire et l'ouvrir avec l'application par défaut
            var tempPath = Path.Combine(Path.GetTempPath(), $"FatouraDZ_{Facture.NumeroFacture}.pdf");
            await _pdfService.GenererPdfAsync(Facture, Entrepreneur, tempPath);
            
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
            ErreurMessage = $"Erreur lors de l'impression : {ex.Message}";
        }
    }

    [RelayCommand]
    private void Annuler()
    {
        DemanderFermeture?.Invoke();
    }
}
