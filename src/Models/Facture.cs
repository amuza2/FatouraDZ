using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FatouraDZ.Models;

public class Facture : INotifyPropertyChanged
{
    private StatutFacture _statut = StatutFacture.EnAttente;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; set; }
    public string NumeroFacture { get; set; } = string.Empty;
    public DateTime DateFacture { get; set; } = DateTime.Today;
    public DateTime DateEcheance { get; set; } = DateTime.Today;
    public DateTime? DateValidite { get; set; } // Validity date for Proforma invoices
    public TypeFacture TypeFacture { get; set; } = TypeFacture.Normale;
    public string ModePaiement { get; set; } = string.Empty;
    
    // Détails du paiement - Mode reglement table
    public string? PaiementReference { get; set; }
    public decimal PaiementValeur { get; set; }
    public string? PaiementNumeroPiece { get; set; }

    // Informations client
    public BusinessType ClientBusinessType { get; set; } = BusinessType.AutoEntrepreneur;
    public string ClientNom { get; set; } = string.Empty;
    public string ClientAdresse { get; set; } = string.Empty;
    public string ClientTelephone { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientFormeJuridique { get; set; }
    public string? ClientRC { get; set; }
    public string? ClientNIS { get; set; }
    public string? ClientNIF { get; set; }
    public string? ClientAI { get; set; }
    public string? ClientNumeroImmatriculation { get; set; }
    public string? ClientActivite { get; set; }
    public string? ClientFax { get; set; }
    public string? ClientCapitalSocial { get; set; }

    // Référence à la facture originale (pour avoir/annulation)
    public string? NumeroFactureOrigine { get; set; }

    // Retenue à la source (optionnel)
    public decimal? TauxRetenueSource { get; set; }
    public decimal RetenueSource { get; set; }

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
    
    public StatutFacture Statut
    {
        get => _statut;
        set
        {
            if (_statut != value)
            {
                _statut = value;
                OnPropertyChanged();
            }
        }
    }
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime? DateModification { get; set; }
    
    // Archive status
    public bool IsArchived { get; set; } = false;

    // Foreign key to Business
    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    // Navigation
    public ICollection<LigneFacture> Lignes { get; set; } = new List<LigneFacture>();

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum TypeFacture
{
    Normale,
    Avoir,
    Proforma
}

public enum StatutFacture
{
    EnAttente,
    Payee,
    Annulee,
    Archivee
}
