namespace FatouraDZ.Models;

public class LigneFacture
{
    public int Id { get; set; }
    public int FactureId { get; set; }
    public int NumeroLigne { get; set; }
    public string? Reference { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public Unite Unite { get; set; } = Unite.PCS;
    public decimal PrixUnitaire { get; set; }
    public TauxTVA TauxTVA { get; set; } = TauxTVA.TVA19;
    public decimal TotalHT { get; set; }

    // Navigation
    public Facture Facture { get; set; } = null!;
}

public enum TauxTVA
{
    TVA19,
    TVA9,
    Exonere
}

public enum Unite
{
    PCS,    // Pièce
    BOIT,   // Boîte
    KG,     // Kilogramme
    L,      // Litre
    M,      // Mètre
    M2,     // Mètre carré
    M3,     // Mètre cube
    H,      // Heure
    J,      // Jour
    FORF    // Forfait
}
