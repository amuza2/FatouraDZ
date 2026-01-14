using System;
using System.Collections.Generic;

namespace FatouraDZ.Models;

public class Business
{
    public int Id { get; set; }
    
    // Business name (displayed in the list)
    public string Nom { get; set; } = string.Empty;
    
    // Business type determines which fields are required/shown
    public BusinessType TypeEntreprise { get; set; } = BusinessType.AutoEntrepreneur;
    
    // For Auto-Entrepreneur and Forfait: full name of owner
    // For Reel (Company): not used (use RaisonSociale instead)
    public string NomComplet { get; set; } = string.Empty;
    
    // Brand name (optional for Auto-Entrepreneur/Forfait, Company name for Reel)
    public string? RaisonSociale { get; set; }
    
    public string? Activite { get; set; }
    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string Wilaya { get; set; } = string.Empty;
    public string? CodePostal { get; set; }
    public string Telephone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Fax { get; set; }
    
    // RC (Registre Commerce) - used for Forfait and Reel
    public string RC { get; set; } = string.Empty;
    
    public string NIS { get; set; } = string.Empty;
    public string NIF { get; set; } = string.Empty;
    public string AI { get; set; } = string.Empty;
    
    // N° Immatriculation - used only for Auto-Entrepreneur
    public string NumeroImmatriculation { get; set; } = string.Empty;
    
    // Capital social - only for Reel (Company)
    public string? CapitalSocial { get; set; }
    
    public string? CheminLogo { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime? DateModification { get; set; }
    
    // Navigation property - invoices belonging to this business
    public ICollection<Facture> Factures { get; set; } = new List<Facture>();
}
