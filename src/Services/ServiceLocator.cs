using System;
using Microsoft.Extensions.DependencyInjection;

namespace FatouraDZ.Services;

/// <summary>
/// Point d'accès unique aux services de l'application.
/// Les services sont résolus par un conteneur d'injection de dépendances
/// (Microsoft.Extensions.DependencyInjection) construit au premier accès.
/// Ce "locator" sert de passerelle : il permet une migration progressive des ViewModels
/// vers l'injection par constructeur, et d'injecter des doublures dans les tests via
/// <see cref="SetProvider"/>.
/// </summary>
public static class ServiceLocator
{
    private static readonly object Verrou = new();
    private static IServiceProvider? _provider;

    /// <summary>
    /// Remplace le conteneur de services (utilisé par les tests pour injecter des doubles).
    /// </summary>
    public static void SetProvider(IServiceProvider provider)
    {
        lock (Verrou)
        {
            _provider = provider;
        }
    }

    private static IServiceProvider Provider
    {
        get
        {
            if (_provider != null)
                return _provider;

            lock (Verrou)
            {
                return _provider ??= ConstruireConteneurParDefaut();
            }
        }
    }

    private static IServiceProvider ConstruireConteneurParDefaut()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<ICalculationService, CalculationService>();
        services.AddSingleton<INumberToWordsService, NumberToWordsService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IAppLogger, FileLogger>();
        services.AddSingleton<ICrashReporter, CrashReporter>();
        // Un seul hôte interrogé (api.github.com) : un HttpClient unique suffit et
        // évite l'épuisement de sockets des clients créés à la demande.
        services.AddSingleton<System.Net.Http.HttpClient>(_ => GitHubReleaseClient.CreerHttpClient());
        services.AddSingleton<IReleaseGitHubClient, GitHubReleaseClient>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IInvoiceNumberService, InvoiceNumberService>();
        services.AddSingleton<IPdfService, PdfService>();
        services.AddSingleton<IExcelService, ExcelService>();

        return services.BuildServiceProvider();
    }

    public static IDatabaseService DatabaseService => Provider.GetRequiredService<IDatabaseService>();

    public static ICalculationService CalculationService => Provider.GetRequiredService<ICalculationService>();

    public static IInvoiceNumberService InvoiceNumberService => Provider.GetRequiredService<IInvoiceNumberService>();

    public static INumberToWordsService NumberToWordsService => Provider.GetRequiredService<INumberToWordsService>();

    public static IPdfService PdfService => Provider.GetRequiredService<IPdfService>();

    public static IExcelService ExcelService => Provider.GetRequiredService<IExcelService>();

    public static IValidationService ValidationService => Provider.GetRequiredService<IValidationService>();

    public static ICrashReporter CrashReporter => Provider.GetRequiredService<ICrashReporter>();

    public static IUpdateService UpdateService => Provider.GetRequiredService<IUpdateService>();

    public static IAppLogger Logger => Provider.GetRequiredService<IAppLogger>();
}
