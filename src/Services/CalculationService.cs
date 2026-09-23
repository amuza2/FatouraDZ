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

        // Droit de timbre sur les règlements en espèces, calculé sur le montant TTC.
        // Barème progressif (Loi de Finances 2025, art. 100 du code du timbre) :
        //   <= 300 DA             : exonéré
        //   300 DA  -> 30 000 DA  : 1 %
        //   30 000  -> 100 000 DA : 1,5 %
        //   > 100 000 DA          : 2 %
        // Minimum de perception : 5 DA. Aucun plafond.
        if (montantTTC <= settings.TimbreSeuilExoneration)
            return 0m;

        var taux = montantTTC <= settings.TimbreSeuil1 ? settings.TimbreTaux1
                 : montantTTC <= settings.TimbreSeuil2 ? settings.TimbreTaux2
                 : settings.TimbreTaux3;

        var timbre = Math.Round(montantTTC * (taux / 100m), 2);

        return Math.Max(timbre, settings.TimbreMinimum);
    }

    public (decimal TotalHT, decimal TVA19, decimal TVA9, decimal TotalTTC, decimal TimbreFiscal, decimal MontantTotal, decimal MontantRemiseGlobale) 
        CalculerTotaux(IEnumerable<LigneFacture> lignes, bool appliquerTimbre, decimal remiseGlobale = 0, TypeRemise typeRemiseGlobale = TypeRemise.Pourcentage)
    {
        decimal totalHTBrut = 0m;
        decimal tva19 = 0m;
        decimal tva9 = 0m;

        foreach (var ligne in lignes)
        {
            // Compute the line total from its own fields (including line-level discount)
            // so the service does not depend on callers pre-populating LigneFacture.TotalHT.
            var (ligneHT, _) = CalculerTotalHTLigneAvecRemise(
                ligne.Quantite, ligne.PrixUnitaire, ligne.Remise, ligne.TypeRemise);
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
