namespace FatouraDZ.Models;

public class LigneFacture
{
    public int Id { get; set; }
    public int FactureId { get; set; }
    public int NumeroLigne { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
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
