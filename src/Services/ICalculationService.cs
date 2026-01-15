using System.Collections.Generic;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public interface ICalculationService
{
    decimal CalculerTotalHTLigne(decimal quantite, decimal prixUnitaire);
    (decimal TotalHT, decimal MontantRemise) CalculerTotalHTLigneAvecRemise(decimal quantite, decimal prixUnitaire, decimal remise, TypeRemise typeRemise);
    (decimal MontantRemise, decimal TotalHTApresRemise) CalculerRemiseGlobale(decimal totalHT, decimal remise, TypeRemise typeRemise);
    decimal CalculerTVA(decimal totalHT, TauxTVA taux);
    decimal CalculerTimbreFiscal(decimal montantTTC);
    (decimal TotalHT, decimal TVA19, decimal TVA9, decimal TotalTTC, decimal TimbreFiscal, decimal MontantTotal, decimal MontantRemiseGlobale) 
        CalculerTotaux(IEnumerable<LigneFacture> lignes, bool appliquerTimbre, decimal remiseGlobale = 0, TypeRemise typeRemiseGlobale = TypeRemise.Pourcentage);
}
