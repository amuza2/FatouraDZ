using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public interface IDatabaseService
{
    // Business
    Task<List<Business>> GetBusinessesAsync();
    Task<Business?> GetBusinessByIdAsync(int id);
    Task SaveBusinessAsync(Business business);
    Task DeleteBusinessAsync(int id);
    
    // Factures
    Task<List<Facture>> GetFacturesAsync();
    Task<List<Facture>> GetFacturesByBusinessIdAsync(int businessId);
    /// <summary>
    /// Factures d'une entreprise filtrées côté base de données (année, archivage, type,
    /// statut, recherche). Les lignes ne sont pas chargées : inutiles pour la liste.
    /// </summary>
    Task<List<Facture>> GetFacturesFiltreesAsync(int businessId, int annee, bool archived, TypeFacture? type, StatutFacture? statut, string? recherche);
    /// <summary>Années (distinctes) pour lesquelles l'entreprise possède des factures.</summary>
    Task<List<int>> GetAnneesFacturesAsync(int businessId);
    /// <summary>Agrégats (nombre, CA, statuts) pour une entreprise et une année.</summary>
    Task<StatistiquesFactures> GetStatistiquesFacturesAsync(int businessId, int annee);
    Task<List<Facture>> GetFacturesAsync(DateTime? dateDebut, DateTime? dateFin, TypeFacture? type, StatutFacture? statut, string? recherche);
    Task<Facture?> GetFactureByIdAsync(int id);
    Task<Facture?> GetFactureByNumeroAsync(string numero);
    Task SaveFactureAsync(Facture facture);
    Task DeleteFactureAsync(int id);
    Task UpdateStatutFactureAsync(int id, StatutFacture nouveauStatut);
    Task UpdateCheminPdfAsync(int id, string cheminPdf);
    Task<Facture> DupliquerFactureAsync(int id);
    
    // Clients
    Task<List<Client>> GetClientsByBusinessIdAsync(int businessId);
    Task<Client?> GetClientByIdAsync(int id);
    Task SaveClientAsync(Client client);
    Task DeleteClientAsync(int id);
    
    // Statistiques
    Task<int> GetNombreFacturesAsync();
    Task<decimal> GetChiffreAffairesTotalAsync();
    Task<decimal> GetMontantMoyenAsync();
    Task<List<Facture>> GetDernieresFacturesAsync(int nombre);
    
    // Transactions
    Task<List<Transaction>> GetTransactionsByBusinessIdAsync(int businessId);
    Task SaveTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(int id);
    Task ArchiveTransactionAsync(int id);
    Task<Transaction?> GetTransactionByFactureIdAsync(int factureId);
    
    // Catégories de transactions
    Task<List<CategorieTransaction>> GetCategoriesByBusinessIdAsync(int businessId);
    Task SaveCategorieAsync(CategorieTransaction categorie);
    Task DeleteCategorieAsync(int id);
    
    // Configuration
    Task<string?> GetConfigurationAsync(string cle);
    Task SetConfigurationAsync(string cle, string valeur);

    // Numérotation des factures (persistée dans la table Configuration)
    /// <summary>Retourne (sans écrire) le prochain numéro de facture pour l'année donnée.</summary>
    Task<int> LireProchainNumeroFactureAsync(int annee);
    /// <summary>
    /// Réserve atomiquement le prochain numéro de facture pour l'année donnée.
    /// Réinitialise le compteur à 1 lors d'un changement d'année.
    /// </summary>
    Task<int> ReserverProchainNumeroFactureAsync(int annee);

    // Initialisation
    Task InitializeDatabaseAsync();
    
    // Database path
    string GetDatabasePath();
}
