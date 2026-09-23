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

    #region GenererProchainNumeroAsync Tests (aperçu, sans effet de bord)

    [Fact]
    public async Task GenererProchainNumeroAsync_FirstInvoiceOfYear_ReturnsNumber001()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        _mockDatabaseService.Setup(x => x.LireProchainNumeroFactureAsync(currentYear))
            .ReturnsAsync(1);

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-001", result);
    }

    [Fact]
    public async Task GenererProchainNumeroAsync_SecondInvoice_ReturnsNumber002()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        _mockDatabaseService.Setup(x => x.LireProchainNumeroFactureAsync(currentYear))
            .ReturnsAsync(2);

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-002", result);
    }

    [Fact]
    public async Task GenererProchainNumeroAsync_DoesNotConsumeNumber()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        _mockDatabaseService.Setup(x => x.LireProchainNumeroFactureAsync(currentYear))
            .ReturnsAsync(5);

        // Act
        await _service.GenererProchainNumeroAsync();
        await _service.GenererProchainNumeroAsync();

        // Assert : l'aperçu ne doit jamais réserver de numéro ni écrire en base
        _mockDatabaseService.Verify(
            x => x.ReserverProchainNumeroFactureAsync(It.IsAny<int>()), Times.Never);
        _mockDatabaseService.Verify(
            x => x.SetConfigurationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenererProchainNumeroAsync_LargeNumber_FormatsCorrectly()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        _mockDatabaseService.Setup(x => x.LireProchainNumeroFactureAsync(currentYear))
            .ReturnsAsync(999);

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-999", result);
    }

    [Fact]
    public async Task GenererProchainNumeroAsync_OverThousand_FormatsCorrectly()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        _mockDatabaseService.Setup(x => x.LireProchainNumeroFactureAsync(currentYear))
            .ReturnsAsync(1234);

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-1234", result);
    }

    #endregion

    #region AllouerNumeroFactureAsync Tests (réservation atomique)

    [Fact]
    public async Task AllouerNumeroFactureAsync_ReturnsReservedNumber()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        _mockDatabaseService.Setup(x => x.ReserverProchainNumeroFactureAsync(currentYear))
            .ReturnsAsync(5);

        // Act
        var result = await _service.AllouerNumeroFactureAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-005", result);
        _mockDatabaseService.Verify(x => x.ReserverProchainNumeroFactureAsync(currentYear), Times.Once);
    }

    [Fact]
    public async Task AllouerNumeroFactureAsync_TwoCalls_ReturnDistinctNumbers()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        _mockDatabaseService.SetupSequence(x => x.ReserverProchainNumeroFactureAsync(currentYear))
            .ReturnsAsync(1)
            .ReturnsAsync(2);

        // Act
        var premier = await _service.AllouerNumeroFactureAsync();
        var second = await _service.AllouerNumeroFactureAsync();

        // Assert
        Assert.Equal($"FAC-{currentYear}-001", premier);
        Assert.Equal($"FAC-{currentYear}-002", second);
    }

    #endregion

    #region Format Tests

    [Fact]
    public async Task GenererProchainNumeroAsync_FormatIsCorrect()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        _mockDatabaseService.Setup(x => x.LireProchainNumeroFactureAsync(currentYear))
            .ReturnsAsync(42);

        // Act
        var result = await _service.GenererProchainNumeroAsync();

        // Assert
        Assert.StartsWith("FAC-", result);
        Assert.Contains(currentYear.ToString(), result);
        Assert.Matches(@"^FAC-\d{4}-\d{3,}$", result);
    }

    #endregion

    #region Format configurable

    [Fact]
    public async Task GenererProchainNumeroAsync_UsesConfiguredFormat()
    {
        // Arrange
        var formatOriginal = AppSettings.Instance.FormatNumeroFacture;
        AppSettings.Instance.FormatNumeroFacture = "INV/{ANNEE}/{NUM}";
        try
        {
            var currentYear = DateTime.Now.Year;
            _mockDatabaseService.Setup(x => x.LireProchainNumeroFactureAsync(currentYear))
                .ReturnsAsync(7);

            // Act
            var result = await _service.GenererProchainNumeroAsync();

            // Assert
            Assert.Equal($"INV/{currentYear}/007", result);
        }
        finally
        {
            AppSettings.Instance.FormatNumeroFacture = formatOriginal;
        }
    }

    #endregion
}
