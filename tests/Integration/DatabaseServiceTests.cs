using FatouraDZ.Database;
using FatouraDZ.Models;
using FatouraDZ.Services;
using Microsoft.EntityFrameworkCore;

namespace FatouraDZ.Tests.Integration;

public class DatabaseServiceTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly DatabaseService _service;

    public DatabaseServiceTests()
    {
        // Create a unique test database for each test run
        _testDbPath = Path.Combine(Path.GetTempPath(), $"fatouradz_test_{Guid.NewGuid()}.db");
        Environment.SetEnvironmentVariable("FATOURADZ_TEST_DB", _testDbPath);
        _service = new DatabaseService();
    }

    public void Dispose()
    {
        // Clean up test database
        Environment.SetEnvironmentVariable("FATOURADZ_TEST_DB", null);
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
}
