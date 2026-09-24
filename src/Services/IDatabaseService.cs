using System;
using System.Collections.Generic;
using System.Threading;
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
    /// <summary>Bascule le statut d'archivage d'une entreprise (sans passer par SaveBusinessAsync).</summary>
    Task ArchiveBusinessAsync(int id);
    
    // Factures
    Task<List<Facture>> GetFacturesAsync();
    Task<List<Facture>> GetFacturesByBusinessIdAsync(int businessId);
    /// <summary>
    /// Factures d'une entreprise filtrées côté base de données (année, archivage, type,
    /// statut, recherche). Les lignes ne sont pas chargées : inutiles pour la liste.
    /// </summary>
    Task<List<Facture>> GetFacturesFiltreesAsync(int businessId, int annee, bool archived, TypeFacture? type, StatutFacture? statut, string? recherche, CancellationToken cancellationToken = default);
    /// <summary>Années (distinctes) pour lesquelles l'entreprise possède des factures.</summary>
    Task<List<int>> GetAnneesFacturesAsync(int businessId);
    /// <summary>Factures rattachées à un client enregistré (lien Facture.ClientId).</summary>
    Task<List<Facture>> GetFacturesByClientIdAsync(int clientId);
    /// <summary>Agrégats (nombre, CA, statuts) pour une entreprise et une année.</summary>
    Task<StatistiquesFactures> GetStatistiquesFacturesAsync(int businessId, int annee);
    Task<List<Facture>> GetFacturesAsync(DateTime? dateDebut, DateTime? dateFin, TypeFacture? type, StatutFacture? statut, string? recherche);
    Task<Facture?> GetFactureByIdAsync(int id);
    Task<Facture?> GetFactureByNumeroAsync(string numero);
    Task SaveFactureAsync(Facture facture);
    Task DeleteFactureAsync(int id);
    Task UpdateStatutFactureAsync(int id, StatutFacture nouveauStatut);
    /// <summary>Bascule l'archivage d'une facture sans toucher à ses lignes.</summary>
    Task ArchiveFactureAsync(int id);
    Task UpdateCheminPdfAsync(int id, string cheminPdf);
    Task<Facture> DupliquerFactureAsync(int id);

    // Journal d'audit (qui a fait quoi, quand)
    Task AjouterJournalAsync(JournalAudit entree);
    Task<List<JournalAudit>> GetJournalAsync(string entiteType, int entiteId);
    
    // Clients
    Task<List<Client>> GetClientsByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
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
    /// <summary>
    /// Transactions filtrées et paginées côté base de données (archivage, plage de dates,
    /// type, catégorie). La plage de dates inclut le jour de fin.
    /// </summary>
    Task<List<Transaction>> GetTransactionsFiltreesAsync(int businessId, bool afficherArchivees, DateTime debut, DateTime fin, TypeTransaction? type, string? categorie, int skip, int take);
    /// <summary>Nombre de transactions correspondant aux mêmes filtres (pour la pagination).</summary>
    Task<int> GetNombreTransactionsFiltreesAsync(int businessId, bool afficherArchivees, DateTime debut, DateTime fin, TypeTransaction? type, string? categorie);
    /// <summary>Totaux des recettes et dépenses non archivées sur une plage de dates.</summary>
    Task<(decimal Recettes, decimal Depenses)> GetTotauxTransactionsAsync(int businessId, DateTime debut, DateTime fin);
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
