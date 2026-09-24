using FatouraDZ.Models;
using FatouraDZ.Services;
using FatouraDZ.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FatouraDZ.Tests.ViewModels;

/// <summary>
/// Vérifie que la recherche (frappe au clavier) est temporisée et annulable :
/// une rafale de frappes ne doit produire qu'une seule requête, après la pause,
/// et les recherches précédentes doivent être annulées.
/// </summary>
public class SearchDebounceTests : IDisposable
{
    public void Dispose()
    {
        // Restaure le conteneur de services réel pour les autres tests.
        ServiceLocator.SetProvider(null!);
    }

    [Fact]
    public async Task ClientList_SaisieRapide_NeLanceQuUneRequeteApresLaPause()
    {
        // Arrange
        var appels = 0;
        var mockDatabase = new Mock<IDatabaseService>();
        mockDatabase
            .Setup(s => s.GetClientsByBusinessIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => appels++)
            .ReturnsAsync(new List<Client>());
        ServiceLocator.SetProvider(new ServiceCollection()
            .AddSingleton(mockDatabase.Object)
            .BuildServiceProvider());

        var viewModel = new ClientListViewModel();
        viewModel.SetBusiness(new Business { Id = 1, Nom = "Entreprise" });
        await viewModel.ChargerClientsAsync();
        appels = 0;

        // Act : l'utilisateur tape "client" très rapidement.
        foreach (var saisie in new[] { "c", "cl", "cli", "clie", "clien", "client" })
        {
            viewModel.Recherche = saisie;
        }

        // Assert : aucune requête pendant la frappe...
        Assert.Equal(0, appels);

        // ... puis une seule après la pause (les précédentes sont annulées).
        await Task.Delay(900);
        Assert.Equal(1, appels);
    }

    [Fact]
    public async Task BusinessDetail_SaisieRapide_NeLanceQuUneRequeteApresLaPause()
    {
        // Arrange
        var appels = 0;
        var mockDatabase = new Mock<IDatabaseService>();
        mockDatabase
            .Setup(s => s.GetFacturesFiltreesAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<TypeFacture?>(),
                It.IsAny<StatutFacture?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback(() => appels++)
            .ReturnsAsync(new List<Facture>());
        mockDatabase
            .Setup(s => s.GetAnneesFacturesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<int>());
        mockDatabase
            .Setup(s => s.GetStatistiquesFacturesAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new StatistiquesFactures());

        var mockNumeros = new Mock<IInvoiceNumberService>();
        ServiceLocator.SetProvider(new ServiceCollection()
            .AddSingleton(mockDatabase.Object)
            .AddSingleton(mockNumeros.Object)
            .BuildServiceProvider());

        var viewModel = new BusinessDetailViewModel { Business = new Business { Id = 1, Nom = "Entreprise" } };
        await viewModel.ChargerFacturesAsync();
        appels = 0;

        // Act : l'utilisateur tape "facture" très rapidement.
        foreach (var saisie in new[] { "f", "fa", "fac", "fact", "factu", "facture" })
        {
            viewModel.Recherche = saisie;
        }

        // Assert : aucune requête pendant la frappe...
        Assert.Equal(0, appels);

        // ... puis une seule après la pause (les précédentes sont annulées).
        await Task.Delay(900);
        Assert.Equal(1, appels);
    }
}
