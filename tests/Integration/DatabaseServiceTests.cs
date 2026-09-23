using FatouraDZ.Database;
using FatouraDZ.Models;
using FatouraDZ.Services;
using Microsoft.EntityFrameworkCore;

namespace FatouraDZ.Tests.Integration;

public class DatabaseServiceTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _originalDbPath;
    private readonly DatabaseService _service;

    public DatabaseServiceTests()
    {
        // Base de données temporaire et isolée : les tests ne doivent JAMAIS toucher
        // la base de données réelle de l'utilisateur.
        _testDbPath = Path.Combine(Path.GetTempPath(), $"fatouradz_test_{Guid.NewGuid()}.db");
        _originalDbPath = AppSettings.Instance.DatabasePath;
        AppSettings.Instance.DatabasePath = _testDbPath;
        _service = new DatabaseService();
    }

    public void Dispose()
    {
        // Restaurer le chemin d'origine et nettoyer la base de test.
        AppSettings.Instance.DatabasePath = _originalDbPath;
        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); } catch { }
        }
    }

    #region Business Tests

    [Fact]
    public async Task SaveBusinessAsync_NewBusiness_SavesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();

        // Act
        await _service.SaveBusinessAsync(business);
        var result = await _service.GetBusinessByIdAsync(business.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(business.Nom, result.Nom);
        Assert.Equal(business.Email, result.Email);
    }

    [Fact]
    public async Task SaveBusinessAsync_UpdateExisting_UpdatesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        // Act
        business.Nom = "Updated Name";
        business.Email = "updated@example.com";
        await _service.SaveBusinessAsync(business);
        var result = await _service.GetBusinessByIdAsync(business.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Nom);
        Assert.Equal("updated@example.com", result.Email);
    }

    [Fact]
    public async Task GetAllBusinessesAsync_ReturnsAllBusinesses()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var initialCount = (await _service.GetBusinessesAsync()).Count;
        await _service.SaveBusinessAsync(CreateTestBusiness("Business 1"));
        await _service.SaveBusinessAsync(CreateTestBusiness("Business 2"));

        // Act
        var result = await _service.GetBusinessesAsync();

        // Assert
        Assert.Equal(initialCount + 2, result.Count);
    }

    #endregion

    #region Facture Tests

    [Fact]
    public async Task SaveFactureAsync_NewFacture_SavesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        var facture = CreateTestFacture("FAC-2025-001", business.Id);

        // Act
        await _service.SaveFactureAsync(facture);
        var result = await _service.GetFactureByIdAsync(facture.Id);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("FAC-2025-001", result.NumeroFacture);
        Assert.Equal("Client Test", result.ClientNom);
        Assert.NotEmpty(result.Lignes);
    }

    [Fact]
    public async Task SaveFactureAsync_WithLines_SavesLinesCorrectly()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        var facture = CreateTestFacture("FAC-2025-002", business.Id);
        facture.Lignes.Add(new LigneFacture
        {
            NumeroLigne = 2,
            Designation = "Second Service",
            Quantite = 2,
            PrixUnitaire = 500,
            TauxTVA = TauxTVA.TVA9,
            TotalHT = 1000
        });

        // Act
        await _service.SaveFactureAsync(facture);
        var result = await _service.GetFactureByIdAsync(facture.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Lignes.Count);
    }

    [Fact]
    public async Task GetFacturesAsync_ReturnsAllFactures()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        await _service.SaveFactureAsync(CreateTestFacture("FAC-2025-001", business.Id));
        await _service.SaveFactureAsync(CreateTestFacture("FAC-2025-002", business.Id));
        await _service.SaveFactureAsync(CreateTestFacture("FAC-2025-003", business.Id));

        // Act
        var result = await _service.GetFacturesByBusinessIdAsync(business.Id);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetFacturesAsync_WithFilters_FiltersCorrectly()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        
        var facture1 = CreateTestFacture("FAC-2025-001", business.Id);
        facture1.TypeFacture = TypeFacture.Normale;
        facture1.Statut = StatutFacture.Payee;
        
        var facture2 = CreateTestFacture("FAC-2025-002", business.Id);
        facture2.TypeFacture = TypeFacture.Avoir;
        facture2.Statut = StatutFacture.EnAttente;
        
        await _service.SaveFactureAsync(facture1);
        await _service.SaveFactureAsync(facture2);

        // Act - Get factures for this business and verify types
        var allFactures = await _service.GetFacturesByBusinessIdAsync(business.Id);
        var normales = allFactures.Where(f => f.TypeFacture == TypeFacture.Normale).ToList();
        var avoirs = allFactures.Where(f => f.TypeFacture == TypeFacture.Avoir).ToList();

        // Assert
        Assert.Single(normales);
        Assert.Single(avoirs);
        Assert.StartsWith("FAC-2025-001", normales[0].NumeroFacture);
        Assert.StartsWith("FAC-2025-002", avoirs[0].NumeroFacture);
    }

    [Fact]
    public async Task GetFacturesAsync_WithSearch_SearchesCorrectly()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        
        // Use unique client names to avoid conflicts with other tests
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var facture1 = CreateTestFacture("FAC-2025-001", business.Id);
        facture1.ClientNom = $"UniqueClient_{uniqueId}_Ahmed";
        
        var facture2 = CreateTestFacture("FAC-2025-002", business.Id);
        facture2.ClientNom = $"UniqueClient_{uniqueId}_Mohamed";
        
        await _service.SaveFactureAsync(facture1);
        await _service.SaveFactureAsync(facture2);

        // Act - Search for the unique Ahmed client
        var result = await _service.GetFacturesAsync(null, null, null, null, $"UniqueClient_{uniqueId}_Ahmed");

        // Assert
        Assert.Single(result);
        Assert.Contains("Ahmed", result[0].ClientNom);
    }

    [Fact]
    public async Task DeleteFactureAsync_DeletesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        var facture = CreateTestFacture("FAC-2025-001", business.Id);
        await _service.SaveFactureAsync(facture);
        var factureId = facture.Id;

        // Act
        await _service.DeleteFactureAsync(factureId);
        var result = await _service.GetFactureByIdAsync(factureId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateStatutFactureAsync_UpdatesStatus()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        var facture = CreateTestFacture("FAC-2025-001", business.Id);
        facture.Statut = StatutFacture.EnAttente;
        await _service.SaveFactureAsync(facture);

        // Act
        await _service.UpdateStatutFactureAsync(facture.Id, StatutFacture.Payee);
        var result = await _service.GetFactureByIdAsync(facture.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatutFacture.Payee, result.Statut);
    }

    [Fact]
    public async Task DupliquerFactureAsync_CreatesCorrectCopy()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        var original = CreateTestFacture("FAC-2025-001", business.Id);
        original.ClientNom = "Original Client";
        await _service.SaveFactureAsync(original);

        // Act
        var copie = await _service.DupliquerFactureAsync(original.Id);

        // Assert
        Assert.Equal("Original Client", copie.ClientNom);
        Assert.Equal(StatutFacture.EnAttente, copie.Statut);
        Assert.Equal(0, copie.Id); // New invoice, not saved yet
        Assert.NotEmpty(copie.Lignes);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetNombreFacturesAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var initialCount = await _service.GetNombreFacturesAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        await _service.SaveFactureAsync(CreateTestFacture("FAC-2025-001", business.Id));
        await _service.SaveFactureAsync(CreateTestFacture("FAC-2025-002", business.Id));

        // Act
        var count = await _service.GetNombreFacturesAsync();

        // Assert
        Assert.Equal(initialCount + 2, count);
    }

    [Fact]
    public async Task GetChiffreAffairesTotalAsync_ExcludesAnnulees()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var initialTotal = await _service.GetChiffreAffairesTotalAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        
        var facture1 = CreateTestFacture("FAC-2025-001", business.Id);
        facture1.MontantTotal = 1000;
        facture1.Statut = StatutFacture.Payee;
        
        var facture2 = CreateTestFacture("FAC-2025-002", business.Id);
        facture2.MontantTotal = 2000;
        facture2.Statut = StatutFacture.Annulee; // Should be excluded
        
        await _service.SaveFactureAsync(facture1);
        await _service.SaveFactureAsync(facture2);

        // Act
        var total = await _service.GetChiffreAffairesTotalAsync();

        // Assert
        Assert.Equal(initialTotal + 1000, total);
    }

    [Fact]
    public async Task GetDernieresFacturesAsync_ReturnsLimitedResults()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);
        for (int i = 1; i <= 10; i++)
        {
            await _service.SaveFactureAsync(CreateTestFacture($"FAC-2025-{i:D3}", business.Id));
        }

        // Act
        var result = await _service.GetDernieresFacturesAsync(5);

        // Assert
        Assert.Equal(5, result.Count);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task SetConfigurationAsync_NewKey_SavesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();

        // Act
        await _service.SetConfigurationAsync("test_key", "test_value");
        var result = await _service.GetConfigurationAsync("test_key");

        // Assert
        Assert.Equal("test_value", result);
    }

    [Fact]
    public async Task SetConfigurationAsync_ExistingKey_UpdatesValue()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        await _service.SetConfigurationAsync("test_key", "original_value");

        // Act
        await _service.SetConfigurationAsync("test_key", "updated_value");
        var result = await _service.GetConfigurationAsync("test_key");

        // Assert
        Assert.Equal("updated_value", result);
    }

    [Fact]
    public async Task GetConfigurationAsync_NonExistentKey_ReturnsNull()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();

        // Act
        var result = await _service.GetConfigurationAsync("non_existent_key");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Helper Methods

    private static Business CreateTestBusiness(string nom = "Test Business")
    {
        return new Business
        {
            Nom = nom,
            NomComplet = "Test Owner",
            TypeEntreprise = BusinessType.AutoEntrepreneur,
            Adresse = "123 Test Street",
            Ville = "Alger",
            Wilaya = "Alger",
            CodePostal = "16000",
            Telephone = "0550123456",
            Email = "test@example.com",
            RC = "RC123456",
            NIS = "123456789012345",
            NIF = "NIF123456",
            AI = "AI123456",
            NumeroImmatriculation = "IMM123456"
        };
    }

    private static Facture CreateTestFacture(string numero, int businessId)
    {
        // Add unique suffix to avoid conflicts with other test runs
        var uniqueNumero = $"{numero}-{Guid.NewGuid().ToString()[..8]}";
        return new Facture
        {
            NumeroFacture = uniqueNumero,
            DateFacture = DateTime.Today,
            DateEcheance = DateTime.Today.AddDays(30),
            TypeFacture = TypeFacture.Normale,
            ModePaiement = "Espèces",
            ClientNom = "Client Test",
            ClientAdresse = "456 Client Street",
            ClientTelephone = "0660123456",
            TotalHT = 1000,
            TotalTVA19 = 190,
            TotalTVA9 = 0,
            TotalTTC = 1190,
            TimbreFiscal = 0,
            EstTimbreApplique = false,
            MontantTotal = 1190,
            MontantEnLettres = "Mille cent quatre-vingt-dix dinars algériens",
            Statut = StatutFacture.EnAttente,
            BusinessId = businessId,
            Lignes = new List<LigneFacture>
            {
                new()
                {
                    NumeroLigne = 1,
                    Designation = "Test Service",
                    Quantite = 1,
                    PrixUnitaire = 1000,
                    TauxTVA = TauxTVA.TVA19,
                    TotalHT = 1000
                }
            }
        };
    }

    #endregion

    #region Numérotation des factures (réservation atomique)

    [Fact]
    public async Task LireProchainNumeroFactureAsync_DoesNotConsumeNumber()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var annee = DateTime.Now.Year;

        // Act
        var premier = await _service.LireProchainNumeroFactureAsync(annee);
        var second = await _service.LireProchainNumeroFactureAsync(annee);

        // Assert : la lecture ne consomme jamais de numéro
        Assert.Equal(1, premier);
        Assert.Equal(1, second);

        var reserve = await _service.ReserverProchainNumeroFactureAsync(annee);
        Assert.Equal(1, reserve);
    }

    [Fact]
    public async Task ReserverProchainNumeroFactureAsync_ReturnsSequentialNumbers()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var annee = DateTime.Now.Year;

        // Act
        var premier = await _service.ReserverProchainNumeroFactureAsync(annee);
        var second = await _service.ReserverProchainNumeroFactureAsync(annee);
        var troisieme = await _service.ReserverProchainNumeroFactureAsync(annee);

        // Assert : chaque réservation retourne un numéro distinct et croissant
        Assert.Equal(1, premier);
        Assert.Equal(2, second);
        Assert.Equal(3, troisieme);
    }

    [Fact]
    public async Task ReserverProchainNumeroFactureAsync_NewYear_ResetsCounter()
    {
        // Arrange : l'année précédente s'est terminée au numéro 50
        await _service.InitializeDatabaseAsync();
        await _service.SetConfigurationAsync("derniere_annee_facture", (DateTime.Now.Year - 1).ToString());
        await _service.SetConfigurationAsync("prochain_numero", "50");

        // Act
        var premier = await _service.ReserverProchainNumeroFactureAsync(DateTime.Now.Year);
        var second = await _service.ReserverProchainNumeroFactureAsync(DateTime.Now.Year);

        // Assert : réinitialisation à 1 puis 2 (et non 50 -> 51 comme avant le correctif)
        Assert.Equal(1, premier);
        Assert.Equal(2, second);
    }

    [Fact]
    public async Task ReserverProchainNumeroFactureAsync_SameYear_ContinuesCounter()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        await _service.SetConfigurationAsync("derniere_annee_facture", DateTime.Now.Year.ToString());
        await _service.SetConfigurationAsync("prochain_numero", "7");

        // Act
        var numero = await _service.ReserverProchainNumeroFactureAsync(DateTime.Now.Year);

        // Assert
        Assert.Equal(7, numero);
    }

    #endregion

    #region Filtrage et statistiques des factures (côté base de données)

    [Fact]
    public async Task GetFacturesFiltreesAsync_ReturnsOnlyMatchingYear()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        var facture2025 = CreateTestFacture("F2025", business.Id);
        facture2025.DateFacture = new DateTime(2025, 6, 15);
        await _service.SaveFactureAsync(facture2025);

        var facture2026 = CreateTestFacture("F2026", business.Id);
        facture2026.DateFacture = new DateTime(2026, 6, 15);
        await _service.SaveFactureAsync(facture2026);

        // Act
        var resultat = await _service.GetFacturesFiltreesAsync(
            business.Id, 2025, archived: false, type: null, statut: null, recherche: null);

        // Assert
        Assert.Single(resultat);
        Assert.Equal(facture2025.NumeroFacture, resultat[0].NumeroFacture);
    }

    [Fact]
    public async Task GetFacturesFiltreesAsync_FiltersByStatutAndRecherche()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        var payee = CreateTestFacture("RECH-PAYEE", business.Id);
        payee.DateFacture = new DateTime(2026, 2, 1);
        payee.Statut = StatutFacture.Payee;
        await _service.SaveFactureAsync(payee);

        var attente = CreateTestFacture("RECH-ATTENTE", business.Id);
        attente.DateFacture = new DateTime(2026, 3, 1);
        attente.Statut = StatutFacture.EnAttente;
        await _service.SaveFactureAsync(attente);

        // Act : filtre par statut + recherche sur le numéro
        var resultat = await _service.GetFacturesFiltreesAsync(
            business.Id, 2026, archived: false, type: null, statut: StatutFacture.Payee, recherche: "RECH-");

        // Assert
        Assert.Single(resultat);
        Assert.Equal(payee.NumeroFacture, resultat[0].NumeroFacture);
    }

    [Fact]
    public async Task GetAnneesFacturesAsync_ReturnsDistinctYearsDescending()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        var f2024a = CreateTestFacture("A", business.Id);
        f2024a.DateFacture = new DateTime(2024, 3, 1);
        await _service.SaveFactureAsync(f2024a);

        var f2026 = CreateTestFacture("B", business.Id);
        f2026.DateFacture = new DateTime(2026, 3, 1);
        await _service.SaveFactureAsync(f2026);

        var f2024b = CreateTestFacture("C", business.Id);
        f2024b.DateFacture = new DateTime(2024, 9, 1);
        await _service.SaveFactureAsync(f2024b);

        // Act
        var annees = await _service.GetAnneesFacturesAsync(business.Id);

        // Assert
        Assert.Equal(new List<int> { 2026, 2024 }, annees);
    }

    [Fact]
    public async Task GetStatistiquesFacturesAsync_AggregatesYearData()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        var payee = CreateTestFacture("P", business.Id);
        payee.DateFacture = new DateTime(2026, 2, 1);
        payee.Statut = StatutFacture.Payee;
        payee.MontantTotal = 1000;
        await _service.SaveFactureAsync(payee);

        var enAttente = CreateTestFacture("W", business.Id);
        enAttente.DateFacture = new DateTime(2026, 3, 1);
        enAttente.Statut = StatutFacture.EnAttente;
        enAttente.MontantTotal = 500;
        await _service.SaveFactureAsync(enAttente);

        var annulee = CreateTestFacture("X", business.Id);
        annulee.DateFacture = new DateTime(2026, 4, 1);
        annulee.Statut = StatutFacture.Annulee;
        annulee.MontantTotal = 700;
        await _service.SaveFactureAsync(annulee);

        // Act
        var stats = await _service.GetStatistiquesFacturesAsync(business.Id, 2026);

        // Assert
        Assert.Equal(3, stats.NombreFactures);
        Assert.Equal(1500m, stats.ChiffreAffaires); // payée + en attente, hors annulée
        Assert.Equal(1, stats.FacturesPayees);
        Assert.Equal(1, stats.FacturesEnAttente);
    }

    [Fact]
    public async Task GetStatistiquesFacturesAsync_ExcludesOtherYears()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        var facture2025 = CreateTestFacture("Y", business.Id);
        facture2025.DateFacture = new DateTime(2025, 5, 1);
        await _service.SaveFactureAsync(facture2025);

        // Act
        var stats = await _service.GetStatistiquesFacturesAsync(business.Id, 2026);

        // Assert
        Assert.Equal(0, stats.NombreFactures);
        Assert.Equal(0m, stats.ChiffreAffaires);
    }

    #endregion

    #region Version de schéma et sauvegarde avant migration

    [Fact]
    public async Task InitializeDatabaseAsync_OnFreshDatabase_WritesSchemaVersion()
    {
        await _service.InitializeDatabaseAsync();

        Assert.Equal("1", await _service.GetConfigurationAsync("schema_version"));
    }

    [Fact]
    public async Task InitializeDatabaseAsync_IsIdempotent()
    {
        await _service.InitializeDatabaseAsync();
        await _service.InitializeDatabaseAsync(); // ne doit pas lever d'exception

        Assert.Equal("1", await _service.GetConfigurationAsync("schema_version"));
    }

    [Fact]
    public async Task InitializeDatabaseAsync_LegacyDatabase_BacksUpBeforeMigrating()
    {
        // Arrange : simuler une base existante sans version de schéma (ancienne installation)
        await _service.InitializeDatabaseAsync();
        await _service.SetConfigurationAsync("schema_version", "0");

        // Act
        await _service.InitializeDatabaseAsync();

        // Assert : version mise à jour
        Assert.Equal("1", await _service.GetConfigurationAsync("schema_version"));

        // Assert : une sauvegarde de sécurité a été créée avant la migration
        var sauvegardes = Directory.GetFiles(
            Path.GetTempPath(), Path.GetFileName(_testDbPath) + ".backup-*");
        Assert.NotEmpty(sauvegardes);

        foreach (var sauvegarde in sauvegardes)
        {
            try { File.Delete(sauvegarde); } catch { }
        }
    }

    #endregion

    #region Préservation des champs gérés par l'application

    [Fact]
    public async Task SaveBusinessAsync_EditingRebuiltBusiness_KeepsArchiveFlagAndCreationDate()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        var dateCreation = business.DateCreation;
        await _service.ArchiveBusinessAsync(business.Id);

        // Act : édition via un objet reconstruit, comme le fait le formulaire
        var edition = new Business
        {
            Id = business.Id,
            TypeEntreprise = business.TypeEntreprise,
            Nom = "Nom modifié",
            NomComplet = business.NomComplet,
            Adresse = business.Adresse,
            Ville = business.Ville,
            Wilaya = business.Wilaya,
            Telephone = business.Telephone,
            RC = business.RC,
            NIS = business.NIS,
            NIF = business.NIF,
            AI = business.AI,
            NumeroImmatriculation = business.NumeroImmatriculation
        };
        await _service.SaveBusinessAsync(edition);

        // Assert : l'édition ne doit ni désarchiver ni réinitialiser la date de création
        var recharge = await _service.GetBusinessByIdAsync(business.Id);
        Assert.NotNull(recharge);
        Assert.Equal("Nom modifié", recharge!.Nom);
        Assert.True(recharge.IsArchived);
        Assert.Equal(dateCreation, recharge.DateCreation);
    }

    [Fact]
    public async Task SaveClientAsync_EditingRebuiltClient_KeepsCreationDate()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        var client = new Client
        {
            BusinessId = business.Id,
            Nom = "Client Test",
            Adresse = "Rue 1",
            Telephone = "0550123456"
        };
        await _service.SaveClientAsync(client);
        var dateCreation = client.DateCreation;

        // Act
        var edition = new Client
        {
            Id = client.Id,
            BusinessId = business.Id,
            Nom = "Client Modifié",
            Adresse = "Rue 2",
            Telephone = "0660123456"
        };
        await _service.SaveClientAsync(edition);

        // Assert
        var recharge = await _service.GetClientByIdAsync(client.Id);
        Assert.NotNull(recharge);
        Assert.Equal("Client Modifié", recharge!.Nom);
        Assert.Equal(dateCreation, recharge.DateCreation);
    }

    #endregion

    #region Transactions : filtrage, pagination et totaux côté base

    [Fact]
    public async Task GetTransactionsFiltreesAsync_FiltersByTypeAndPaginates()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        for (int i = 0; i < 5; i++)
        {
            await _service.SaveTransactionAsync(new Transaction
            {
                BusinessId = business.Id,
                Date = new DateTime(2026, 3, 10).AddDays(i),
                Description = $"Recette {i}",
                Montant = 100 + i,
                Type = TypeTransaction.Recette,
                Categorie = "Ventes"
            });
        }

        await _service.SaveTransactionAsync(new Transaction
        {
            BusinessId = business.Id,
            Date = new DateTime(2026, 3, 10),
            Description = "Dépense",
            Montant = 999,
            Type = TypeTransaction.Depense,
            Categorie = "Achats"
        });

        var debut = new DateTime(2026, 3, 1);
        var fin = new DateTime(2026, 3, 31);

        // Act
        var total = await _service.GetNombreTransactionsFiltreesAsync(
            business.Id, afficherArchivees: false, debut, fin, TypeTransaction.Recette, null);
        var page = await _service.GetTransactionsFiltreesAsync(
            business.Id, afficherArchivees: false, debut, fin, TypeTransaction.Recette, null, skip: 0, take: 2);

        // Assert
        Assert.Equal(5, total);
        Assert.Equal(2, page.Count);
        Assert.All(page, t => Assert.Equal(TypeTransaction.Recette, t.Type));
    }

    [Fact]
    public async Task GetTransactionsFiltreesAsync_IncludesWholeEndDay()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        await _service.SaveTransactionAsync(new Transaction
        {
            BusinessId = business.Id,
            Date = new DateTime(2026, 3, 31, 23, 59, 0), // dernier moment du jour de fin
            Description = "En fin de journée",
            Montant = 50,
            Type = TypeTransaction.Recette,
            Categorie = "Ventes"
        });

        // Act : la borne de fin est un jour entier, pas un instant à minuit
        var page = await _service.GetTransactionsFiltreesAsync(
            business.Id, afficherArchivees: false,
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 31),
            type: null, categorie: null, skip: 0, take: 10);

        // Assert
        Assert.Single(page);
    }

    [Fact]
    public async Task GetTotauxTransactionsAsync_ExcludesArchivedAndSplitsByType()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var business = CreateTestBusiness();
        await _service.SaveBusinessAsync(business);

        await _service.SaveTransactionAsync(new Transaction
        {
            BusinessId = business.Id,
            Date = new DateTime(2026, 3, 5),
            Description = "Recette",
            Montant = 1000,
            Type = TypeTransaction.Recette,
            Categorie = "Ventes"
        });

        await _service.SaveTransactionAsync(new Transaction
        {
            BusinessId = business.Id,
            Date = new DateTime(2026, 3, 6),
            Description = "Dépense",
            Montant = 400,
            Type = TypeTransaction.Depense,
            Categorie = "Achats"
        });

        var annulee = new Transaction
        {
            BusinessId = business.Id,
            Date = new DateTime(2026, 3, 7),
            Description = "Recette annulée",
            Montant = 5000,
            Type = TypeTransaction.Recette,
            Categorie = "Ventes"
        };
        await _service.SaveTransactionAsync(annulee);
        await _service.ArchiveTransactionAsync(annulee.Id);

        // Act
        var (recettes, depenses) = await _service.GetTotauxTransactionsAsync(
            business.Id, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        // Assert
        Assert.Equal(1000m, recettes);
        Assert.Equal(400m, depenses);
    }

    #endregion
}
