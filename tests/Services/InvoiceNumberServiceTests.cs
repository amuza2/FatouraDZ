using FatouraDZ.Services;
using Moq;

namespace FatouraDZ.Tests.Services;

public class InvoiceNumberServiceTests
{
    private readonly Mock<IDatabaseService> _mockDatabaseService;
    private readonly InvoiceNumberService _service;

    public InvoiceNumberServiceTests()
    {
        _mockDatabaseService = new Mock<IDatabaseService>();
        _service = new InvoiceNumberService(_mockDatabaseService.Object);
    }

    #region GenererProchainNumeroAsync Tests

    [Fact]
    public async Task GenererProchainNumeroAsync_FirstInvoiceOfYear_ReturnsNumber001()
    {
        // Arrange
        var currentYear = DateTime.Now.Year.ToString();
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("derniere_annee_facture"))
            .ReturnsAsync(currentYear);
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("prochain_numero"))
            .ReturnsAsync("1");

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-001", result);
    }

    [Fact]
    public async Task GenererProchainNumeroAsync_SecondInvoice_ReturnsNumber002()
    {
        // Arrange
        var currentYear = DateTime.Now.Year.ToString();
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("derniere_annee_facture"))
            .ReturnsAsync(currentYear);
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("prochain_numero"))
            .ReturnsAsync("2");

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-002", result);
    }

    [Fact]
    public async Task GenererProchainNumeroAsync_NewYear_ResetsToNumber001()
    {
        // Arrange
        var currentYear = DateTime.Now.Year.ToString();
        var lastYear = (DateTime.Now.Year - 1).ToString();
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("derniere_annee_facture"))
            .ReturnsAsync(lastYear);
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("prochain_numero"))
            .ReturnsAsync("50"); // Last year ended at 50

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-001", result);
        _mockDatabaseService.Verify(x => x.SetConfigurationAsync("derniere_annee_facture", currentYear), Times.Once);
    }

    [Fact]
    public async Task GenererProchainNumeroAsync_NoExistingConfig_ReturnsNumber001()
    {
        // Arrange
        var currentYear = DateTime.Now.Year.ToString();
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("derniere_annee_facture"))
            .ReturnsAsync((string?)null);
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("prochain_numero"))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-001", result);
    }

    [Fact]
    public async Task GenererProchainNumeroAsync_LargeNumber_FormatsCorrectly()
    {
        // Arrange
        var currentYear = DateTime.Now.Year.ToString();
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("derniere_annee_facture"))
            .ReturnsAsync(currentYear);
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("prochain_numero"))
            .ReturnsAsync("999");

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-999", result);
    }

    [Fact]
    public async Task GenererProchainNumeroAsync_OverThousand_FormatsCorrectly()
    {
        // Arrange
        var currentYear = DateTime.Now.Year.ToString();
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("derniere_annee_facture"))
            .ReturnsAsync(currentYear);
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("prochain_numero"))
            .ReturnsAsync("1234");

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-1234", result);
    }

    #endregion

    #region ConfirmerNumeroFactureAsync Tests

    [Fact]
    public async Task ConfirmerNumeroFactureAsync_IncrementsNumber()
    {
        // Arrange
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("prochain_numero"))
            .ReturnsAsync("5");

        // Act
        await _service.ConfirmerNumeroFactureAsync();

        // Assert
        _mockDatabaseService.Verify(x => x.SetConfigurationAsync("prochain_numero", "6"), Times.Once);
    }

    [Fact]
    public async Task ConfirmerNumeroFactureAsync_NoExistingNumber_SetsTo2()
    {
        // Arrange
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("prochain_numero"))
            .ReturnsAsync((string?)null);

        // Act
        await _service.ConfirmerNumeroFactureAsync();

        // Assert
        _mockDatabaseService.Verify(x => x.SetConfigurationAsync("prochain_numero", "2"), Times.Once);
    }

    [Fact]
    public async Task ConfirmerNumeroFactureAsync_InvalidNumber_DefaultsTo1AndIncrements()
    {
        // Arrange
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("prochain_numero"))
            .ReturnsAsync("invalid");

        // Act
        await _service.ConfirmerNumeroFactureAsync();

        // Assert
        _mockDatabaseService.Verify(x => x.SetConfigurationAsync("prochain_numero", "2"), Times.Once);
    }

    #endregion

    #region Format Tests

    [Fact]
    public async Task GenererProchainNumeroAsync_FormatIsCorrect()
    {
        // Arrange
        var currentYear = DateTime.Now.Year.ToString();
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("derniere_annee_facture"))
            .ReturnsAsync(currentYear);
        _mockDatabaseService.Setup(x => x.GetConfigurationAsync("prochain_numero"))
            .ReturnsAsync("42");

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.StartsWith("FAC-", result);
        Assert.Contains(currentYear, result);
        Assert.Matches(@"^FAC-\d{4}-\d{3,}$", result);
    }

    #endregion
}
