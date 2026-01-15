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
        _facture = facture;
        _business = business;
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
            // 1. SELLER INFO (top) - Left | Logo Center | Right
            column.Item().Row(row =>
            {
                // Left column - Company name and address
                row.RelativeItem().Column(col =>
                {
                    if (Business.TypeEntreprise == BusinessType.Reel)
                    {
                        col.Item().Text(Business.RaisonSociale ?? Business.Nom).Bold().FontSize(14);
                        if (!string.IsNullOrEmpty(Business.CapitalSocial))
                            col.Item().Text($"Capital : {Business.CapitalSocial}").FontSize(9);
                    }
                    else
                    {
                        col.Item().Text(Business.NomComplet).Bold().FontSize(14);
                        if (!string.IsNullOrEmpty(Business.RaisonSociale))
                            col.Item().Text($"Nom commercial : {Business.RaisonSociale}").FontSize(9);
                    }
                    
                    col.Item().Text($"{Business.Adresse}").FontSize(9);
                    col.Item().Text($"{Business.CodePostal} {Business.Ville}, {Business.Wilaya}").FontSize(9);
                    col.Item().Text($"Tél : {Business.Telephone}").FontSize(9);
                    if (!string.IsNullOrEmpty(Business.Email))
                        col.Item().Text($"Email : {Business.Email}").FontSize(9);
                });

                // Center - Logo
                if (!string.IsNullOrEmpty(Business.CheminLogo) && File.Exists(Business.CheminLogo))
                {
                    try
                    {
                        var logoBytes = File.ReadAllBytes(Business.CheminLogo);
                        row.ConstantItem(100).AlignCenter().AlignMiddle().Height(70).Image(logoBytes).FitArea();
                    }
                    catch
                    {
                        row.ConstantItem(100);
                    }
                }
                else
                {
                    row.ConstantItem(100);
                }

                // Right column - Fiscal info
                row.RelativeItem().AlignRight().Column(col =>
                {
                    if (!string.IsNullOrEmpty(Business.Activite))
                        col.Item().Text($"Activité : {Business.Activite}").FontSize(9);
                    if (Business.TypeEntreprise == BusinessType.AutoEntrepreneur)
                        col.Item().Text($"N° Immatriculation : {Business.NumeroImmatriculation}").FontSize(9);
                    else
                        col.Item().Text($"RC : {Business.RC}").FontSize(9);
                    col.Item().Text($"NIF : {Business.NIF}").FontSize(9);
                    col.Item().Text($"AI : {Business.AI}").FontSize(9);
                    col.Item().Text($"NIS : {Business.NIS}").FontSize(9);
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1);

            // 2. INVOICE TITLE AND NUMBER (center)
            column.Item().PaddingVertical(10).AlignCenter().Column(col =>
            {
                var titreFacture = Facture.TypeFacture switch
                {
                    TypeFacture.Avoir => "FACTURE D'AVOIR",
                    TypeFacture.Proforma => "FACTURE PROFORMA",
                    _ => "FACTURE"
                };
                col.Item().Text(titreFacture).Bold().FontSize(20);
                col.Item().Text($"N° {Facture.NumeroFacture}").Bold().FontSize(12);
                col.Item().Text($"Date : {Facture.DateFacture:dd/MM/yyyy}").FontSize(11);
                
                if (Facture.TypeFacture == TypeFacture.Proforma)
                {
                    col.Item().PaddingTop(3).Text("Document sans valeur comptable ni fiscale").FontSize(9).Italic();
                    if (Facture.DateValidite.HasValue)
                        col.Item().Text($"Valide jusqu'au : {Facture.DateValidite.Value:dd/MM/yyyy}").FontSize(9).Bold();
                }
                
                if (!string.IsNullOrEmpty(Facture.NumeroFactureOrigine))
                {
                    col.Item().PaddingTop(3).Text($"Réf. facture originale : {Facture.NumeroFactureOrigine}").FontSize(9).Italic();
                }
            });

            column.Item().PaddingVertical(5).LineHorizontal(1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            // 3. CLIENT INFO
            column.Item().Border(1).Padding(10).Column(col =>
            {
                col.Item().Text("CLIENT").Bold().FontSize(11);
                col.Item().PaddingTop(5);
                col.Item().Text(Facture.ClientNom).Bold();
                if (Facture.ClientBusinessType == BusinessType.Reel && !string.IsNullOrEmpty(Facture.ClientCapitalSocial))
                    col.Item().Text($"Capital : {Facture.ClientCapitalSocial}");
                col.Item().Text(Facture.ClientAdresse);
                col.Item().Text($"Tél : {Facture.ClientTelephone}");
                if (!string.IsNullOrEmpty(Facture.ClientEmail))
                    col.Item().Text($"Email : {Facture.ClientEmail}");
                if (!string.IsNullOrEmpty(Facture.ClientFax))
                    col.Item().Text($"Fax : {Facture.ClientFax}");
                
                col.Item().PaddingTop(5);
                if (!string.IsNullOrEmpty(Facture.ClientActivite))
                    col.Item().Text($"Activité : {Facture.ClientActivite}");
                
                if (Facture.ClientBusinessType == BusinessType.AutoEntrepreneur)
                {
                    if (!string.IsNullOrEmpty(Facture.ClientNumeroImmatriculation))
                        col.Item().Text($"N° Immatriculation : {Facture.ClientNumeroImmatriculation}");
                }
                else
                {
                    if (!string.IsNullOrEmpty(Facture.ClientRC))
                        col.Item().Text($"RC : {Facture.ClientRC}");
                }
                if (!string.IsNullOrEmpty(Facture.ClientNIF))
                    col.Item().Text($"NIF : {Facture.ClientNIF}");
                if (!string.IsNullOrEmpty(Facture.ClientAI))
                    col.Item().Text($"AI : {Facture.ClientAI}");
                if (!string.IsNullOrEmpty(Facture.ClientNIS))
                    col.Item().Text($"NIS : {Facture.ClientNIS}");
            });

            column.Item().PaddingVertical(15);

            // Check if any line has discount
            var hasLineDiscount = Facture.Lignes.Any(l => l.MontantRemise > 0);

            // Tableau des lignes - Réf, Désignation, Qté, Unité, Prix H.T, TVA, [Remise], Total H.T
            column.Item().Table(table =>
            {
                if (hasLineDiscount)
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // Réf
                        columns.RelativeColumn(2.5f); // Désignation
                        columns.RelativeColumn(0.7f); // Qté
                        columns.RelativeColumn(0.6f); // Unité
                        columns.RelativeColumn(1f);   // Prix H.T
                        columns.RelativeColumn(0.5f); // TVA
                        columns.RelativeColumn(0.8f); // Remise
                        columns.RelativeColumn(1.1f); // Total H.T
                    });
                }
                else
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);   // Réf
                        columns.RelativeColumn(3);    // Désignation
                        columns.RelativeColumn(0.8f); // Qté
                        columns.RelativeColumn(0.7f); // Unité
                        columns.RelativeColumn(1.2f); // Prix H.T
                        columns.RelativeColumn(0.6f); // TVA
                        columns.RelativeColumn(1.3f); // Total H.T
                    });
                }

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Réf").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Désignation").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Qté").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text("Unité").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Prix H.T").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text("TVA").Bold();
                    if (hasLineDiscount)
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Remise").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Total H.T").Bold();
                });

                var lignesOrdonnees = Facture.Lignes.OrderBy(l => l.NumeroLigne).ToList();
                for (int i = 0; i < lignesOrdonnees.Count; i++)
                {
                    var ligne = lignesOrdonnees[i];
                    var bgColor = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                    table.Cell().Background(bgColor).Padding(5).Text(ligne.Reference ?? (i + 1).ToString());
                    table.Cell().Background(bgColor).Padding(5).Text(ligne.Designation);
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text(ligne.Quantite.ToString("N2"));
                    table.Cell().Background(bgColor).Padding(5).AlignCenter().Text(FormatUnite(ligne.Unite));
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{ligne.PrixUnitaire:N2}");
                    table.Cell().Background(bgColor).Padding(5).AlignCenter().Text(FormatTauxTVA(ligne.TauxTVA));
                    if (hasLineDiscount)
                    {
                        var remiseText = ligne.MontantRemise > 0 
                            ? $"-{ligne.MontantRemise:N2}" 
                            : "-";
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text(remiseText);
                    }
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{ligne.TotalHT:N2}");
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
                if (Facture.MontantRemiseGlobale > 0)
                {
                    col.Item().Row(row =>
                    {
                        var typeRemise = Facture.TypeRemiseGlobale == TypeRemise.Pourcentage 
                            ? $"Remise globale ({Facture.RemiseGlobale}%) :" 
                            : "Remise globale :";
                        row.RelativeItem().Text(typeRemise);
                        row.RelativeItem().AlignRight().Text($"-{Facture.MontantRemiseGlobale:N2} DZD");
                    });
                }
                if (Facture.TotalTVA19 > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"TVA {AppSettings.Instance.TauxTVAStandard}% :");
                        row.RelativeItem().AlignRight().Text($"{Facture.TotalTVA19:N2} DZD");
                    });
                }
                if (Facture.TotalTVA9 > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"TVA {AppSettings.Instance.TauxTVAReduit}% :");
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
                if (Facture.TauxRetenueSource.HasValue && Facture.RetenueSource > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Retenue source ({Facture.TauxRetenueSource}% du HT) :");
                        row.RelativeItem().AlignRight().Text($"-{Facture.RetenueSource:N2} DZD");
                    });
                }
                col.Item().PaddingTop(5).LineHorizontal(1);
                var labelTotal = Facture.TypeFacture switch
                {
                    TypeFacture.Avoir => "NET À DÉDUIRE :",
                    TypeFacture.Proforma => "NET À PAYER :",
                    _ => "NET À PAYER :"
                };
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text(labelTotal).Bold();
                    row.RelativeItem().AlignRight().Text($"{Facture.MontantTotal:N2} DZD").Bold();
                });
            });

            column.Item().PaddingTop(10);
            column.Item().Text($"Montant en lettres : {Facture.MontantEnLettres}").Italic();

            column.Item().PaddingTop(15);

            // Tableau Mode de règlement
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f); // Mode règlement
                    columns.RelativeColumn(1.5f); // Valeur
                    columns.RelativeColumn(1.5f); // N° Pièce
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text("Mode règlement").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).AlignCenter().Text("Valeur").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).AlignCenter().Text("N° Pièce").Bold();
                });

                // Payment row
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(Facture.ModePaiement);
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).AlignRight().Text($"{Facture.MontantTotal:N2} DZD");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).AlignCenter().Text(Facture.PaiementNumeroPiece ?? "-");
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Date d'échéance : {Facture.DateEcheance:dd/MM/yyyy}");
            });
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

    private static string FormatUnite(Unite unite) => unite switch
    {
        Unite.PCS => "PCS",
        Unite.BOIT => "BOIT",
        Unite.KG => "KG",
        Unite.L => "L",
        Unite.M => "M",
        Unite.M2 => "M²",
        Unite.M3 => "M³",
        Unite.H => "H",
        Unite.J => "J",
        Unite.FORF => "FORF",
        _ => "PCS"
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
                await _pdfService.GenererPdfAsync(Facture, Business, cheminPdf);
                
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
            await _pdfService.GenererPdfAsync(Facture, Business, tempPath);
            
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
                EstSauvegarde = true;
            }
        }
        catch (Exception ex)
        {
            ErreurMessage = $"Erreur lors de l'enregistrement Excel : {ex.Message}";
        }
    }
}
