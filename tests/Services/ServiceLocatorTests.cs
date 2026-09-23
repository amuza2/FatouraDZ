using FatouraDZ.Services;

namespace FatouraDZ.Tests.Services;

public class ServiceLocatorTests
{
    [Fact]
    public void TousLesServices_SontResolvables()
    {
        // Le conteneur d'injection de dépendances doit pouvoir fournir tous les services.
        Assert.NotNull(ServiceLocator.DatabaseService);
        Assert.NotNull(ServiceLocator.CalculationService);
        Assert.NotNull(ServiceLocator.InvoiceNumberService);
        Assert.NotNull(ServiceLocator.NumberToWordsService);
        Assert.NotNull(ServiceLocator.PdfService);
        Assert.NotNull(ServiceLocator.ExcelService);
        Assert.NotNull(ServiceLocator.ValidationService);
        Assert.NotNull(ServiceLocator.Logger);
    }

    [Fact]
    public void Services_SontDesSingletons()
    {
        Assert.Same(ServiceLocator.DatabaseService, ServiceLocator.DatabaseService);
        Assert.Same(ServiceLocator.CalculationService, ServiceLocator.CalculationService);
        Assert.Same(ServiceLocator.Logger, ServiceLocator.Logger);
    }
}
