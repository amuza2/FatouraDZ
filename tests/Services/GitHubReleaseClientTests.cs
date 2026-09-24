using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FatouraDZ.Services;
using Moq;

namespace FatouraDZ.Tests.Services;

/// <summary>
/// Comportement HTTP face à l'API GitHub : les réponses d'erreur et les pannes
/// réseau doivent être absorbées, et la requête doit rester conforme (GitHub
/// rejette les requêtes sans User-Agent).
/// </summary>
public class GitHubReleaseClientTests
{
    private readonly Mock<IAppLogger> _logger = new();

    private sealed class FauxHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _fabrique;

        public HttpRequestMessage? DerniereRequete { get; private set; }

        public FauxHandler(HttpStatusCode statut, string contenu = "{}")
            : this(_ => new HttpResponseMessage(statut) { Content = new StringContent(contenu) })
        {
        }

        public FauxHandler(Func<HttpRequestMessage, HttpResponseMessage> fabrique) => _fabrique = fabrique;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            DerniereRequete = request;
            return Task.FromResult(_fabrique(request));
        }
    }

    private sealed class HandlerEnPanne : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Nom d'hôte introuvable");
    }

    private static GitHubReleaseClient Creer(IAppLogger logger, HttpMessageHandler handler) =>
        // Le client configuré (User-Agent, délai) est celui de production : c'est lui
        // qu'on veut vérifier, pas un HttpClient nu.
        new(logger, GitHubReleaseClient.CreerHttpClient(handler));

    [Fact]
    public async Task ObtenirDerniereRelease_ReponseValide_RetourneLeJson()
    {
        var handler = new FauxHandler(HttpStatusCode.OK, """{ "tag_name": "v0.2.0" }""");
        var client = Creer(_logger.Object, handler);

        var json = await client.ObtenirDerniereReleaseJsonAsync();

        Assert.Equal("""{ "tag_name": "v0.2.0" }""", json);
    }

    [Fact]
    public async Task ObtenirDerniereRelease_InterrogeLApiDuDepotConfigure()
    {
        var handler = new FauxHandler(HttpStatusCode.OK);
        var client = Creer(_logger.Object, handler);

        await client.ObtenirDerniereReleaseJsonAsync();

        Assert.Equal(
            $"https://api.github.com/repos/{AppInfo.DepotGitHub}/releases/latest",
            handler.DerniereRequete!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ObtenirDerniereRelease_EnvoieUnUserAgent()
    {
        // GitHub répond 403 sans User-Agent : c'est une régression silencieuse facile à introduire.
        var handler = new FauxHandler(HttpStatusCode.OK);
        var client = Creer(_logger.Object, handler);

        await client.ObtenirDerniereReleaseJsonAsync();

        var agent = handler.DerniereRequete!.Headers.UserAgent.ToString();
        Assert.Contains(AppInfo.NomProduit, agent);
        Assert.Contains(AppInfo.Version, agent);
    }

    [Fact]
    public async Task ObtenirDerniereRelease_404_RetourneNull()
    {
        var client = Creer(_logger.Object, new FauxHandler(HttpStatusCode.NotFound));

        Assert.Null(await client.ObtenirDerniereReleaseJsonAsync());
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ObtenirDerniereRelease_ErreurServeur_RetourneNull(HttpStatusCode statut)
    {
        var client = Creer(_logger.Object, new FauxHandler(statut));

        Assert.Null(await client.ObtenirDerniereReleaseJsonAsync());
    }

    [Fact]
    public async Task ObtenirDerniereRelease_ReseauIndisponible_RetourneNull()
    {
        var client = Creer(_logger.Object, new HandlerEnPanne());

        Assert.Null(await client.ObtenirDerniereReleaseJsonAsync());
    }

    [Fact]
    public void CreerHttpClient_ConfigureLeDelaiEtLEnTeteAccept()
    {
        using var client = GitHubReleaseClient.CreerHttpClient();

        Assert.Equal(TimeSpan.FromSeconds(10), client.Timeout);
        Assert.Contains("application/vnd.github+json", client.DefaultRequestHeaders.Accept.ToString());
    }
}
