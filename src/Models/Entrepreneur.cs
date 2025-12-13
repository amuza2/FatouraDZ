using System;

namespace FatouraDZ.Models;

public class Entrepreneur
{
    public int Id { get; set; }
    public string NomComplet { get; set; } = string.Empty;
    public string? RaisonSociale { get; set; }
    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string Wilaya { get; set; } = string.Empty;
    public string? CodePostal { get; set; }
    public string Telephone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string RC { get; set; } = string.Empty;
    public string NIS { get; set; } = string.Empty;
    public string NIF { get; set; } = string.Empty;
    public string AI { get; set; } = string.Empty;
    public string NumeroImmatriculation { get; set; } = string.Empty;
    public string? CapitalSocial { get; set; }
    public bool EstCapitalApplicable { get; set; }
    public string? CheminLogo { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime? DateModification { get; set; }
}
