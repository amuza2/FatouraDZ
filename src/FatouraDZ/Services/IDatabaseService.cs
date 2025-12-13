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
    Task<Facture?> GetFactureByIdAsync(int id);
    Task<Facture?> GetFactureByNumeroAsync(string numero);
    Task SaveFactureAsync(Facture facture);
    Task DeleteFactureAsync(int id);
    
    // Configuration
    Task<string?> GetConfigurationAsync(string cle);
    Task SetConfigurationAsync(string cle, string valeur);
    
    // Initialisation
    Task InitializeDatabaseAsync();
}
