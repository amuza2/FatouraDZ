using System;
using System.Collections.Generic;
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

    // Entrepreneur
    public async Task<Entrepreneur?> GetEntrepreneurAsync()
    {
        await using var context = new AppDbContext();
        return await context.Entrepreneurs.FirstOrDefaultAsync();
    }

    public async Task SaveEntrepreneurAsync(Entrepreneur entrepreneur)
    {
        await using var context = new AppDbContext();
        var existing = await context.Entrepreneurs.FirstOrDefaultAsync();
        
        if (existing != null)
        {
            entrepreneur.Id = existing.Id;
            entrepreneur.DateModification = DateTime.Now;
            context.Entry(existing).CurrentValues.SetValues(entrepreneur);
        }
        else
        {
            entrepreneur.DateCreation = DateTime.Now;
            context.Entrepreneurs.Add(entrepreneur);
        }
        
        await context.SaveChangesAsync();
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
}
