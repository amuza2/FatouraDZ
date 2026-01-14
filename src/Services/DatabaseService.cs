using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FatouraDZ.Database;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public class DatabaseService : IDatabaseService
{
    public async Task InitializeDatabaseAsync()
    {
        await using var context = new AppDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    // Business
    public async Task<List<Business>> GetBusinessesAsync()
    {
        await using var context = new AppDbContext();
        return await context.Businesses
            .OrderBy(b => b.Nom)
            .ToListAsync();
    }

    public async Task<Business?> GetBusinessByIdAsync(int id)
    {
        await using var context = new AppDbContext();
        return await context.Businesses
            .Include(b => b.Factures)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task SaveBusinessAsync(Business business)
    {
        await using var context = new AppDbContext();
        
        if (business.Id == 0)
        {
            business.DateCreation = DateTime.Now;
            context.Businesses.Add(business);
        }
        else
        {
            business.DateModification = DateTime.Now;
            var existing = await context.Businesses.FindAsync(business.Id);
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(business);
            }
        }
        
        await context.SaveChangesAsync();
    }

    public async Task DeleteBusinessAsync(int id)
    {
        await using var context = new AppDbContext();
        var business = await context.Businesses.FindAsync(id);
        if (business != null)
        {
            context.Businesses.Remove(business);
            await context.SaveChangesAsync();
        }
    }

    // Factures
    public async Task<List<Facture>> GetFacturesAsync()
    {
        await using var context = new AppDbContext();
        return await context.Factures
            .Include(f => f.Lignes)
            .OrderByDescending(f => f.DateCreation)
            .ToListAsync();
    }

    public async Task<List<Facture>> GetFacturesByBusinessIdAsync(int businessId)
    {
        await using var context = new AppDbContext();
        return await context.Factures
            .Include(f => f.Lignes)
            .Where(f => f.BusinessId == businessId)
            .OrderByDescending(f => f.DateCreation)
            .ToListAsync();
    }

    public async Task<Facture?> GetFactureByIdAsync(int id)
    {
        await using var context = new AppDbContext();
        return await context.Factures
            .Include(f => f.Lignes)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Facture?> GetFactureByNumeroAsync(string numero)
    {
        await using var context = new AppDbContext();
        return await context.Factures
            .Include(f => f.Lignes)
            .FirstOrDefaultAsync(f => f.NumeroFacture == numero);
    }

    public async Task SaveFactureAsync(Facture facture)
    {
        await using var context = new AppDbContext();
        
        if (facture.Id == 0)
        {
            facture.DateCreation = DateTime.Now;
            context.Factures.Add(facture);
        }
        else
        {
            facture.DateModification = DateTime.Now;
            var existing = await context.Factures
                .Include(f => f.Lignes)
                .FirstOrDefaultAsync(f => f.Id == facture.Id);
            
            if (existing != null)
            {
                context.LignesFacture.RemoveRange(existing.Lignes);
                context.Entry(existing).CurrentValues.SetValues(facture);
                foreach (var ligne in facture.Lignes)
                {
                    ligne.FactureId = existing.Id;
                    context.LignesFacture.Add(ligne);
                }
            }
        }
        
        await context.SaveChangesAsync();
    }

    public async Task DeleteFactureAsync(int id)
    {
        await using var context = new AppDbContext();
        var facture = await context.Factures.FindAsync(id);
        if (facture != null)
        {
            context.Factures.Remove(facture);
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateStatutFactureAsync(int id, StatutFacture nouveauStatut)
    {
        await using var context = new AppDbContext();
        var facture = await context.Factures.FindAsync(id);
        if (facture != null)
        {
            facture.Statut = nouveauStatut;
            facture.DateModification = DateTime.Now;
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<Facture>> GetFacturesAsync(DateTime? dateDebut, DateTime? dateFin, TypeFacture? type, StatutFacture? statut, string? recherche)
    {
        await using var context = new AppDbContext();
        var query = context.Factures.Include(f => f.Lignes).AsQueryable();

        if (dateDebut.HasValue)
            query = query.Where(f => f.DateFacture >= dateDebut.Value);

        if (dateFin.HasValue)
            query = query.Where(f => f.DateFacture <= dateFin.Value);

        if (type.HasValue)
            query = query.Where(f => f.TypeFacture == type.Value);

        if (statut.HasValue)
            query = query.Where(f => f.Statut == statut.Value);

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var searchLower = recherche.ToLower();
            query = query.Where(f => 
                f.NumeroFacture.ToLower().Contains(searchLower) ||
                f.ClientNom.ToLower().Contains(searchLower));
        }

        return await query.OrderByDescending(f => f.DateCreation).ToListAsync();
    }

    public async Task<Facture> DupliquerFactureAsync(int id)
    {
        await using var context = new AppDbContext();
        var original = await context.Factures
            .Include(f => f.Lignes)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (original == null)
            throw new InvalidOperationException($"Facture avec Id {id} non trouvée");

        var copie = new Facture
        {
            BusinessId = original.BusinessId,
            DateFacture = DateTime.Today,
            DateEcheance = DateTime.Today.AddDays(30),
            TypeFacture = original.TypeFacture,
            ModePaiement = original.ModePaiement,
            ClientBusinessType = original.ClientBusinessType,
            ClientNom = original.ClientNom,
            ClientAdresse = original.ClientAdresse,
            ClientTelephone = original.ClientTelephone,
            ClientEmail = original.ClientEmail,
            ClientFax = original.ClientFax,
            ClientRC = original.ClientRC,
            ClientNIS = original.ClientNIS,
            ClientNIF = original.ClientNIF,
            ClientAI = original.ClientAI,
            ClientNumeroImmatriculation = original.ClientNumeroImmatriculation,
            ClientActivite = original.ClientActivite,
            ClientCapitalSocial = original.ClientCapitalSocial,
            TotalHT = original.TotalHT,
            TotalTVA19 = original.TotalTVA19,
            TotalTVA9 = original.TotalTVA9,
            TotalTTC = original.TotalTTC,
            TimbreFiscal = original.TimbreFiscal,
            EstTimbreApplique = original.EstTimbreApplique,
            MontantTotal = original.MontantTotal,
            MontantEnLettres = original.MontantEnLettres,
            Statut = StatutFacture.EnAttente
        };

        foreach (var ligne in original.Lignes)
        {
            copie.Lignes.Add(new LigneFacture
            {
                NumeroLigne = ligne.NumeroLigne,
                Designation = ligne.Designation,
                Quantite = ligne.Quantite,
                PrixUnitaire = ligne.PrixUnitaire,
                TauxTVA = ligne.TauxTVA,
                TotalHT = ligne.TotalHT
            });
        }

        return copie;
    }

    // Statistiques
    public async Task<int> GetNombreFacturesAsync()
    {
        await using var context = new AppDbContext();
        return await context.Factures.CountAsync();
    }

    public async Task<decimal> GetChiffreAffairesTotalAsync()
    {
        await using var context = new AppDbContext();
        var total = await context.Factures
            .Where(f => f.Statut != StatutFacture.Annulee)
            .SumAsync(f => (decimal?)f.MontantTotal) ?? 0m;
        return total;
    }

    public async Task<decimal> GetMontantMoyenAsync()
    {
        await using var context = new AppDbContext();
        var moyenne = await context.Factures
            .Where(f => f.Statut != StatutFacture.Annulee)
            .AverageAsync(f => (decimal?)f.MontantTotal) ?? 0m;
        return moyenne;
    }

    public async Task<List<Facture>> GetDernieresFacturesAsync(int nombre)
    {
        await using var context = new AppDbContext();
        return await context.Factures
            .Include(f => f.Lignes)
            .OrderByDescending(f => f.DateCreation)
            .Take(nombre)
            .ToListAsync();
    }

    // Configuration
    public async Task<string?> GetConfigurationAsync(string cle)
    {
        await using var context = new AppDbContext();
        var config = await context.Configurations.FindAsync(cle);
        return config?.Valeur;
    }

    public async Task SetConfigurationAsync(string cle, string valeur)
    {
        await using var context = new AppDbContext();
        var config = await context.Configurations.FindAsync(cle);
        
        if (config != null)
        {
            config.Valeur = valeur;
        }
        else
        {
            context.Configurations.Add(new Configuration { Cle = cle, Valeur = valeur });
        }
        
        await context.SaveChangesAsync();
    }

    public string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "FatouraDZ", "fatouradz.db");
    }
}
