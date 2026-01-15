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

    public (decimal TotalHT, decimal MontantRemise) CalculerTotalHTLigneAvecRemise(decimal quantite, decimal prixUnitaire, decimal remise, TypeRemise typeRemise)
    {
        var totalBrut = Math.Round(quantite * prixUnitaire, 2);
        
        if (remise <= 0)
            return (totalBrut, 0m);

        decimal montantRemise;
        if (typeRemise == TypeRemise.Pourcentage)
        {
            // Percentage discount (capped at 100%)
            var pourcentage = Math.Min(remise, 100m);
            montantRemise = Math.Round(totalBrut * pourcentage / 100m, 2);
        }
        else
        {
            // Fixed amount discount (capped at total)
            montantRemise = Math.Min(Math.Round(remise, 2), totalBrut);
        }

        var totalHT = totalBrut - montantRemise;
        return (totalHT, montantRemise);
    }

    public (decimal MontantRemise, decimal TotalHTApresRemise) CalculerRemiseGlobale(decimal totalHT, decimal remise, TypeRemise typeRemise)
    {
        if (remise <= 0)
            return (0m, totalHT);

        decimal montantRemise;
        if (typeRemise == TypeRemise.Pourcentage)
        {
            // Percentage discount (capped at 100%)
            var pourcentage = Math.Min(remise, 100m);
            montantRemise = Math.Round(totalHT * pourcentage / 100m, 2);
        }
        else
        {
            // Fixed amount discount (capped at total)
            montantRemise = Math.Min(Math.Round(remise, 2), totalHT);
        }

        var totalHTApresRemise = totalHT - montantRemise;
        return (montantRemise, totalHTApresRemise);
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

    public (decimal TotalHT, decimal TVA19, decimal TVA9, decimal TotalTTC, decimal TimbreFiscal, decimal MontantTotal, decimal MontantRemiseGlobale) 
        CalculerTotaux(IEnumerable<LigneFacture> lignes, bool appliquerTimbre, decimal remiseGlobale = 0, TypeRemise typeRemiseGlobale = TypeRemise.Pourcentage)
    {
        decimal totalHTBrut = 0m;
        decimal tva19 = 0m;
        decimal tva9 = 0m;

        foreach (var ligne in lignes)
        {
            // Use line total which already includes line-level discount
            var ligneHT = ligne.TotalHT;
            totalHTBrut += ligneHT;

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

        // Apply global discount on Total HT
        var (montantRemiseGlobale, totalHT) = CalculerRemiseGlobale(totalHTBrut, remiseGlobale, typeRemiseGlobale);

        // Recalculate TVA after global discount (proportionally reduce TVA)
        if (totalHTBrut > 0 && montantRemiseGlobale > 0)
        {
            var ratio = totalHT / totalHTBrut;
            tva19 = Math.Round(tva19 * ratio, 2);
            tva9 = Math.Round(tva9 * ratio, 2);
        }

        var totalTTC = Math.Round(totalHT + tva19 + tva9, 2);
        var timbre = appliquerTimbre ? CalculerTimbreFiscal(totalTTC) : 0m;
        var montantTotal = totalTTC + timbre;

        return (totalHT, tva19, tva9, totalTTC, timbre, montantTotal, montantRemiseGlobale);
    }
}
