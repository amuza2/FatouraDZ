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
    // Version du schéma applicatif. À incrémenter à chaque nouvelle migration.
    private const int VersionSchemaActuelle = 1;
    private const string CleVersionSchema = "schema_version";

    public async Task InitializeDatabaseAsync()
    {
        var cheminBase = AppSettings.Instance.DatabasePath;
        var fichierExistait = File.Exists(cheminBase);

        await using var context = new AppDbContext();
        var baseCreee = await context.Database.EnsureCreatedAsync();

        if (baseCreee)
        {
            // Base neuve : le schéma correspond déjà à la version courante.
            await EcrireVersionSchemaAsync(context, VersionSchemaActuelle);
            return;
        }

        var versionActuelle = await LireVersionSchemaAsync(context);
        if (versionActuelle >= VersionSchemaActuelle)
            return;

        // Base existante à mettre à niveau : sauvegarde de sécurité obligatoire avant altération.
        if (fichierExistait)
            SauvegarderAvantMigration(cheminBase);

        // Migrations idempotentes, appliquées dans l'ordre.
        await MigrateColumnsAsync(context);
        await MigrateClientTableAsync(context);
        await MigrateTransactionTablesAsync(context);

        await EcrireVersionSchemaAsync(context, VersionSchemaActuelle);
    }

    private static async Task<int> LireVersionSchemaAsync(AppDbContext context)
    {
        var config = await context.Configurations.FindAsync(CleVersionSchema);
        return config != null && int.TryParse(config.Valeur, out var version) ? version : 0;
    }

    private static async Task EcrireVersionSchemaAsync(AppDbContext context, int version)
    {
        var config = await context.Configurations.FindAsync(CleVersionSchema);
        if (config != null)
        {
            config.Valeur = version.ToString();
        }
        else
        {
            context.Configurations.Add(new Configuration { Cle = CleVersionSchema, Valeur = version.ToString() });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Copie la base de données avant une migration de schéma afin de pouvoir restaurer
    /// les données de l'utilisateur en cas de problème.
    /// </summary>
    private static void SauvegarderAvantMigration(string cheminBase)
    {
        try
        {
            if (!File.Exists(cheminBase))
                return;

            var cheminSauvegarde = $"{cheminBase}.backup-{DateTime.Now:yyyyMMdd_HHmmss}";
            File.Copy(cheminBase, cheminSauvegarde, overwrite: true);
            ServiceLocator.Logger.Info($"Sauvegarde de la base avant migration : {cheminSauvegarde}");
        }
        catch (Exception ex)
        {
            // Une sauvegarde impossible ne doit pas empêcher le démarrage, mais doit être tracée.
            ServiceLocator.Logger.Warning("Impossible de créer la sauvegarde avant migration", ex);
        }
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

    private async Task MigrateTransactionTablesAsync(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();
        
        try
        {
            // Create Transactions table if not exists
            using var cmd1 = connection.CreateCommand();
            cmd1.CommandText = @"
                CREATE TABLE IF NOT EXISTS Transactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BusinessId INTEGER NOT NULL,
                    Date TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Montant REAL NOT NULL DEFAULT 0,
                    Type INTEGER NOT NULL DEFAULT 0,
                    Categorie TEXT NOT NULL DEFAULT '',
                    FactureId INTEGER,
                    NumeroFacture TEXT,
                    DateCreation TEXT NOT NULL,
                    DateModification TEXT,
                    FOREIGN KEY (BusinessId) REFERENCES Businesses(Id) ON DELETE CASCADE
                )";
            await cmd1.ExecuteNonQueryAsync();

            // Create CategoriesTransaction table if not exists
            using var cmd2 = connection.CreateCommand();
            cmd2.CommandText = @"
                CREATE TABLE IF NOT EXISTS CategoriesTransaction (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BusinessId INTEGER NOT NULL,
                    Nom TEXT NOT NULL,
                    Type INTEGER NOT NULL DEFAULT 0,
                    DateCreation TEXT NOT NULL,
                    FOREIGN KEY (BusinessId) REFERENCES Businesses(Id) ON DELETE CASCADE
                )";
            await cmd2.ExecuteNonQueryAsync();

            // Add IsArchived column if missing
            await AddColumnIfNotExistsAsync(connection, "Transactions", "IsArchived", "INTEGER DEFAULT 0");
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
            var existing = await context.Businesses.FindAsync(business.Id);
            if (existing != null)
            {
                // Conserver les champs gérés par l'application et non par le formulaire :
                // sinon une simple édition désarchiverait l'entreprise et réinitialiserait sa date de création.
                var isArchived = existing.IsArchived;
                var dateCreation = existing.DateCreation;

                context.Entry(existing).CurrentValues.SetValues(business);

                existing.IsArchived = isArchived;
                existing.DateCreation = dateCreation;
                existing.DateModification = DateTime.Now;
            }
        }
        
        await context.SaveChangesAsync();
    }

    public async Task ArchiveBusinessAsync(int id)
    {
        await using var context = new AppDbContext();
        var business = await context.Businesses.FindAsync(id);
        if (business != null)
        {
            business.IsArchived = !business.IsArchived;
            business.DateModification = DateTime.Now;
            await context.SaveChangesAsync();
        }
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

    public async Task<List<Facture>> GetFacturesFiltreesAsync(int businessId, int annee, bool archived, TypeFacture? type, StatutFacture? statut, string? recherche)
    {
        await using var context = new AppDbContext();

        var debutAnnee = new DateTime(annee, 1, 1);
        var finAnnee = debutAnnee.AddYears(1);

        // Filtrage effectué côté SQLite : on ne rapatrie que les factures utiles (sans les lignes).
        var query = context.Factures
            .Where(f => f.BusinessId == businessId
                     && f.DateFacture >= debutAnnee
                     && f.DateFacture < finAnnee
                     && f.IsArchived == archived);

        if (type.HasValue)
            query = query.Where(f => f.TypeFacture == type.Value);

        if (statut.HasValue)
            query = query.Where(f => f.Statut == statut.Value);

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var search = recherche.ToLower();
            query = query.Where(f => f.NumeroFacture.ToLower().Contains(search)
                                  || f.ClientNom.ToLower().Contains(search));
        }

        return await query
            .OrderByDescending(f => f.DateFacture)
            .ThenByDescending(f => f.Id)
            .ToListAsync();
    }

    public async Task<List<int>> GetAnneesFacturesAsync(int businessId)
    {
        await using var context = new AppDbContext();
        return await context.Factures
            .Where(f => f.BusinessId == businessId)
            .Select(f => f.DateFacture.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();
    }

    public async Task<StatistiquesFactures> GetStatistiquesFacturesAsync(int businessId, int annee)
    {
        await using var context = new AppDbContext();

        var debutAnnee = new DateTime(annee, 1, 1);
        var finAnnee = debutAnnee.AddYears(1);

        var deLAnnee = context.Factures
            .Where(f => f.BusinessId == businessId
                     && f.DateFacture >= debutAnnee
                     && f.DateFacture < finAnnee);

        return new StatistiquesFactures
        {
            NombreFactures = await deLAnnee.CountAsync(),
            ChiffreAffaires = await deLAnnee
                .Where(f => f.Statut != StatutFacture.Annulee && !f.IsArchived)
                .SumAsync(f => (decimal?)f.MontantTotal) ?? 0m,
            FacturesEnAttente = await deLAnnee.CountAsync(f => f.Statut == StatutFacture.EnAttente && !f.IsArchived),
            FacturesPayees = await deLAnnee.CountAsync(f => f.Statut == StatutFacture.Payee && !f.IsArchived)
        };
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

    public async Task UpdateCheminPdfAsync(int id, string cheminPdf)
    {
        await using var context = new AppDbContext();
        var facture = await context.Factures.FindAsync(id);
        if (facture != null)
        {
            facture.CheminPDF = cheminPdf;
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
            DateEcheance = DateTime.Today.AddDays(AppSettings.Instance.DelaiPaiementDefaut),
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
            var existing = await context.Clients.FindAsync(client.Id);
            if (existing != null)
            {
                // Ne pas écraser la date de création d'origine avec la valeur par défaut du modèle.
                var dateCreation = existing.DateCreation;

                context.Entry(existing).CurrentValues.SetValues(client);

                existing.DateCreation = dateCreation;
                existing.DateModification = DateTime.Now;
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

    // Transactions
    public async Task<List<Transaction>> GetTransactionsByBusinessIdAsync(int businessId)
    {
        await using var context = new AppDbContext();
        return await context.Transactions
            .Where(t => t.BusinessId == businessId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.DateCreation)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetTransactionsFiltreesAsync(int businessId, bool afficherArchivees, DateTime debut, DateTime fin, TypeTransaction? type, string? categorie, int skip, int take)
    {
        await using var context = new AppDbContext();
        return await ConstruireRequeteTransactions(context, businessId, afficherArchivees, debut, fin, type, categorie)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetNombreTransactionsFiltreesAsync(int businessId, bool afficherArchivees, DateTime debut, DateTime fin, TypeTransaction? type, string? categorie)
    {
        await using var context = new AppDbContext();
        return await ConstruireRequeteTransactions(context, businessId, afficherArchivees, debut, fin, type, categorie)
            .CountAsync();
    }

    public async Task<(decimal Recettes, decimal Depenses)> GetTotauxTransactionsAsync(int businessId, DateTime debut, DateTime fin)
    {
        await using var context = new AppDbContext();

        // Borne de fin exclusive pour inclure toute la journée de fin.
        var debutInclus = debut.Date;
        var finExclue = fin.Date.AddDays(1);

        var requete = context.Transactions
            .Where(t => t.BusinessId == businessId
                     && !t.IsArchived
                     && t.Date >= debutInclus
                     && t.Date < finExclue);

        var recettes = await requete
            .Where(t => t.Type == TypeTransaction.Recette)
            .SumAsync(t => (decimal?)t.Montant) ?? 0m;

        var depenses = await requete
            .Where(t => t.Type == TypeTransaction.Depense)
            .SumAsync(t => (decimal?)t.Montant) ?? 0m;

        return (recettes, depenses);
    }

    private static IQueryable<Transaction> ConstruireRequeteTransactions(AppDbContext context, int businessId, bool afficherArchivees, DateTime debut, DateTime fin, TypeTransaction? type, string? categorie)
    {
        // Borne de fin exclusive pour inclure toute la journée de fin.
        var debutInclus = debut.Date;
        var finExclue = fin.Date.AddDays(1);

        var query = context.Transactions.Where(t =>
            t.BusinessId == businessId
            && t.IsArchived == afficherArchivees
            && t.Date >= debutInclus
            && t.Date < finExclue);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(categorie) && categorie != "Toutes")
            query = query.Where(t => t.Categorie == categorie);

        return query;
    }

    public async Task SaveTransactionAsync(Transaction transaction)
    {
        await using var context = new AppDbContext();
        if (transaction.Id == 0)
        {
            transaction.DateCreation = DateTime.Now;
            context.Transactions.Add(transaction);
        }
        else
        {
            transaction.DateModification = DateTime.Now;
            var existing = await context.Transactions.FindAsync(transaction.Id);
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(transaction);
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task DeleteTransactionAsync(int id)
    {
        await using var context = new AppDbContext();
        var transaction = await context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            context.Transactions.Remove(transaction);
            await context.SaveChangesAsync();
        }
    }

    public async Task ArchiveTransactionAsync(int id)
    {
        await using var context = new AppDbContext();
        var transaction = await context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            transaction.IsArchived = !transaction.IsArchived;
            transaction.DateModification = DateTime.Now;
            await context.SaveChangesAsync();
        }
    }

    public async Task<Transaction?> GetTransactionByFactureIdAsync(int factureId)
    {
        await using var context = new AppDbContext();
        return await context.Transactions
            .FirstOrDefaultAsync(t => t.FactureId == factureId);
    }

    // Catégories de transactions
    public async Task<List<CategorieTransaction>> GetCategoriesByBusinessIdAsync(int businessId)
    {
        await using var context = new AppDbContext();
        return await context.CategoriesTransaction
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.Nom)
            .ToListAsync();
    }

    public async Task SaveCategorieAsync(CategorieTransaction categorie)
    {
        await using var context = new AppDbContext();
        if (categorie.Id == 0)
        {
            categorie.DateCreation = DateTime.Now;
            context.CategoriesTransaction.Add(categorie);
        }
        else
        {
            var existing = await context.CategoriesTransaction.FindAsync(categorie.Id);
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(categorie);
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task DeleteCategorieAsync(int id)
    {
        await using var context = new AppDbContext();
        var categorie = await context.CategoriesTransaction.FindAsync(id);
        if (categorie != null)
        {
            context.CategoriesTransaction.Remove(categorie);
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

    // Numérotation des factures : lecture et réservation atomique du compteur annuel.
    private const string CleDerniereAnnee = "derniere_annee_facture";
    private const string CleProchainNumero = "prochain_numero";

    public async Task<int> LireProchainNumeroFactureAsync(int annee)
    {
        await using var context = new AppDbContext();
        var anneeStr = annee.ToString();

        var derniereAnnee = await context.Configurations.FindAsync(CleDerniereAnnee);
        if (derniereAnnee?.Valeur != anneeStr)
            return 1; // Nouvelle année (ou première facture) : on repart à 1

        var compteur = await context.Configurations.FindAsync(CleProchainNumero);
        return int.TryParse(compteur?.Valeur, out var numero) && numero > 0 ? numero : 1;
    }

    public async Task<int> ReserverProchainNumeroFactureAsync(int annee)
    {
        await using var context = new AppDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var anneeStr = annee.ToString();
        var derniereAnnee = await context.Configurations.FindAsync(CleDerniereAnnee);
        var compteur = await context.Configurations.FindAsync(CleProchainNumero);

        int numero;
        if (derniereAnnee?.Valeur != anneeStr)
        {
            // Changement d'année : réinitialisation du compteur (001) + persistance de l'année.
            numero = 1;
            UpsertConfiguration(context, CleDerniereAnnee, anneeStr);
        }
        else
        {
            numero = int.TryParse(compteur?.Valeur, out var n) && n > 0 ? n : 1;
        }

        // Le compteur pointe désormais vers le numéro suivant, persisté dans la même transaction.
        UpsertConfiguration(context, CleProchainNumero, (numero + 1).ToString());

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return numero;
    }

    private static void UpsertConfiguration(AppDbContext context, string cle, string valeur)
    {
        var config = context.Configurations.Find(cle);
        if (config != null)
        {
            config.Valeur = valeur;
        }
        else
        {
            context.Configurations.Add(new Configuration { Cle = cle, Valeur = valeur });
        }
    }

    public string GetDatabasePath()
    {
        return AppSettings.Instance.DatabasePath;
    }
}
