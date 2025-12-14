using System;
using System.Collections.Generic;

namespace FatouraDZ.Models;

public class Facture
{
    public int Id { get; set; }
    public string NumeroFacture { get; set; } = string.Empty;
    public DateTime DateFacture { get; set; } = DateTime.Today;
    public DateTime DateEcheance { get; set; } = DateTime.Today;
    public TypeFacture TypeFacture { get; set; } = TypeFacture.Normale;
    public string ModePaiement { get; set; } = string.Empty;

    // Informations client
    public string ClientNom { get; set; } = string.Empty;
    public string ClientAdresse { get; set; } = string.Empty;
    public string ClientTelephone { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientRC { get; set; }
    public string? ClientNIS { get; set; }
    public string? ClientAI { get; set; }
    public string? ClientNumeroImmatriculation { get; set; }
    public string? ClientActivite { get; set; }

    // Montants
    public decimal TotalHT { get; set; }
    public decimal TotalTVA19 { get; set; }
    public decimal TotalTVA9 { get; set; }
    public decimal TotalTTC { get; set; }
    public decimal TimbreFiscal { get; set; }
    public bool EstTimbreApplique { get; set; }
    public decimal MontantTotal { get; set; }
    public string MontantEnLettres { get; set; } = string.Empty;

    // Métadonnées
    public string? CheminPDF { get; set; }
    public StatutFacture Statut { get; set; } = StatutFacture.EnAttente;
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime? DateModification { get; set; }

    // Navigation
    public ICollection<LigneFacture> Lignes { get; set; } = new List<LigneFacture>();
}

public enum TypeFacture
{
    Normale,
    Avoir,
    Annulation
}

public enum StatutFacture
{
    EnAttente,
    Payee,
    Annulee,
    Archivee
}
