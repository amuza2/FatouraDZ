using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public class PdfService : IPdfService
{
    private readonly INumberToWordsService _numberToWordsService;

    public PdfService(INumberToWordsService numberToWordsService)
    {
        _numberToWordsService = numberToWordsService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<string> GenererPdfAsync(Facture facture, Business business, string cheminDestination)
    {
        return Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, facture, business));
                    page.Content().Element(c => ComposeContent(c, facture, business));
                    page.Footer().Element(c => ComposeFooter(c, facture));
                });
            }).GeneratePdf(cheminDestination);

            return cheminDestination;
        });
    }

    private void ComposeHeader(IContainer container, Facture facture, Business business)
    {
        container.Column(column =>
        {
            // 1. SELLER INFO (top) - Left | Logo Center | Right
            column.Item().Row(row =>
            {
                // Left column - Company name and address
                row.RelativeItem().Column(col =>
                {
                    if (business.TypeEntreprise == BusinessType.Reel)
                    {
                        col.Item().Text(business.RaisonSociale ?? business.Nom).Bold().FontSize(14);
                        if (!string.IsNullOrEmpty(business.CapitalSocial))
                            col.Item().Text($"Capital : {business.CapitalSocial}").FontSize(9);
                    }
                    else
                    {
                        col.Item().Text(business.NomComplet).Bold().FontSize(14);
                        if (!string.IsNullOrEmpty(business.RaisonSociale))
                            col.Item().Text($"Nom commercial : {business.RaisonSociale}").FontSize(9);
                    }
                    
                    col.Item().Text($"{business.Adresse}").FontSize(9);
                    col.Item().Text($"{business.CodePostal} {business.Ville}, {business.Wilaya}").FontSize(9);
                    col.Item().Text($"Tél : {business.Telephone}").FontSize(9);
                    if (!string.IsNullOrEmpty(business.Email))
                        col.Item().Text($"Email : {business.Email}").FontSize(9);
                });

                // Center - Logo
                if (!string.IsNullOrEmpty(business.CheminLogo) && File.Exists(business.CheminLogo))
                {
                    try
                    {
                        var logoBytes = File.ReadAllBytes(business.CheminLogo);
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
                    if (!string.IsNullOrEmpty(business.Activite))
                        col.Item().Text($"Activité : {business.Activite}").FontSize(9);
                    if (business.TypeEntreprise == BusinessType.AutoEntrepreneur)
                        col.Item().Text($"N° Immatriculation : {business.NumeroImmatriculation}").FontSize(9);
                    else
                        col.Item().Text($"RC : {business.RC}").FontSize(9);
                    col.Item().Text($"NIF : {business.NIF}").FontSize(9);
                    col.Item().Text($"AI : {business.AI}").FontSize(9);
                    col.Item().Text($"NIS : {business.NIS}").FontSize(9);
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1);
            
            // Add proforma notice if applicable
            if (facture.TypeFacture == TypeFacture.Proforma)
            {
                column.Item().PaddingVertical(5).AlignCenter().Text("Document sans valeur comptable ni fiscale").FontSize(9).Italic();
            }
        });
    }

    private void ComposeContent(IContainer container, Facture facture, Business business)
    {
        container.Column(column =>
        {
            // 3. CLIENT INFO - Split into two columns like seller section
            column.Item().Border(1).Padding(10).Row(row =>
            {
                // Left column - Client name and contact info
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("CLIENT").Bold().FontSize(11);
                    col.Item().PaddingTop(5);
                    col.Item().Text(facture.ClientNom).Bold().FontSize(12);
                    if (facture.ClientBusinessType == BusinessType.Reel && !string.IsNullOrEmpty(facture.ClientCapitalSocial))
                        col.Item().Text($"Capital : {facture.ClientCapitalSocial}").FontSize(9);
                    col.Item().Text(facture.ClientAdresse).FontSize(9);
                    col.Item().Text($"Tél : {facture.ClientTelephone}").FontSize(9);
                    if (!string.IsNullOrEmpty(facture.ClientEmail))
                        col.Item().Text($"Email : {facture.ClientEmail}").FontSize(9);
                    if (!string.IsNullOrEmpty(facture.ClientFax))
                        col.Item().Text($"Fax : {facture.ClientFax}").FontSize(9);
                });

                // Center - Invoice number and date
                row.ConstantItem(150).AlignCenter().Column(col =>
                {
                    var titreFacture = facture.TypeFacture switch
                    {
                        TypeFacture.Avoir => "AVOIR",
                        TypeFacture.Proforma => "PROFORMA",
                        _ => "FACTURE"
                    };
                    col.Item().AlignCenter().Text(titreFacture).Bold().FontSize(14);
                    col.Item().AlignCenter().Text($"N° {facture.NumeroFacture}").Bold().FontSize(11);
                    col.Item().AlignCenter().Text($"{facture.DateFacture:dd/MM/yyyy}").FontSize(10);
                    
                    if (facture.TypeFacture == TypeFacture.Proforma && facture.DateValidite.HasValue)
                        col.Item().AlignCenter().PaddingTop(3).Text($"Valide jusqu'au : {facture.DateValidite.Value:dd/MM/yyyy}").FontSize(8).Italic();
                    
                    if (!string.IsNullOrEmpty(facture.NumeroFactureOrigine))
                        col.Item().AlignCenter().PaddingTop(3).Text($"Réf: {facture.NumeroFactureOrigine}").FontSize(8).Italic();
                });

                // Right column - Fiscal info
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("INFORMATIONS FISCALES").Bold().FontSize(9);
                    col.Item().PaddingTop(5);
                    if (!string.IsNullOrEmpty(facture.ClientActivite))
                        col.Item().Text($"Activité : {facture.ClientActivite}").FontSize(9);
                    
                    if (facture.ClientBusinessType == BusinessType.AutoEntrepreneur)
                    {
                        if (!string.IsNullOrEmpty(facture.ClientNumeroImmatriculation))
                            col.Item().Text($"N° Immat. : {facture.ClientNumeroImmatriculation}").FontSize(9);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(facture.ClientRC))
                            col.Item().Text($"RC : {facture.ClientRC}").FontSize(9);
                    }
                    if (!string.IsNullOrEmpty(facture.ClientNIF))
                        col.Item().Text($"NIF : {facture.ClientNIF}").FontSize(9);
                    if (!string.IsNullOrEmpty(facture.ClientAI))
                        col.Item().Text($"AI : {facture.ClientAI}").FontSize(9);
                    if (!string.IsNullOrEmpty(facture.ClientNIS))
                        col.Item().Text($"NIS : {facture.ClientNIS}").FontSize(9);
                });
            });

            column.Item().PaddingVertical(15);

            // Check if any line has discount
            var hasLineDiscount = facture.Lignes.Any(l => l.MontantRemise > 0);

            // Tableau des lignes - Ref, Désignation, Quantité, Unité, Prix H.T, TVA%, Remise, Total H.T
            column.Item().Table(table =>
            {
                if (hasLineDiscount)
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // Ref
                        columns.RelativeColumn(2.5f); // Désignation
                        columns.RelativeColumn(0.7f); // Quantité
                        columns.RelativeColumn(0.6f); // Unité
                        columns.RelativeColumn(1f);   // Prix H.T
                        columns.RelativeColumn(0.5f); // TVA %
                        columns.RelativeColumn(0.8f); // Remise
                        columns.RelativeColumn(1.1f); // Total H.T
                    });
                }
                else
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);   // Ref
                        columns.RelativeColumn(3);    // Désignation
                        columns.RelativeColumn(0.8f); // Quantité
                        columns.RelativeColumn(0.7f); // Unité
                        columns.RelativeColumn(1.2f); // Prix H.T
                        columns.RelativeColumn(0.6f); // TVA %
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

                var lignesOrdonnees = facture.Lignes.OrderBy(l => l.NumeroLigne).ToList();
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
                    row.RelativeItem().AlignRight().Text($"{facture.TotalHT:N2} DZD");
                });
                if (facture.MontantRemiseGlobale > 0)
                {
                    col.Item().Row(row =>
                    {
                        var typeRemise = facture.TypeRemiseGlobale == TypeRemise.Pourcentage 
                            ? $"Remise globale ({facture.RemiseGlobale}%) :" 
                            : "Remise globale :";
                        row.RelativeItem().Text(typeRemise);
                        row.RelativeItem().AlignRight().Text($"-{facture.MontantRemiseGlobale:N2} DZD");
                    });
                }
                if (facture.TotalTVA19 > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"TVA {AppSettings.Instance.TauxTVAStandard}% :");
                        row.RelativeItem().AlignRight().Text($"{facture.TotalTVA19:N2} DZD");
                    });
                }
                if (facture.TotalTVA9 > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"TVA {AppSettings.Instance.TauxTVAReduit}% :");
                        row.RelativeItem().AlignRight().Text($"{facture.TotalTVA9:N2} DZD");
                    });
                }
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total TTC :");
                    row.RelativeItem().AlignRight().Text($"{facture.TotalTTC:N2} DZD");
                });
                if (facture.EstTimbreApplique)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Timbre fiscal :");
                        row.RelativeItem().AlignRight().Text($"{facture.TimbreFiscal:N2} DZD");
                    });
                }
                if (facture.TauxRetenueSource.HasValue && facture.RetenueSource > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Retenue source ({facture.TauxRetenueSource}% du HT) :");
                        row.RelativeItem().AlignRight().Text($"-{facture.RetenueSource:N2} DZD");
                    });
                }
                col.Item().PaddingTop(5).LineHorizontal(1);
                var labelTotal = facture.TypeFacture switch
                {
                    TypeFacture.Avoir => "NET À DÉDUIRE :",
                    TypeFacture.Proforma => "NET À PAYER :",
                    _ => "NET À PAYER :"
                };
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text(labelTotal).Bold();
                    row.RelativeItem().AlignRight().Text($"{facture.MontantTotal:N2} DZD").Bold();
                });
            });

            column.Item().PaddingTop(10);
            column.Item().Text($"Montant en lettres : {facture.MontantEnLettres}").Italic();

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
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(facture.ModePaiement);
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).AlignRight().Text($"{facture.MontantTotal:N2} DZD");
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).AlignCenter().Text(facture.PaiementNumeroPiece ?? "-");
            });
        });
    }

    private void ComposeFooter(IContainer container, Facture facture)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Date d'échéance : {facture.DateEcheance:dd/MM/yyyy}");
            });
            
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
}
