namespace FatouraDZ.Models;

/// <summary>
/// Agrégats affichés sur l'écran d'historique d'une entreprise pour une année donnée.
/// Calculés côté base de données pour éviter de charger toutes les factures en mémoire.
/// </summary>
public class StatistiquesFactures
{
    public int NombreFactures { get; set; }
    public decimal ChiffreAffaires { get; set; }
    public int FacturesEnAttente { get; set; }
    public int FacturesPayees { get; set; }
}
