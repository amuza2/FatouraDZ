using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FatouraDZ.Services;
using Moq;

namespace FatouraDZ.Tests.Services;

/// <summary>
/// La vérification de mise à jour ne doit jamais gêner l'utilisateur : hors ligne,
/// API en erreur ou réponse illisible doivent se traduire par « pas de mise à
/// jour », jamais par une exception.
/// </summary>
public class UpdateServiceTests
{
    private readonly Mock<IAppLogger> _logger = new();

    private sealed class FausseSourceReleases : IReleaseGitHubClient
    {
        private readonly string? _json;
        public int Appels { get; private set; }

        public FausseSourceReleases(string? json) => _json = json;

        public Task<string?> ObtenirDerniereReleaseJsonAsync(CancellationToken cancellationToken = default)
        {
            Appels++;
            return Task.FromResult(_json);
        }
    }

    private static string ReleaseJson(
        string tag,
        bool prerelease = false,
        bool draft = false,
        string? name = "FatouraDZ 0.2.0",
        string url = "https://github.com/amuza2/FatouraDZ/releases/tag/v0.2.0",
        string? body = "- Corrections",
        string publishedAt = "2026-09-01T10:00:00Z") =>
        $$"""
        {
          "tag_name": "{{tag}}",
          "name": "{{name}}",
          "html_url": "{{url}}",
          "body": "{{body}}",
          "published_at": "{{publishedAt}}",
          "prerelease": {{(prerelease ? "true" : "false")}},
          "draft": {{(draft ? "true" : "false")}}
        }
        """;

    private UpdateService Creer(string? json) => new(_logger.Object, new FausseSourceReleases(json));

    #region Comparaison de versions

    [Theory]
    [InlineData("v0.2.0", "0.1.0", true)]
    [InlineData("0.2.0", "0.1.0", true)]
    [InlineData("0.1.0", "0.1.0", false)]
    [InlineData("v0.1.0", "0.1.0", false)]
    [InlineData("0.1.0", "0.2.0", false)]
    [InlineData("1.0.0", "0.9.9", true)]
    // Comparaison numérique, pas alphabétique : 0.10.0 est après 0.9.0.
    [InlineData("0.10.0", "0.9.0", true)]
    [InlineData("0.9.0", "0.10.0", false)]
    [InlineData("1.2.3+build.7", "1.2.3", false)]
    [InlineData("1.2.4", "1.2.3+build.7", true)]
    [InlineData("", "1.0.0", false)]
    public void EstPlusRecente_CompareCorrectement(string candidate, string courante, bool attendu)
    {
        Assert.Equal(attendu, UpdateService.EstPlusRecente(candidate, courante));
    }

    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2.3+build", "1.2.3")]
    [InlineData("1.2.3-beta.1", "1.2.3")]
    [InlineData("  v0.1.0  ", "0.1.0")]
    public void Nettoyer_ExtraitLeNoyauNumerique(string entree, string attendu)
    {
        Assert.Equal(attendu, UpdateService.Nettoyer(entree));
    }

    [Fact]
    public void Nettoyer_LaisseIntactUnTexteNonNumerique()
    {
        Assert.Equal("release-finale", UpdateService.Nettoyer("release-finale"));
    }

    #endregion

    #region Vérification

    [Fact]
    public async Task VerifierAsync_VersionPlusRecente_RetourneLesInformations()
    {
        var service = Creer(ReleaseJson("v0.2.0"));

        var disponible = await service.VerifierAsync();

        Assert.NotNull(disponible);
        Assert.Equal("0.2.0", disponible!.Version);
        Assert.Equal("v0.2.0", disponible.NumeroTag);
        Assert.Equal("FatouraDZ 0.2.0", disponible.Titre);
        Assert.Equal("https://github.com/amuza2/FatouraDZ/releases/tag/v0.2.0", disponible.PageHtml);
        Assert.Equal("- Corrections", disponible.Notes);
        Assert.Equal(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), disponible.PublieeLe);
    }

    [Fact]
    public async Task VerifierAsync_VersionIdentique_RetourneNull()
    {
        var service = Creer(ReleaseJson($"v{AppInfo.Version}"));

        Assert.Null(await service.VerifierAsync());
    }

    [Fact]
    public async Task VerifierAsync_VersionPlusAncienne_RetourneNull()
    {
        var service = Creer(ReleaseJson("v0.0.1"));

        Assert.Null(await service.VerifierAsync());
    }

    [Fact]
    public async Task VerifierAsync_Preversion_EstIgnoree()
    {
        var service = Creer(ReleaseJson("v9.9.9", prerelease: true));

        Assert.Null(await service.VerifierAsync());
    }

    [Fact]
    public async Task VerifierAsync_Brouillon_EstIgnore()
    {
        var service = Creer(ReleaseJson("v9.9.9", draft: true));

        Assert.Null(await service.VerifierAsync());
    }

    [Fact]
    public async Task VerifierAsync_HorsLigne_RetourneNull()
    {
        var service = Creer(null);

        Assert.Null(await service.VerifierAsync());
    }

    [Fact]
    public async Task VerifierAsync_JsonIllisible_RetourneNull()
    {
        var service = Creer("pas du json du tout");

        Assert.Null(await service.VerifierAsync());
    }

    [Fact]
    public async Task VerifierAsync_SansNumeroDeVersion_RetourneNull()
    {
        var service = Creer("""{ "name": "FatouraDZ", "html_url": "https://example.invalid" }""");

        Assert.Null(await service.VerifierAsync());
    }

    [Fact]
    public async Task VerifierAsync_SansPageHtml_UtiliseLaPageDesReleases()
    {
        var service = Creer("""{ "tag_name": "v0.2.0" }""");

        var disponible = await service.VerifierAsync();

        Assert.NotNull(disponible);
        Assert.Equal(AppInfo.PageReleases, disponible!.PageHtml);
    }

    #endregion
}
