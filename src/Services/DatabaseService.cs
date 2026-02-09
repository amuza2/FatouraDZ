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
        
        // Run migrations
        await MigrateColumnsAsync(context);
        await MigrateClientTableAsync(context);
    }

    private async Task MigrateColumnsAsync(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        
        try
        {
            // LignesFacture columns
            await AddColumnIfNotExistsAsync(connection, "LignesFacture", "Reference", "TEXT");
            await AddColumnIfNotExistsAsync(connection, "LignesFacture", "Unite", "INTEGER DEFAULT 0");
            await AddColumnIfNotExistsAsync(connection, "LignesFacture", "Remise", "REAL DEFAULT 0");
            await AddColumnIfNotExistsAsync(connection, "LignesFacture", "TypeRemise", "INTEGER DEFAULT 0");
            await AddColumnIfNotExistsAsync(connection, "LignesFacture", "MontantRemise", "REAL DEFAULT 0");
            
            // Factures - discount columns
            await AddColumnIfNotExistsAsync(connection, "Factures", "RemiseGlobale", "REAL DEFAULT 0");
            await AddColumnIfNotExistsAsync(connection, "Factures", "TypeRemiseGlobale", "INTEGER DEFAULT 0");
            await AddColumnIfNotExistsAsync(connection, "Factures", "MontantRemiseGlobale", "REAL DEFAULT 0");
            
            // Factures - payment details
            await AddColumnIfNotExistsAsync(connection, "Factures", "PaiementReference", "TEXT");
            await AddColumnIfNotExistsAsync(connection, "Factures", "PaiementValeur", "REAL DEFAULT 0");
            await AddColumnIfNotExistsAsync(connection, "Factures", "PaiementNumeroPiece", "TEXT");
            
            // Factures - client extended fields
            await AddColumnIfNotExistsAsync(connection, "Factures", "ClientBusinessType", "INTEGER DEFAULT 0");
            await AddColumnIfNotExistsAsync(connection, "Factures", "ClientFormeJuridique", "TEXT");
            await AddColumnIfNotExistsAsync(connection, "Factures", "ClientFax", "TEXT");
            await AddColumnIfNotExistsAsync(connection, "Factures", "ClientCapitalSocial", "TEXT");
            await AddColumnIfNotExistsAsync(connection, "Factures", "ClientNumeroImmatriculation", "TEXT");
            await AddColumnIfNotExistsAsync(connection, "Factures", "ClientActivite", "TEXT");
            
            // Factures - avoir, retenue, proforma, archive
            await AddColumnIfNotExistsAsync(connection, "Factures", "NumeroFactureOrigine", "TEXT");
            await AddColumnIfNotExistsAsync(connection, "Factures", "TauxRetenueSource", "REAL");
            await AddColumnIfNotExistsAsync(connection, "Factures", "RetenueSource", "REAL DEFAULT 0");
            await AddColumnIfNotExistsAsync(connection, "Factures", "DateValidite", "TEXT");
            await AddColumnIfNotExistsAsync(connection, "Factures", "IsArchived", "INTEGER DEFAULT 0");
            
            // Businesses - archive
            await AddColumnIfNotExistsAsync(connection, "Businesses", "IsArchived", "INTEGER DEFAULT 0");
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private async Task AddColumnIfNotExistsAsync(System.Data.Common.DbConnection connection, string tableName, string columnName, string columnType)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName})";
        
        bool columnExists = false;
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    columnExists = true;
                    break;
                }
            }
        }
        
        if (!columnExists)
        {
            using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";
            await alterCommand.ExecuteNonQueryAsync();
        }
    }

    private async Task MigrateClientTableAsync(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();
        
        try
        {
            // Check if Clients table exists
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Clients'";
            var result = await command.ExecuteScalarAsync();
            
            if (result == null)
            {
                // Create Clients table
                using var createCommand = connection.CreateCommand();
                createCommand.CommandText = @"
                    CREATE TABLE Clients (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        BusinessId INTEGER NOT NULL,
                        TypeClient INTEGER DEFAULT 0,
                        Nom TEXT NOT NULL,
                        Adresse TEXT NOT NULL,
                        Telephone TEXT NOT NULL,
                        Email TEXT,
                        Fax TEXT,
                        FormeJuridique TEXT,
                        RC TEXT,
                        NIS TEXT,
                        NIF TEXT,
                        AI TEXT,
                        NumeroImmatriculation TEXT,
                        Activite TEXT,
                        CapitalSocial TEXT,
                        DateCreation TEXT NOT NULL,
                        DateModification TEXT,
                        FOREIGN KEY (BusinessId) REFERENCES Businesses(Id) ON DELETE CASCADE
                    )";
                await createCommand.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
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
            RemiseGlobale = original.RemiseGlobale,
            TypeRemiseGlobale = original.TypeRemiseGlobale,
            MontantRemiseGlobale = original.MontantRemiseGlobale,
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
                Reference = ligne.Reference,
                Designation = ligne.Designation,
                Quantite = ligne.Quantite,
                Unite = ligne.Unite,
                PrixUnitaire = ligne.PrixUnitaire,
                TauxTVA = ligne.TauxTVA,
                Remise = ligne.Remise,
                TypeRemise = ligne.TypeRemise,
                MontantRemise = ligne.MontantRemise,
                TotalHT = ligne.TotalHT
            });
        }

        return copie;
    }

    // Clients
    public async Task<List<Client>> GetClientsByBusinessIdAsync(int businessId)
    {
        await using var context = new AppDbContext();
        return await context.Clients
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.Nom)
            .ToListAsync();
    }

    public async Task<Client?> GetClientByIdAsync(int id)
    {
        await using var context = new AppDbContext();
        return await context.Clients.FindAsync(id);
    }

    public async Task SaveClientAsync(Client client)
    {
        await using var context = new AppDbContext();
        
        if (client.Id == 0)
        {
            client.DateCreation = DateTime.Now;
            context.Clients.Add(client);
        }
        else
        {
            client.DateModification = DateTime.Now;
            var existing = await context.Clients.FindAsync(client.Id);
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(client);
            }
        }
        
        await context.SaveChangesAsync();
    }

    public async Task DeleteClientAsync(int id)
    {
        await using var context = new AppDbContext();
        var client = await context.Clients.FindAsync(id);
        if (client != null)
        {
            context.Clients.Remove(client);
            await context.SaveChangesAsync();
        }
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
        return AppSettings.Instance.DatabasePath;
    }
}
