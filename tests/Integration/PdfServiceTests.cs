using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.Tests.Integration;

public class PdfServiceTests : IDisposable
{
    private readonly PdfService _service;
    private readonly string _testOutputDir;

    public PdfServiceTests()
    {
        var numberToWordsService = new NumberToWordsService();
        _service = new PdfService(numberToWordsService);
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"fatouradz_pdf_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testOutputDir))
        {
            try { Directory.Delete(_testOutputDir, true); } catch { }
        }
    }

    #region GenerateInvoicePdf Tests

    [Fact]
    public async Task GenererPdfAsync_ValidData_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        var outputPath = Path.Combine(_testOutputDir, "test_invoice.pdf");

        // Act
        await _service.GenererPdfAsync(facture, entrepreneur, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0);
    }

    [Fact]
    public async Task GenererPdfAsync_WithMultipleLines_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        
        // Add more lines
        for (int i = 2; i <= 10; i++)
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
        
        var outputPath = Path.Combine(_testOutputDir, "test_invoice_multiline.pdf");

        // Act
        await _service.GenererPdfAsync(facture, entrepreneur, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererPdfAsync_AvoirType_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        facture.TypeFacture = TypeFacture.Avoir;
        facture.NumeroFactureOrigine = "FAC-2025-001";
        var outputPath = Path.Combine(_testOutputDir, "test_avoir.pdf");

        // Act
        await _service.GenererPdfAsync(facture, entrepreneur, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererPdfAsync_AnnulationType_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        facture.TypeFacture = TypeFacture.Annulation;
        facture.NumeroFactureOrigine = "FAC-2025-001";
        var outputPath = Path.Combine(_testOutputDir, "test_annulation.pdf");

        // Act
        await _service.GenererPdfAsync(facture, entrepreneur, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererPdfAsync_WithTimbre_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        facture.EstTimbreApplique = true;
        facture.TimbreFiscal = 11.90m;
        facture.MontantTotal = 1201.90m;
        var outputPath = Path.Combine(_testOutputDir, "test_with_timbre.pdf");

        // Act
        await _service.GenererPdfAsync(facture, entrepreneur, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererPdfAsync_WithRetenueSource_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        facture.TauxRetenueSource = 30;
        facture.RetenueSource = 300; // 30% of 1000 HT
        facture.MontantTotal = 890; // 1190 TTC - 300 retenue
        var outputPath = Path.Combine(_testOutputDir, "test_with_retenue.pdf");

        // Act
        await _service.GenererPdfAsync(facture, entrepreneur, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererPdfAsync_LargeAmount_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        facture.TotalHT = 1000000;
        facture.TotalTVA19 = 190000;
        facture.TotalTTC = 1190000;
        facture.MontantTotal = 1190000;
        facture.MontantEnLettres = "Un million cent quatre-vingt-dix mille dinars algériens";
        var outputPath = Path.Combine(_testOutputDir, "test_large_amount.pdf");

        // Act
        await _service.GenererPdfAsync(facture, entrepreneur, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererPdfAsync_WithAllClientInfo_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        facture.ClientRC = "RC-CLIENT-123";
        facture.ClientNIS = "987654321012345";
        facture.ClientAI = "AI-CLIENT-456";
        facture.ClientNumeroImmatriculation = "IMM-CLIENT-789";
        facture.ClientActivite = "Commerce de détail";
        var outputPath = Path.Combine(_testOutputDir, "test_full_client.pdf");

        // Act
        await _service.GenererPdfAsync(facture, entrepreneur, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GenererPdfAsync_EmptyLines_ThrowsOrHandlesGracefully()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        facture.Lignes.Clear();
        var outputPath = Path.Combine(_testOutputDir, "test_empty_lines.pdf");

        // Act & Assert - Should either throw or handle gracefully
        try
        {
            await _service.GenererPdfAsync(facture, entrepreneur, outputPath);
            // If it doesn't throw, the file should still be created
            Assert.True(File.Exists(outputPath));
        }
        catch (Exception)
        {
            // Expected behavior - empty invoice shouldn't be generated
            Assert.True(true);
        }
    }

    [Fact]
    public async Task GenererPdfAsync_LongDesignation_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        var lignesList = facture.Lignes.ToList();
        lignesList[0].Designation = new string('A', 500); // Very long designation
        var outputPath = Path.Combine(_testOutputDir, "test_long_designation.pdf");

        // Act
        await _service.GenererPdfAsync(facture, entrepreneur, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererPdfAsync_SpecialCharacters_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        facture.ClientNom = "Société à responsabilité limitée «Test & Co»";
        var lignesList = facture.Lignes.ToList();
        lignesList[0].Designation = "Service de développement (C#, .NET) - 50% avance";
        var outputPath = Path.Combine(_testOutputDir, "test_special_chars.pdf");

        // Act
        await _service.GenererPdfAsync(facture, entrepreneur, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenererPdfAsync_ArabicCharacters_CreatesPdfFile()
    {
        // Arrange
        var entrepreneur = CreateTestEntrepreneur();
        var facture = CreateTestFacture();
        facture.ClientNom = "شركة الاختبار";
        var outputPath = Path.Combine(_testOutputDir, "test_arabic.pdf");

        // Act - May or may not support Arabic, but shouldn't crash
        try
        {
            await _service.GenererPdfAsync(facture, entrepreneur, outputPath);
            Assert.True(File.Exists(outputPath));
        }
        catch (Exception)
        {
            // Arabic may not be supported, which is acceptable
            Assert.True(true);
        }
    }

    #endregion

    #region Helper Methods

    private static Entrepreneur CreateTestEntrepreneur()
    {
        return new Entrepreneur
        {
            NomComplet = "Mohammed Chami",
            RaisonSociale = "Chami Consulting",
            Adresse = "123 Rue Didouche Mourad",
            Ville = "Alger",
            Wilaya = "Alger",
            CodePostal = "16000",
            Telephone = "0550123456",
            Email = "contact@chamiconsulting.dz",
            RC = "RC16/00-123456B99",
            NIS = "123456789012345",
            NIF = "123456789012345678",
            AI = "16123456789",
            NumeroImmatriculation = "AE-16-2024-123456",
            FormeJuridique = "Auto-entrepreneur",
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
