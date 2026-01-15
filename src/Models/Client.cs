using System;
using System.Collections.Generic;

namespace FatouraDZ.Models;

public class Client
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    
    // Type de client
    public BusinessType TypeClient { get; set; } = BusinessType.Reel;
    
    // Informations générales
    public string Nom { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Fax { get; set; }
    
    // Informations fiscales
    public string? FormeJuridique { get; set; }
    public string? RC { get; set; }
    public string? NIS { get; set; }
    public string? NIF { get; set; }
    public string? AI { get; set; }
    public string? NumeroImmatriculation { get; set; } // Pour auto-entrepreneur
    public string? Activite { get; set; }
    public string? CapitalSocial { get; set; }
    
    // Métadonnées
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime? DateModification { get; set; }
    
    // Navigation
    public Business Business { get; set; } = null!;
    public List<Facture> Factures { get; set; } = new();
}
