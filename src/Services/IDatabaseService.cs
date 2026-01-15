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
    Task<List<Facture>> GetFacturesAsync(DateTime? dateDebut, DateTime? dateFin, TypeFacture? type, StatutFacture? statut, string? recherche);
    Task<Facture?> GetFactureByIdAsync(int id);
    Task<Facture?> GetFactureByNumeroAsync(string numero);
    Task SaveFactureAsync(Facture facture);
    Task DeleteFactureAsync(int id);
    Task UpdateStatutFactureAsync(int id, StatutFacture nouveauStatut);
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
    
    // Configuration
    Task<string?> GetConfigurationAsync(string cle);
    Task SetConfigurationAsync(string cle, string valeur);
    
    // Initialisation
    Task InitializeDatabaseAsync();
    
    // Database path
    string GetDatabasePath();
}
