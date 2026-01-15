using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.Tests.Services;

public class ValidationServiceTests
{
    private readonly ValidationService _service;

    public ValidationServiceTests()
    {
        _service = new ValidationService();
    }

    #region EstTelephoneValide Tests

    [Theory]
    [InlineData("0550123456", true)]   // Mobile 05
    [InlineData("0660123456", true)]   // Mobile 06
    [InlineData("0770123456", true)]   // Mobile 07
    [InlineData("05 50 12 34 56", true)] // With spaces
    [InlineData("05-50-12-34-56", true)] // With dashes
    [InlineData("05.50.12.34.56", true)] // With dots
    [InlineData("024456521", true)]    // Fixe (9 digits)
    [InlineData("021234567", true)]    // Fixe Alger
    [InlineData("0550123", false)]     // Too short
    [InlineData("05501234567", false)] // Too long
    [InlineData("0850123456", false)]  // Invalid prefix
    [InlineData("1234567890", false)]  // No leading 0
    [InlineData("", false)]            // Empty
    [InlineData(null, false)]          // Null
    public void EstTelephoneValide_ReturnsExpectedResult(string? telephone, bool expected)
    {
        var result = _service.EstTelephoneValide(telephone!);
        Assert.Equal(expected, result);
    }

    #endregion

    #region EstNISValide Tests

    [Theory]
    [InlineData("123456789012345", true)]  // 15 digits
    [InlineData("000000000000000", true)]  // 15 zeros
    [InlineData("12345678901234", false)]  // 14 digits
    [InlineData("1234567890123456", false)]// 16 digits
    [InlineData("12345678901234A", false)] // Contains letter
    [InlineData("", false)]                // Empty
    [InlineData(null, false)]              // Null
    [InlineData("123 456 789 012 345", true)] // With spaces (should be cleaned)
    public void EstNISValide_ReturnsExpectedResult(string? nis, bool expected)
    {
        var result = _service.EstNISValide(nis!);
        Assert.Equal(expected, result);
    }

    #endregion

    #region ValiderEntrepreneur Tests

    [Fact]
    public void ValiderEntrepreneur_ValidData_ReturnsNoErrors()
    {
        var entrepreneur = CreateValidEntrepreneur();

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.True(result.EstValide);
        Assert.Empty(result.Erreurs);
    }

    [Fact]
    public void ValiderEntrepreneur_MissingNomComplet_ReturnsError()
    {
        var entrepreneur = CreateValidEntrepreneur();
        entrepreneur.NomComplet = "";

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("nom complet"));
    }

    [Fact]
    public void ValiderEntrepreneur_MissingAdresse_ReturnsError()
    {
        var entrepreneur = CreateValidEntrepreneur();
        entrepreneur.Adresse = "";

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("adresse"));
    }

    [Fact]
    public void ValiderEntrepreneur_InvalidTelephone_ReturnsError()
    {
        var entrepreneur = CreateValidEntrepreneur();
        entrepreneur.Telephone = "123456";

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("téléphone"));
    }

    [Fact]
    public void ValiderEntrepreneur_InvalidNIS_ReturnsError()
    {
        var entrepreneur = CreateValidEntrepreneur();
        entrepreneur.NIS = "12345"; // Too short

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("NIS"));
    }

    [Fact]
    public void ValiderEntrepreneur_InvalidEmail_ReturnsError()
    {
        var entrepreneur = CreateValidEntrepreneur();
        entrepreneur.Email = "invalid-email";

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("email"));
    }

    [Fact]
    public void ValiderEntrepreneur_EmptyEmail_IsValid()
    {
        var entrepreneur = CreateValidEntrepreneur();
        entrepreneur.Email = "";

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.True(result.EstValide);
    }

    [Fact]
    public void ValiderEntrepreneur_MissingRC_ReturnsError_ForForfait()
    {
        var entrepreneur = CreateValidEntrepreneur();
        entrepreneur.TypeEntreprise = BusinessType.Forfait; // RC required for Forfait
        entrepreneur.RC = "";

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("RC"));
    }

    [Fact]
    public void ValiderEntrepreneur_MissingNIF_ReturnsError()
    {
        var entrepreneur = CreateValidEntrepreneur();
        entrepreneur.NIF = "";

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("NIF"));
    }

    [Fact]
    public void ValiderEntrepreneur_MissingAI_ReturnsError()
    {
        var entrepreneur = CreateValidEntrepreneur();
        entrepreneur.AI = "";

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("AI"));
    }

    [Fact]
    public void ValiderEntrepreneur_MissingNumeroImmatriculation_ReturnsError()
    {
        var entrepreneur = CreateValidEntrepreneur();
        entrepreneur.NumeroImmatriculation = "";

        var result = _service.ValiderEntrepreneur(entrepreneur);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("immatriculation"));
    }

    #endregion

    #region ValiderFacture Tests

    [Fact]
    public void ValiderFacture_ValidData_ReturnsNoErrors()
    {
        var facture = CreateValidFacture();

        var result = _service.ValiderFacture(facture);

        Assert.True(result.EstValide);
        Assert.Empty(result.Erreurs);
    }

    [Fact]
    public void ValiderFacture_MissingClientNom_ReturnsError()
    {
        var facture = CreateValidFacture();
        facture.ClientNom = "";

        var result = _service.ValiderFacture(facture);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("nom du client"));
    }

    [Fact]
    public void ValiderFacture_MissingClientAdresse_ReturnsError()
    {
        var facture = CreateValidFacture();
        facture.ClientAdresse = "";

        var result = _service.ValiderFacture(facture);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("adresse du client"));
    }

    [Fact]
    public void ValiderFacture_InvalidClientTelephone_ReturnsError()
    {
        var facture = CreateValidFacture();
        facture.ClientTelephone = "123";

        var result = _service.ValiderFacture(facture);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("téléphone du client"));
    }

    [Fact]
    public void ValiderFacture_MissingModePaiement_ReturnsError()
    {
        var facture = CreateValidFacture();
        facture.ModePaiement = "";

        var result = _service.ValiderFacture(facture);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("mode de paiement"));
    }

    [Fact]
    public void ValiderFacture_DateEcheanceBeforeDateFacture_ReturnsError()
    {
        var facture = CreateValidFacture();
        facture.DateFacture = DateTime.Today;
        facture.DateEcheance = DateTime.Today.AddDays(-1);

        var result = _service.ValiderFacture(facture);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("échéance"));
    }

    [Fact]
    public void ValiderFacture_NoLines_ReturnsError()
    {
        var facture = CreateValidFacture();
        facture.Lignes = new List<LigneFacture>();

        var result = _service.ValiderFacture(facture);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("au moins une ligne"));
    }

    [Fact]
    public void ValiderFacture_NullLines_ReturnsError()
    {
        var facture = CreateValidFacture();
        facture.Lignes = null!;

        var result = _service.ValiderFacture(facture);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("au moins une ligne"));
    }

    [Fact]
    public void ValiderFacture_InvalidClientEmail_ReturnsError()
    {
        var facture = CreateValidFacture();
        facture.ClientEmail = "not-an-email";

        var result = _service.ValiderFacture(facture);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("email du client"));
    }

    #endregion

    #region ValiderLigneFacture Tests

    [Fact]
    public void ValiderLigneFacture_ValidData_ReturnsNoErrors()
    {
        var ligne = new LigneFacture
        {
            Designation = "Service de consultation",
            Quantite = 1,
            PrixUnitaire = 5000
        };

        var result = _service.ValiderLigneFacture(ligne);

        Assert.True(result.EstValide);
        Assert.Empty(result.Erreurs);
    }

    [Fact]
    public void ValiderLigneFacture_MissingDesignation_ReturnsError()
    {
        var ligne = new LigneFacture
        {
            Designation = "",
            Quantite = 1,
            PrixUnitaire = 5000
        };

        var result = _service.ValiderLigneFacture(ligne);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("désignation"));
    }

    [Fact]
    public void ValiderLigneFacture_ZeroQuantite_ReturnsError()
    {
        var ligne = new LigneFacture
        {
            Designation = "Service",
            Quantite = 0,
            PrixUnitaire = 5000
        };

        var result = _service.ValiderLigneFacture(ligne);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("quantité"));
    }

    [Fact]
    public void ValiderLigneFacture_NegativeQuantite_ReturnsError()
    {
        var ligne = new LigneFacture
        {
            Designation = "Service",
            Quantite = -1,
            PrixUnitaire = 5000
        };

        var result = _service.ValiderLigneFacture(ligne);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("quantité"));
    }

    [Fact]
    public void ValiderLigneFacture_NegativePrixUnitaire_ReturnsError()
    {
        var ligne = new LigneFacture
        {
            Designation = "Service",
            Quantite = 1,
            PrixUnitaire = -100
        };

        var result = _service.ValiderLigneFacture(ligne);

        Assert.False(result.EstValide);
        Assert.Contains(result.Erreurs, e => e.Contains("prix unitaire"));
    }

    [Fact]
    public void ValiderLigneFacture_ZeroPrixUnitaire_IsValid()
    {
        var ligne = new LigneFacture
        {
            Designation = "Service gratuit",
            Quantite = 1,
            PrixUnitaire = 0
        };

        var result = _service.ValiderLigneFacture(ligne);

        Assert.True(result.EstValide);
    }

    #endregion

    #region Helper Methods

    private static Entrepreneur CreateValidEntrepreneur()
    {
        return new Entrepreneur
        {
            TypeEntreprise = BusinessType.AutoEntrepreneur,
            NomComplet = "Mohammed Chami",
            Activite = "Développement logiciel",
            Adresse = "123 Rue Test",
            Ville = "Alger",
            Wilaya = "Alger",
            CodePostal = "16000",
            Telephone = "0550123456",
            Email = "test@example.com",
            NIS = "123456789012345",
            NIF = "NIF123456",
            AI = "AI123456",
            NumeroImmatriculation = "IMM123456"
        };
    }

    private static Facture CreateValidFacture()
    {
        return new Facture
        {
            NumeroFacture = "FAC-2025-001",
            DateFacture = DateTime.Today,
            DateEcheance = DateTime.Today.AddDays(30),
            TypeFacture = TypeFacture.Normale,
            ModePaiement = "Espèces",
            ClientNom = "Client Test",
            ClientAdresse = "456 Rue Client",
            ClientTelephone = "0660123456",
            ClientEmail = "client@example.com",
            Lignes = new List<LigneFacture>
            {
                new() { Designation = "Service", Quantite = 1, PrixUnitaire = 1000 }
            }
        };
    }

    #endregion
}
