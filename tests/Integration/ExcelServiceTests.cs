using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.Tests.Integration;

public class ExcelServiceTests : IDisposable
{
    private readonly ExcelService _service;
    private readonly string _testOutputDir;

    public ExcelServiceTests()
    {
        var numberToWordsService = new NumberToWordsService();
        _service = new ExcelService(numberToWordsService);
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"fatouradz_excel_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
        {
            try { Directory.Delete(_testOutputDir, true); } catch { }
        }
    }

    #region GenererExcelAsync Tests

    [Fact]
    public async Task GenererExcelAsync_ValidData_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        var facture = CreateTestFacture();
        var outputPath = Path.Combine(_testOutputDir, "test_invoice.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0);
    }

    [Fact]
    public async Task GenererExcelAsync_WithMultipleLines_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        var facture = CreateTestFacture();
        
        for (int i = 2; i <= 5; i++)
        {
            facture.Lignes.Add(new LigneFacture
            {
                NumeroLigne = i,
                Designation = $"Service {i}",
                Quantite = i,
                PrixUnitaire = 100 * i,
                TauxTVA = i % 2 == 0 ? TauxTVA.TVA19 : TauxTVA.TVA9,
                TotalHT = i * 100 * i
            });
        }
        
        var outputPath = Path.Combine(_testOutputDir, "test_invoice_multiline.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererExcelAsync_AvoirType_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        var facture = CreateTestFacture();
        facture.TypeFacture = TypeFacture.Avoir;
        facture.NumeroFactureOrigine = "FAC-2025-001";
        var outputPath = Path.Combine(_testOutputDir, "test_avoir.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererExcelAsync_ProformaType_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        var facture = CreateTestFacture();
        facture.TypeFacture = TypeFacture.Proforma;
        var outputPath = Path.Combine(_testOutputDir, "test_proforma.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererExcelAsync_WithTimbre_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        var facture = CreateTestFacture();
        facture.EstTimbreApplique = true;
        facture.TimbreFiscal = 11.90m;
        facture.MontantTotal = 1201.90m;
        var outputPath = Path.Combine(_testOutputDir, "test_with_timbre.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererExcelAsync_WithRetenueSource_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        var facture = CreateTestFacture();
        facture.TauxRetenueSource = 30;
        facture.RetenueSource = 300;
        facture.MontantTotal = 890;
        var outputPath = Path.Combine(_testOutputDir, "test_with_retenue.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererExcelAsync_WithAllClientInfo_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        var facture = CreateTestFacture();
        facture.ClientRC = "RC-CLIENT-123";
        facture.ClientNIS = "987654321012345";
        facture.ClientAI = "AI-CLIENT-456";
        facture.ClientNIF = "NIF-CLIENT-789";
        facture.ClientNumeroImmatriculation = "IMM-CLIENT-789";
        facture.ClientActivite = "Commerce de détail";
        var outputPath = Path.Combine(_testOutputDir, "test_full_client.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererExcelAsync_AutoEntrepreneurBusiness_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        business.TypeEntreprise = BusinessType.AutoEntrepreneur;
        var facture = CreateTestFacture();
        var outputPath = Path.Combine(_testOutputDir, "test_auto_entrepreneur.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererExcelAsync_ReelBusiness_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        business.TypeEntreprise = BusinessType.Reel;
        business.RaisonSociale = "SARL Test Company";
        var facture = CreateTestFacture();
        var outputPath = Path.Combine(_testOutputDir, "test_reel.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererExcelAsync_LargeAmount_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        var facture = CreateTestFacture();
        facture.TotalHT = 1000000;
        facture.TotalTVA19 = 190000;
        facture.TotalTTC = 1190000;
        facture.MontantTotal = 1190000;
        facture.MontantEnLettres = "Un million cent quatre-vingt-dix mille dinars algériens";
        var outputPath = Path.Combine(_testOutputDir, "test_large_amount.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererExcelAsync_WithPaymentInfo_CreatesExcelFile()
    {
        // Arrange
        var business = CreateTestBusiness();
        var facture = CreateTestFacture();
        facture.ModePaiement = "Virement bancaire";
        facture.PaiementValeur = 1190m;
        facture.PaiementNumeroPiece = "VIR-2025-001";
        var outputPath = Path.Combine(_testOutputDir, "test_payment.xlsx");

        // Act
        await _service.GenererExcelAsync(facture, business, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    #endregion

    #region Helper Methods

    private static Business CreateTestBusiness()
    {
        return new Business
        {
            Nom = "Test Business",
            NomComplet = "Mohammed Chami",
            RaisonSociale = "Chami Consulting",
            TypeEntreprise = BusinessType.AutoEntrepreneur,
            Adresse = "123 Rue Didouche Mourad",
            Ville = "Alger",
            Wilaya = "Alger",
            CodePostal = "16000",
            Telephone = "0550123456",
            Email = "contact@test.dz",
            RC = "RC16/00-123456B99",
            NIS = "123456789012345",
            NIF = "123456789012345678",
            AI = "16123456789",
            NumeroImmatriculation = "AE-16-2024-123456",
            Activite = "Développement logiciel"
        };
    }

    private static Facture CreateTestFacture()
    {
        return new Facture
        {
            NumeroFacture = "FAC-2025-001",
            DateFacture = DateTime.Today,
            DateEcheance = DateTime.Today.AddDays(30),
            TypeFacture = TypeFacture.Normale,
            ModePaiement = "Virement bancaire",
            ClientNom = "SARL Client Test",
            ClientAdresse = "456 Boulevard Mohamed V, Oran",
            ClientTelephone = "0660123456",
            ClientEmail = "client@test.dz",
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
                    Designation = "Développement application web",
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
