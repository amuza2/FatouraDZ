using System;
using System.Collections.Generic;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public class CalculationService : ICalculationService
{
    public decimal CalculerTotalHTLigne(decimal quantite, decimal prixUnitaire)
    {
        return Math.Round(quantite * prixUnitaire, 2);
    }

    public decimal CalculerTVA(decimal totalHT, TauxTVA taux)
    {
        var settings = AppSettings.Instance;
        return taux switch
        {
            TauxTVA.TVA19 => Math.Round(totalHT * (settings.TauxTVAStandard / 100m), 2),
            TauxTVA.TVA9 => Math.Round(totalHT * (settings.TauxTVAReduit / 100m), 2),
            TauxTVA.Exonere => 0m,
            _ => 0m
        };
    }

    public decimal CalculerTimbreFiscal(decimal montantTTC)
    {
        var settings = AppSettings.Instance;
        
        if (montantTTC <= 0)
            return 0m;

        var timbre = Math.Round(montantTTC * (settings.TauxTimbreFiscal / 100m), 2);
        
        // Apply maximum limit from settings
        timbre = Math.Min(timbre, settings.MontantMaxTimbre);
        
        // Minimum légal : 5 DA
        return Math.Max(timbre, 5m);
    }

    public (decimal TotalHT, decimal TVA19, decimal TVA9, decimal TotalTTC, decimal TimbreFiscal, decimal MontantTotal) 
        CalculerTotaux(IEnumerable<LigneFacture> lignes, bool appliquerTimbre)
    {
        decimal totalHT = 0m;
        decimal tva19 = 0m;
        decimal tva9 = 0m;

        foreach (var ligne in lignes)
        {
            var ligneHT = CalculerTotalHTLigne(ligne.Quantite, ligne.PrixUnitaire);
            totalHT += ligneHT;

            switch (ligne.TauxTVA)
            {
                case TauxTVA.TVA19:
                    tva19 += CalculerTVA(ligneHT, TauxTVA.TVA19);
                    break;
                case TauxTVA.TVA9:
                    tva9 += CalculerTVA(ligneHT, TauxTVA.TVA9);
                    break;
            }
        }

        var totalTTC = Math.Round(totalHT + tva19 + tva9, 2);
        var timbre = appliquerTimbre ? CalculerTimbreFiscal(totalTTC) : 0m;
        var montantTotal = totalTTC + timbre;

        return (totalHT, tva19, tva9, totalTTC, timbre, montantTotal);
    }
}
