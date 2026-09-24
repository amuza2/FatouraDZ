using FatouraDZ.Models;
using FatouraDZ.Services;
using FatouraDZ.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FatouraDZ.Tests.ViewModels;

/// <summary>
/// Garde des modifications non enregistrées : quitter le formulaire de facture en cours
/// de saisie doit toujours passer par une confirmation explicite.
/// </summary>
public class NouvelleFactureGuardTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _originalDbPath;
    private readonly DatabaseService _service;

    public NouvelleFactureGuardTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"fatouradz_guard_{Guid.NewGuid()}.db");
        _originalDbPath = AppSettings.Instance.DatabasePath;
        AppSettings.Instance.DatabasePath = _testDbPath;
        _service = new DatabaseService();

        var services = new ServiceCollection();
        services.AddSingleton<IDatabaseService>(_service);
        services.AddSingleton<ICalculationService, CalculationService>();
        services.AddSingleton<INumberToWordsService, NumberToWordsService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IAppLogger, FileLogger>();
        services.AddSingleton<IInvoiceNumberService, InvoiceNumberService>();
        ServiceLocator.SetProvider(services.BuildServiceProvider());
    }

    public void Dispose()
    {
        ServiceLocator.SetProvider(null!);
        AppSettings.Instance.DatabasePath = _originalDbPath;
        try
        {
            if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
        }
        catch
        {
            // Le fichier temporaire sera nettoyé par le système.
        }
    }

    #region Détection des saisies

    [Fact]
    public void NouveauFormulaire_AucuneSaisie_EstPropre()
    {
        var viewModel = new NouvelleFactureViewModel();

        Assert.False(viewModel.EstModifie);
    }

    [Fact]
    public void SaisieClient_EstDetectee()
    {
        var viewModel = new NouvelleFactureViewModel();

        viewModel.ClientNom = "Client Test";

        Assert.True(viewModel.EstModifie);
    }

    [Fact]
    public void SaisieSurUneLigne_EstDetectee()
    {
        var viewModel = new NouvelleFactureViewModel();

        viewModel.Lignes[0].Designation = "Prestation";
        viewModel.Lignes[0].Quantite = 3;
        viewModel.Lignes[0].PrixUnitaire = 1200;

        Assert.True(viewModel.EstModifie);
    }

    [Fact]
    public void OptionsModifiees_SontDetectees()
    {
        var viewModel = new NouvelleFactureViewModel();

        viewModel.RemiseGlobale = 5;

        Assert.True(viewModel.EstModifie);
    }

    [Fact]
    public void FactureChargee_EstPropre_JusquALaPremiereSaisie()
    {
        var viewModel = new NouvelleFactureViewModel();
        var facture = new Facture
        {
            Id = 0,
            NumeroFacture = "FAC-2026-001",
            DateFacture = DateTime.Now.Date,
            DateEcheance = DateTime.Now.Date.AddDays(30),
            ModePaiement = "Espèces",
            ClientNom = "Client Existant",
            ClientAdresse = "1 rue de Test",
            ClientTelephone = "0550123456"
        };
        facture.Lignes.Add(new LigneFacture
        {
            NumeroLigne = 1,
            Designation = "Prestation",
            Quantite = 1,
            PrixUnitaire = 1000
        });

        viewModel.ChargerFacture(facture);

        // Le simple chargement ne rend pas le formulaire "modifié".
        Assert.False(viewModel.EstModifie);

        viewModel.ClientAdresse = "2 rue de Test";

        Assert.True(viewModel.EstModifie);
    }

    #endregion

    #region Demande de confirmation

    [Fact]
    public async Task FormulairePropre_AucuneConfirmationNEstDemandee()
    {
        var viewModel = new NouvelleFactureViewModel();
        var demandes = 0;
        viewModel.DemanderConfirmation += (_, _) =>
        {
            demandes++;
            return Task.FromResult(true);
        };

        var peutQuitter = await viewModel.ConfirmerAbandonAsync();

        Assert.True(peutQuitter);
        Assert.Equal(0, demandes);
    }

    [Fact]
    public async Task FormulaireModifie_ConfirmationDemandee_EtAbandonAutorise()
    {
        var viewModel = new NouvelleFactureViewModel();
        var demandes = 0;
        viewModel.DemanderConfirmation += (_, _) =>
        {
            demandes++;
            return Task.FromResult(true);
        };

        viewModel.ClientNom = "Client Test";

        var peutQuitter = await viewModel.ConfirmerAbandonAsync();

        Assert.True(peutQuitter);
        Assert.Equal(1, demandes);
    }

    [Fact]
    public async Task FormulaireModifie_RefusDeLUtilisateur_BloqueLaSortie()
    {
        var viewModel = new NouvelleFactureViewModel();
        viewModel.DemanderConfirmation += (_, _) => Task.FromResult(false);
        var annulerDemandeEmis = false;
        viewModel.AnnulerDemande += () => annulerDemandeEmis = true;

        viewModel.ClientNom = "Client Test";

        await viewModel.AnnulerCommand.ExecuteAsync(null);

        // La commande Retour ne doit ni quitter, ni perdre la saisie.
        Assert.False(annulerDemandeEmis);
        Assert.True(viewModel.EstModifie);
        Assert.Equal("Client Test", viewModel.ClientNom);
    }

    [Fact]
    public async Task FormulaireModifie_ConfirmationAcceptee_LaisseSortir()
    {
        var viewModel = new NouvelleFactureViewModel();
        viewModel.DemanderConfirmation += (_, _) => Task.FromResult(true);
        var annulerDemandeEmis = false;
        viewModel.AnnulerDemande += () => annulerDemandeEmis = true;

        viewModel.ClientNom = "Client Test";

        await viewModel.AnnulerCommand.ExecuteAsync(null);

        Assert.True(annulerDemandeEmis);
    }

    [Fact]
    public async Task Reinitialiser_Refuse_ParLUtilisateur_ConserveLaSaisie()
    {
        var viewModel = new NouvelleFactureViewModel();
        viewModel.DemanderConfirmation += (_, _) => Task.FromResult(false);

        viewModel.ClientNom = "Client Test";

        await viewModel.ReinitialiserCommand.ExecuteAsync(null);

        Assert.Equal("Client Test", viewModel.ClientNom);
    }

    [Fact]
    public async Task Reinitialiser_Accepte_ParLUtilisateur_RemetLeFormulaireAZero()
    {
        var viewModel = new NouvelleFactureViewModel();
        viewModel.DemanderConfirmation += (_, _) => Task.FromResult(true);

        viewModel.ClientNom = "Client Test";

        await viewModel.ReinitialiserCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, viewModel.ClientNom);
        Assert.False(viewModel.EstModifie);
    }

    #endregion

    #region Enregistrement

    [Fact]
    public async Task SauvegardeReussie_LeFormulaireNEstPlusModifie()
    {
        // Arrange : une entreprise et un client réels (la facture référence le client).
        await _service.InitializeDatabaseAsync();

        var business = new Business { Nom = "Entreprise Test", Ville = "Alger", Wilaya = "Alger" };
        await _service.SaveBusinessAsync(business);

        var client = new Client
        {
            BusinessId = business.Id,
            Nom = "Client Test",
            Adresse = "1 rue de Test",
            Telephone = "0550123456"
        };
        await _service.SaveClientAsync(client);

        var viewModel = new NouvelleFactureViewModel();
        viewModel.SetBusiness(business);
        await viewModel.InitialiserAsync();

        viewModel.ClientSelectionne = client;
        viewModel.Lignes[0].Designation = "Prestation de service";
        viewModel.Lignes[0].Quantite = 2;
        viewModel.Lignes[0].PrixUnitaire = 1000;

        Assert.True(viewModel.EstModifie);

        var demandes = 0;
        viewModel.DemanderConfirmation += (_, _) =>
        {
            demandes++;
            return Task.FromResult(true);
        };

        // Act
        await viewModel.SauvegarderCommand.ExecuteAsync(null);

        // Assert : enregistrement pris en compte, plus aucune saisie à protéger.
        Assert.Equal(string.Empty, viewModel.ErreurMessage ?? string.Empty);
        Assert.False(viewModel.EstModifie);
        Assert.True(await viewModel.ConfirmerAbandonAsync());
        Assert.Equal(0, demandes);
    }

    #endregion

    #region Navigation de la fenêtre principale

    [Fact]
    public async Task MainWindow_ContenuHorsFacture_PasDeConfirmation()
    {
        var viewModel = new MainWindowViewModel();
        var demandes = 0;
        viewModel.DemanderConfirmationDialog += (_, _) =>
        {
            demandes++;
            return Task.FromResult(true);
        };

        viewModel.ContenuActuel = new AProposViewModel();

        Assert.False(viewModel.EstFormulaireFactureModifie);
        Assert.True(await viewModel.ConfirmerAbandonSaisieAsync());
        Assert.Equal(0, demandes);
    }

    [Fact]
    public async Task MainWindow_FactureModifiee_RefusDeLUtilisateur_LaNavigationEstBloquee()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.DemanderConfirmationDialog += (_, _) => Task.FromResult(false);

        var facture = new NouvelleFactureViewModel();
        var demandes = 0;
        facture.DemanderConfirmation += (_, _) =>
        {
            demandes++;
            return Task.FromResult(false);
        };
        facture.ClientNom = "Client Test";
        viewModel.ContenuActuel = facture;

        Assert.True(viewModel.EstFormulaireFactureModifie);
        Assert.False(await viewModel.ConfirmerAbandonSaisieAsync());
        Assert.Equal(1, demandes);

        // La commande de navigation ne doit pas remplacer le formulaire.
        viewModel.AfficherListeEntreprisesCommand.Execute(null);
        await Task.Delay(50);

        Assert.Same(facture, viewModel.ContenuActuel);
        Assert.Equal(2, demandes);
    }

    [Fact]
    public async Task MainWindow_FactureModifiee_ConfirmationAcceptee_LaNavigationContinue()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.DemanderConfirmationDialog += (_, _) => Task.FromResult(true);

        var facture = new NouvelleFactureViewModel();
        facture.DemanderConfirmation += (_, _) => Task.FromResult(true);
        facture.ClientNom = "Client Test";
        viewModel.ContenuActuel = facture;

        Assert.True(await viewModel.ConfirmerAbandonSaisieAsync());
    }

    #endregion
}
