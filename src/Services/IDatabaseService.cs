using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public interface IDatabaseService
{
    // Entrepreneur
    Task<Entrepreneur?> GetEntrepreneurAsync();
    Task SaveEntrepreneurAsync(Entrepreneur entrepreneur);
    
    // Factures
    Task<List<Facture>> GetFacturesAsync();
    Task<List<Facture>> GetFacturesAsync(DateTime? dateDebut, DateTime? dateFin, TypeFacture? type, StatutFacture? statut, string? recherche);
    Task<Facture?> GetFactureByIdAsync(int id);
    Task<Facture?> GetFactureByNumeroAsync(string numero);
    Task SaveFactureAsync(Facture facture);
    Task DeleteFactureAsync(int id);
    Task<Facture> DupliquerFactureAsync(int id);
    
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
}
