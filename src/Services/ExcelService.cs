using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public interface IExcelService
{
    Task<string> GenererExcelAsync(Facture facture, Business business, string cheminDestination);
}

public class ExcelService : IExcelService
{
    private readonly INumberToWordsService _numberToWordsService;

    public ExcelService(INumberToWordsService numberToWordsService)
    {
        _numberToWordsService = numberToWordsService;
    }

    public Task<string> GenererExcelAsync(Facture facture, Business business, string cheminDestination)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Facture");

            // Set column widths
            worksheet.Column(1).Width = 8;
            worksheet.Column(2).Width = 40;
            worksheet.Column(3).Width = 12;
            worksheet.Column(4).Width = 15;
            worksheet.Column(5).Width = 15;

            int row = 1;

            // Header - Company Info
            var titleCell = worksheet.Cell(row, 1);
            titleCell.Value = GetInvoiceTitle(facture.TypeFacture);
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 18;
            worksheet.Range(row, 1, row, 5).Merge();
            row += 2;

            // Seller Info
            worksheet.Cell(row, 1).Value = "ÉMETTEUR";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            row++;
            
            if (business.TypeEntreprise == BusinessType.Reel)
                worksheet.Cell(row, 1).Value = business.RaisonSociale ?? business.Nom;
            else
                worksheet.Cell(row, 1).Value = business.NomComplet;
            row++;
            
            worksheet.Cell(row, 1).Value = business.Adresse;
            row++;
            worksheet.Cell(row, 1).Value = $"{business.CodePostal} {business.Ville}, {business.Wilaya}";
            row++;
            worksheet.Cell(row, 1).Value = $"Tél: {business.Telephone}";
            row++;
            
            if (business.TypeEntreprise == BusinessType.AutoEntrepreneur)
                worksheet.Cell(row, 1).Value = $"N° Immatriculation: {business.NumeroImmatriculation}";
            else
                worksheet.Cell(row, 1).Value = $"RC: {business.RC}";
            row++;
            
            worksheet.Cell(row, 1).Value = $"NIF: {business.NIF} | AI: {business.AI} | NIS: {business.NIS}";
            row += 2;

            // Invoice Info
            worksheet.Cell(row, 1).Value = "N° Facture:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value = facture.NumeroFacture;
            worksheet.Cell(row, 4).Value = "Date:";
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Value = facture.DateFacture.ToString("dd/MM/yyyy");
            row += 2;

            // Client Info
            worksheet.Cell(row, 1).Value = "CLIENT";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 1).Value = facture.ClientNom;
            row++;
            if (!string.IsNullOrEmpty(facture.ClientAdresse))
            {
                worksheet.Cell(row, 1).Value = facture.ClientAdresse;
                row++;
            }
            if (!string.IsNullOrEmpty(facture.ClientTelephone))
            {
                worksheet.Cell(row, 1).Value = $"Tél: {facture.ClientTelephone}";
                row++;
            }
            
            // Fiscal info based on client type
            if (facture.ClientBusinessType == BusinessType.AutoEntrepreneur)
            {
                if (!string.IsNullOrEmpty(facture.ClientNumeroImmatriculation))
                {
                    worksheet.Cell(row, 1).Value = $"N° Immatriculation: {facture.ClientNumeroImmatriculation}";
                    row++;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(facture.ClientRC))
                {
                    worksheet.Cell(row, 1).Value = $"RC: {facture.ClientRC}";
                    row++;
                }
            }
            
            // NIF, AI, NIS on same line
            var fiscalParts = new List<string>();
            if (!string.IsNullOrEmpty(facture.ClientNIF))
                fiscalParts.Add($"NIF: {facture.ClientNIF}");
            if (!string.IsNullOrEmpty(facture.ClientAI))
                fiscalParts.Add($"AI: {facture.ClientAI}");
            if (!string.IsNullOrEmpty(facture.ClientNIS))
                fiscalParts.Add($"NIS: {facture.ClientNIS}");
            
            if (fiscalParts.Count > 0)
            {
                worksheet.Cell(row, 1).Value = string.Join(" | ", fiscalParts);
                row++;
            }
            row++;

            // Items Header
            var headerRange = worksheet.Range(row, 1, row, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(row, 1).Value = "N°";
            worksheet.Cell(row, 2).Value = "Désignation";
            worksheet.Cell(row, 3).Value = "Quantité";
            worksheet.Cell(row, 4).Value = "Prix Unitaire";
            worksheet.Cell(row, 5).Value = "Total HT";
            row++;

            // Items
            int itemNum = 1;
            foreach (var ligne in facture.Lignes)
            {
                worksheet.Cell(row, 1).Value = itemNum++;
                worksheet.Cell(row, 2).Value = ligne.Designation;
                worksheet.Cell(row, 3).Value = ligne.Quantite;
                worksheet.Cell(row, 4).Value = ligne.PrixUnitaire;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 \"DA\"";
                worksheet.Cell(row, 5).Value = ligne.TotalHT;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 \"DA\"";
                
                // Alternate row colors
                if (itemNum % 2 == 0)
                {
                    worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
                row++;
            }

            // Add border to items table
            var tableRange = worksheet.Range(row - facture.Lignes.Count - 1, 1, row - 1, 5);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            row++;

            // Totals section
            int totalsCol = 4;
            
            worksheet.Cell(row, totalsCol).Value = "Total HT:";
            worksheet.Cell(row, totalsCol).Style.Font.Bold = true;
            worksheet.Cell(row, totalsCol + 1).Value = facture.TotalHT;
            worksheet.Cell(row, totalsCol + 1).Style.NumberFormat.Format = "#,##0.00 \"DA\"";
            row++;

            decimal totalTVA = facture.TotalTVA19 + facture.TotalTVA9;
            if (totalTVA > 0)
            {
                if (facture.TotalTVA19 > 0)
                {
                    worksheet.Cell(row, totalsCol).Value = "TVA (19%):";
                    worksheet.Cell(row, totalsCol + 1).Value = facture.TotalTVA19;
                    worksheet.Cell(row, totalsCol + 1).Style.NumberFormat.Format = "#,##0.00 \"DA\"";
                    row++;
                }
                if (facture.TotalTVA9 > 0)
                {
                    worksheet.Cell(row, totalsCol).Value = "TVA (9%):";
                    worksheet.Cell(row, totalsCol + 1).Value = facture.TotalTVA9;
                    worksheet.Cell(row, totalsCol + 1).Style.NumberFormat.Format = "#,##0.00 \"DA\"";
                    row++;
                }
            }

            worksheet.Cell(row, totalsCol).Value = "Total TTC:";
            worksheet.Cell(row, totalsCol).Style.Font.Bold = true;
            worksheet.Cell(row, totalsCol + 1).Value = facture.TotalTTC;
            worksheet.Cell(row, totalsCol + 1).Style.NumberFormat.Format = "#,##0.00 \"DA\"";
            row++;

            if (facture.TimbreFiscal > 0)
            {
                worksheet.Cell(row, totalsCol).Value = "Timbre fiscal:";
                worksheet.Cell(row, totalsCol + 1).Value = facture.TimbreFiscal;
                worksheet.Cell(row, totalsCol + 1).Style.NumberFormat.Format = "#,##0.00 \"DA\"";
                row++;
            }

            if (facture.RetenueSource > 0)
            {
                worksheet.Cell(row, totalsCol).Value = "Retenue source:";
                worksheet.Cell(row, totalsCol + 1).Value = -facture.RetenueSource;
                worksheet.Cell(row, totalsCol + 1).Style.NumberFormat.Format = "#,##0.00 \"DA\"";
                worksheet.Cell(row, totalsCol + 1).Style.Font.FontColor = XLColor.Red;
                row++;
            }

            // Net à payer
            worksheet.Cell(row, totalsCol).Value = "NET À PAYER:";
            worksheet.Cell(row, totalsCol).Style.Font.Bold = true;
            worksheet.Cell(row, totalsCol).Style.Font.FontSize = 12;
            worksheet.Cell(row, totalsCol + 1).Value = facture.MontantTotal;
            worksheet.Cell(row, totalsCol + 1).Style.NumberFormat.Format = "#,##0.00 \"DA\"";
            worksheet.Cell(row, totalsCol + 1).Style.Font.Bold = true;
            worksheet.Cell(row, totalsCol + 1).Style.Font.FontSize = 12;
            worksheet.Cell(row, totalsCol + 1).Style.Font.FontColor = XLColor.FromHtml("#2563EB");
            row += 2;

            // Amount in words
            worksheet.Cell(row, 1).Value = "Arrêté la présente facture à la somme de:";
            worksheet.Cell(row, 1).Style.Font.Italic = true;
            row++;
            worksheet.Cell(row, 1).Value = facture.MontantEnLettres;
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Range(row, 1, row, 5).Merge();
            row += 2;

            // Payment method table
            var paymentHeaderRange = worksheet.Range(row, 1, row, 3);
            paymentHeaderRange.Style.Font.Bold = true;
            paymentHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#64748B");
            paymentHeaderRange.Style.Font.FontColor = XLColor.White;
            paymentHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            paymentHeaderRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            paymentHeaderRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            worksheet.Cell(row, 1).Value = "Mode règlement";
            worksheet.Cell(row, 2).Value = "Valeur";
            worksheet.Cell(row, 3).Value = "N° Pièce";
            row++;

            // Payment data row
            var paymentDataRange = worksheet.Range(row, 1, row, 3);
            paymentDataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            paymentDataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            worksheet.Cell(row, 1).Value = facture.ModePaiement ?? "-";
            worksheet.Cell(row, 2).Value = facture.MontantTotal;
            worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 \"DA\"";
            worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            worksheet.Cell(row, 3).Value = facture.PaiementNumeroPiece ?? "-";
            worksheet.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            workbook.SaveAs(cheminDestination);
            return cheminDestination;
        });
    }

    private string GetInvoiceTitle(TypeFacture type)
    {
        return type switch
        {
            TypeFacture.Normale => "FACTURE",
            TypeFacture.Avoir => "AVOIR",
            TypeFacture.Proforma => "FACTURE PROFORMA",
            _ => "FACTURE"
        };
    }
}
