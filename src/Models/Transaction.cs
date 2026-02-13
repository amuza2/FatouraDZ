using System;

namespace FatouraDZ.Models;

public enum TypeTransaction
{
    Recette = 0,
    Depense = 1
}

public class Transaction
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string Description { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public TypeTransaction Type { get; set; }
    public string Categorie { get; set; } = string.Empty;
    
    // Optional link to invoice (for auto-created recettes)
    public int? FactureId { get; set; }
    public string? NumeroFacture { get; set; }
    
    public bool IsArchived { get; set; } = false;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime? DateModification { get; set; }
    
    // Navigation
    public Business Business { get; set; } = null!;
}
