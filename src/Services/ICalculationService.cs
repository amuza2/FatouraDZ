using System.Collections.Generic;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public interface ICalculationService
{
    decimal CalculerTotalHTLigne(decimal quantite, decimal prixUnitaire);
    decimal CalculerTVA(decimal totalHT, TauxTVA taux);
    decimal CalculerTimbreFiscal(decimal montantTTC);
    (decimal TotalHT, decimal TVA19, decimal TVA9, decimal TotalTTC, decimal TimbreFiscal, decimal MontantTotal) 
        CalculerTotaux(IEnumerable<LigneFacture> lignes, bool appliquerTimbre);
}
