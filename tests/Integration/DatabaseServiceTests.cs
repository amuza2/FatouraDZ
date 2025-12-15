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

    #region Entrepreneur Tests

    [Fact]
    public async Task SaveEntrepreneurAsync_NewEntrepreneur_SavesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var entrepreneur = CreateTestEntrepreneur();

        // Act
        await _service.SaveEntrepreneurAsync(entrepreneur);
        var result = await _service.GetEntrepreneurAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entrepreneur.NomComplet, result.NomComplet);
        Assert.Equal(entrepreneur.Email, result.Email);
    }

    [Fact]
    public async Task SaveEntrepreneurAsync_UpdateExisting_UpdatesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var entrepreneur = CreateTestEntrepreneur();
        await _service.SaveEntrepreneurAsync(entrepreneur);

        // Act
        entrepreneur.NomComplet = "Updated Name";
        entrepreneur.Email = "updated@example.com";
        await _service.SaveEntrepreneurAsync(entrepreneur);
        var result = await _service.GetEntrepreneurAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.NomComplet);
        Assert.Equal("updated@example.com", result.Email);
    }

    [Fact]
    public async Task GetEntrepreneurAsync_NoEntrepreneur_ReturnsNull()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();

        // Act
        var result = await _service.GetEntrepreneurAsync();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Facture Tests

    [Fact]
    public async Task SaveFactureAsync_NewFacture_SavesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var facture = CreateTestFacture("FAC-2025-001");

        // Act
        await _service.SaveFactureAsync(facture);
        var result = await _service.GetFactureByNumeroAsync("FAC-2025-001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("FAC-2025-001", result.NumeroFacture);
        Assert.Equal("Client Test", result.ClientNom);
        Assert.NotEmpty(result.Lignes);
    }

    [Fact]
    public async Task SaveFactureAsync_WithLines_SavesLinesCorrectly()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var facture = CreateTestFacture("FAC-2025-002");
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
        var result = await _service.GetFactureByNumeroAsync("FAC-2025-002");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Lignes.Count);
    }

    [Fact]
    public async Task GetFacturesAsync_ReturnsAllFactures()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        await _service.SaveFactureAsync(CreateTestFacture("FAC-2025-001"));
        await _service.SaveFactureAsync(CreateTestFacture("FAC-2025-002"));
        await _service.SaveFactureAsync(CreateTestFacture("FAC-2025-003"));

        // Act
        var result = await _service.GetFacturesAsync();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetFacturesAsync_WithFilters_FiltersCorrectly()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        
        var facture1 = CreateTestFacture("FAC-2025-001");
        facture1.TypeFacture = TypeFacture.Normale;
        facture1.Statut = StatutFacture.Payee;
        
        var facture2 = CreateTestFacture("FAC-2025-002");
        facture2.TypeFacture = TypeFacture.Avoir;
        facture2.Statut = StatutFacture.EnAttente;
        
        await _service.SaveFactureAsync(facture1);
        await _service.SaveFactureAsync(facture2);

        // Act - Filter by type
        var normales = await _service.GetFacturesAsync(null, null, TypeFacture.Normale, null, null);
        var avoirs = await _service.GetFacturesAsync(null, null, TypeFacture.Avoir, null, null);

        // Assert
        Assert.Single(normales);
        Assert.Single(avoirs);
        Assert.Equal("FAC-2025-001", normales[0].NumeroFacture);
        Assert.Equal("FAC-2025-002", avoirs[0].NumeroFacture);
    }

    [Fact]
    public async Task GetFacturesAsync_WithSearch_SearchesCorrectly()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        
        var facture1 = CreateTestFacture("FAC-2025-001");
        facture1.ClientNom = "Ahmed Benali";
        
        var facture2 = CreateTestFacture("FAC-2025-002");
        facture2.ClientNom = "Mohamed Kaci";
        
        await _service.SaveFactureAsync(facture1);
        await _service.SaveFactureAsync(facture2);

        // Act
        var result = await _service.GetFacturesAsync(null, null, null, null, "Ahmed");

        // Assert
        Assert.Single(result);
        Assert.Equal("Ahmed Benali", result[0].ClientNom);
    }

    [Fact]
    public async Task DeleteFactureAsync_DeletesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var facture = CreateTestFacture("FAC-2025-001");
        await _service.SaveFactureAsync(facture);
        var saved = await _service.GetFactureByNumeroAsync("FAC-2025-001");

        // Act
        await _service.DeleteFactureAsync(saved!.Id);
        var result = await _service.GetFactureByNumeroAsync("FAC-2025-001");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateStatutFactureAsync_UpdatesStatus()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var facture = CreateTestFacture("FAC-2025-001");
        facture.Statut = StatutFacture.EnAttente;
        await _service.SaveFactureAsync(facture);
        var saved = await _service.GetFactureByNumeroAsync("FAC-2025-001");

        // Act
        await _service.UpdateStatutFactureAsync(saved!.Id, StatutFacture.Payee);
        var result = await _service.GetFactureByIdAsync(saved.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatutFacture.Payee, result.Statut);
    }

    [Fact]
    public async Task DupliquerFactureAsync_CreatesCorrectCopy()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var original = CreateTestFacture("FAC-2025-001");
        original.ClientNom = "Original Client";
        await _service.SaveFactureAsync(original);
        var saved = await _service.GetFactureByNumeroAsync("FAC-2025-001");

        // Act
        var copie = await _service.DupliquerFactureAsync(saved!.Id);

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
        await _service.SaveFactureAsync(CreateTestFacture("FAC-2025-001"));
        await _service.SaveFactureAsync(CreateTestFacture("FAC-2025-002"));

        // Act
        var count = await _service.GetNombreFacturesAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetChiffreAffairesTotalAsync_ExcludesAnnulees()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        
        var facture1 = CreateTestFacture("FAC-2025-001");
        facture1.MontantTotal = 1000;
        facture1.Statut = StatutFacture.Payee;
        
        var facture2 = CreateTestFacture("FAC-2025-002");
        facture2.MontantTotal = 2000;
        facture2.Statut = StatutFacture.Annulee; // Should be excluded
        
        await _service.SaveFactureAsync(facture1);
        await _service.SaveFactureAsync(facture2);

        // Act
        var total = await _service.GetChiffreAffairesTotalAsync();

        // Assert
        Assert.Equal(1000, total);
    }

    [Fact]
    public async Task GetDernieresFacturesAsync_ReturnsLimitedResults()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        for (int i = 1; i <= 10; i++)
        {
            await _service.SaveFactureAsync(CreateTestFacture($"FAC-2025-{i:D3}"));
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

    private static Entrepreneur CreateTestEntrepreneur()
    {
        return new Entrepreneur
        {
            NomComplet = "Test Entrepreneur",
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

    private static Facture CreateTestFacture(string numero)
    {
        return new Facture
        {
            NumeroFacture = numero,
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
