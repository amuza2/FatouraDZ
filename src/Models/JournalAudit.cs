using System;

namespace FatouraDZ.Models;

/// <summary>
/// Entrée du journal d'audit : trace « qui a fait quoi, quand » sur les données sensibles
/// (aujourd'hui les factures). Sert de piste d'audit en cas de contrôle ou de litige.
/// </summary>
public class JournalAudit
{
    public int Id { get; set; }

    /// <summary>Type d'entité concernée (ex. « Facture »).</summary>
    public string EntiteType { get; set; } = string.Empty;

    /// <summary>Identifiant de l'entité concernée.</summary>
    public int EntiteId { get; set; }

    /// <summary>Référence lisible (ex. numéro de facture).</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Action réalisée (Création, Modification, Paiement, Archivage, Restauration...).</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Détail des changements constatés.</summary>
    public string? Details { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    /// <summary>Utilisateur à l'origine de l'action (application mono-utilisateur).</summary>
    public string Utilisateur { get; set; } = "Utilisateur local";
}
