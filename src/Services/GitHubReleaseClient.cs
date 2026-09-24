using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FatouraDZ.Services;

/// <summary>
/// Récupère la dernière release publiée sur GitHub, sous forme de JSON brut.
///
/// Abstrait pour que la logique de version (UpdateService) soit testable sans
/// réseau, et pour que l'absence de réseau ne puisse jamais faire échouer un appel.
/// </summary>
public interface IReleaseGitHubClient
{
    /// <summary>JSON de la dernière release, ou <c>null</c> si indisponible.</summary>
    Task<string?> ObtenirDerniereReleaseJsonAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class GitHubReleaseClient : IReleaseGitHubClient
{
    private const string UrlDerniereVersion = $"https://api.github.com/repos/{AppInfo.DepotGitHub}/releases/latest";

    private readonly IAppLogger _logger;
    private readonly HttpClient _client;

    public GitHubReleaseClient(IAppLogger logger, HttpClient client)
    {
        _logger = logger;
        _client = client;
    }

    /// <summary>
    /// Client HTTP configuré pour l'API GitHub : elle refuse les requêtes sans
    /// User-Agent, et un délai court évite de retarder le démarrage quand le réseau
    /// est lent ou absent.
    /// </summary>
    /// <param name="handler">Transport à utiliser ; <c>null</c> pour celui du système (tests).</param>
    public static HttpClient CreerHttpClient(HttpMessageHandler? handler = null)
    {
        var client = handler is null
            ? new HttpClient()
            : new HttpClient(handler);

        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppInfo.NomProduit}/{AppInfo.Version}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public async Task<string?> ObtenirDerniereReleaseJsonAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var reponse = await _client.GetAsync(UrlDerniereVersion, cancellationToken);

            if (reponse.StatusCode == HttpStatusCode.NotFound)
            {
                // Aucune release publiée : cas normal pour un dépôt jeune, pas une erreur.
                _logger.Info("Aucune release publiée : pas de mise à jour à proposer.");
                return null;
            }

            if (!reponse.IsSuccessStatusCode)
            {
                _logger.Warning($"Vérification des mises à jour : réponse HTTP {(int)reponse.StatusCode}.");
                return null;
            }

            return await reponse.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Délai dépassé ou vérification annulée.
            return null;
        }
        catch (HttpRequestException ex)
        {
            // Hors ligne, proxy, DNS : ne jamais déranger l'utilisateur pour ça.
            _logger.Info($"Vérification des mises à jour impossible : {ex.Message}");
            return null;
        }
    }
}
