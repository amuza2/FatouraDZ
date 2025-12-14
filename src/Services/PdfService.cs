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

    public Task<string> GenererPdfAsync(Facture facture, Entrepreneur entrepreneur, string cheminDestination)
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

                    page.Header().Element(c => ComposeHeader(c, facture, entrepreneur));
                    page.Content().Element(c => ComposeContent(c, facture, entrepreneur));
                    page.Footer().Element(c => ComposeFooter(c, facture));
                });
            }).GeneratePdf(cheminDestination);

            return cheminDestination;
        });
    }

    private void ComposeHeader(IContainer container, Facture facture, Entrepreneur entrepreneur)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                if (!string.IsNullOrEmpty(entrepreneur.CheminLogo) && File.Exists(entrepreneur.CheminLogo))
                {
                    row.RelativeItem(1).Height(60).Image(entrepreneur.CheminLogo);
                }
                else
                {
                    row.RelativeItem(1);
                }

                row.RelativeItem(2).AlignCenter().Column(col =>
                {
                    col.Item().Text("FACTURE").Bold().FontSize(24);
                    if (facture.TypeFacture != TypeFacture.Normale)
                    {
                        col.Item().Text($"({facture.TypeFacture})").FontSize(12).Italic();
                    }
                });

                row.RelativeItem(1).AlignRight().Column(col =>
                {
                    col.Item().Text($"N° {facture.NumeroFacture}").Bold();
                    col.Item().Text($"Date : {facture.DateFacture:dd/MM/yyyy}");
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1);
        });
    }

    private void ComposeContent(IContainer container, Facture facture, Entrepreneur entrepreneur)
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
                    if (!string.IsNullOrEmpty(entrepreneur.RaisonSociale))
                    {
                        col.Item().Text(entrepreneur.RaisonSociale).Bold();
                        col.Item().Text($"Représenté par : {entrepreneur.NomComplet}");
                    }
                    else
                    {
                        col.Item().Text(entrepreneur.NomComplet).Bold();
                    }
                    col.Item().Text(entrepreneur.Adresse);
                    col.Item().Text($"{entrepreneur.CodePostal} {entrepreneur.Ville}, {entrepreneur.Wilaya}");
                    col.Item().Text($"Tél : {entrepreneur.Telephone}");
                    if (!string.IsNullOrEmpty(entrepreneur.Email))
                        col.Item().Text($"Email : {entrepreneur.Email}");
                    col.Item().PaddingTop(5);
                    col.Item().Text($"RC : {entrepreneur.RC}");
                    col.Item().Text($"NIS : {entrepreneur.NIS}");
                    col.Item().Text($"NIF : {entrepreneur.NIF}");
                    col.Item().Text($"AI : {entrepreneur.AI}");
                    col.Item().Text($"N° Immatriculation : {entrepreneur.NumeroImmatriculation}");
                    if (entrepreneur.EstCapitalApplicable && !string.IsNullOrEmpty(entrepreneur.CapitalSocial))
                        col.Item().Text($"Capital social : {entrepreneur.CapitalSocial}");
                });

                row.ConstantItem(20);

                row.RelativeItem().Border(1).Padding(10).Column(col =>
                {
                    col.Item().Text("CLIENT / DESTINATAIRE").Bold().FontSize(11);
                    col.Item().PaddingTop(5);
                    col.Item().Text(facture.ClientNom);
                    col.Item().Text(facture.ClientAdresse);
                    col.Item().Text($"Tél : {facture.ClientTelephone}");
                    if (!string.IsNullOrEmpty(facture.ClientEmail))
                        col.Item().Text($"Email : {facture.ClientEmail}");
                    if (!string.IsNullOrEmpty(facture.ClientRC))
                        col.Item().Text($"RC : {facture.ClientRC}");
                    if (!string.IsNullOrEmpty(facture.ClientNIS))
                        col.Item().Text($"NIS : {facture.ClientNIS}");
                    if (!string.IsNullOrEmpty(facture.ClientAI))
                        col.Item().Text($"AI : {facture.ClientAI}");
                    if (!string.IsNullOrEmpty(facture.ClientNumeroImmatriculation))
                        col.Item().Text($"N° Immatriculation : {facture.ClientNumeroImmatriculation}");
                    if (!string.IsNullOrEmpty(facture.ClientActivite))
                        col.Item().Text($"Activité : {facture.ClientActivite}");
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

                var lignesOrdonnees = facture.Lignes.OrderBy(l => l.NumeroLigne).ToList();
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
                    row.RelativeItem().AlignRight().Text($"{facture.TotalHT:N2} DZD");
                });
                if (facture.TotalTVA19 > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TVA 19% :");
                        row.RelativeItem().AlignRight().Text($"{facture.TotalTVA19:N2} DZD");
                    });
                }
                if (facture.TotalTVA9 > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TVA 9% :");
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
                col.Item().PaddingTop(5).LineHorizontal(1);
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("MONTANT TOTAL :").Bold();
                    row.RelativeItem().AlignRight().Text($"{facture.MontantTotal:N2} DZD").Bold();
                });
            });

            column.Item().PaddingTop(10);
            column.Item().Text($"Montant en lettres : {facture.MontantEnLettres}").Italic();
        });
    }

    private void ComposeFooter(IContainer container, Facture facture)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Mode de paiement : {facture.ModePaiement}");
                row.RelativeItem().AlignRight().Text($"Date d'échéance : {facture.DateEcheance:dd/MM/yyyy}");
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
}
