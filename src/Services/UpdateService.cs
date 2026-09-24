using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FatouraDZ.Services;

/// <summary>
/// Compare la version installée à la dernière release publiée.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private readonly IAppLogger _logger;
    private readonly IReleaseGitHubClient _client;

    public UpdateService(IAppLogger logger, IReleaseGitHubClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<VersionDisponible?> VerifierAsync(CancellationToken cancellationToken = default)
    {
        var json = await _client.ObtenirDerniereReleaseJsonAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var racine = document.RootElement;

            // Un brouillon ou une préversion ne doit jamais être annoncé comme
            // « la » mise à jour à installer.
            if (LireBooleen(racine, "draft") == true || LireBooleen(racine, "prerelease") == true)
            {
                _logger.Info("La dernière release est un brouillon ou une préversion : ignorée.");
                return null;
            }

            var tag = LireChaine(racine, "tag_name");
            if (string.IsNullOrWhiteSpace(tag))
            {
                _logger.Warning("La dernière release n'a pas de numéro de version exploitable.");
                return null;
            }

            if (!EstPlusRecente(tag, AppInfo.Version))
            {
                _logger.Info($"Application à jour (installée : {AppInfo.Version}, publiée : {tag}).");
                return null;
            }

            var version = Nettoyer(tag);
            _logger.Info($"Nouvelle version disponible : {version} (installée : {AppInfo.Version}).");

            return new VersionDisponible(
                version,
                tag,
                LireChaine(racine, "name"),
                LireChaine(racine, "html_url") ?? AppInfo.PageReleases,
                LireChaine(racine, "body"),
                LireDate(racine, "published_at"));
        }
        catch (JsonException ex)
        {
            _logger.Warning("Réponse de l'API GitHub illisible", ex);
            return null;
        }
    }

    /// <summary>
    /// Vrai si <paramref name="candidate"/> est une version strictement plus récente
    /// que <paramref name="courante"/>. Les deux acceptent la forme des tags
    /// (« v1.2.3 », « 1.2.3 », « 1.2.3+commit », « 1.2.3-beta.1 »).
    /// </summary>
    public static bool EstPlusRecente(string? candidate, string? courante)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        if (Version.TryParse(Nettoyer(candidate), out var versionCandidate) &&
            Version.TryParse(Nettoyer(courante), out var versionCourante))
        {
            return versionCandidate > versionCourante;
        }

        // Numérotation non standard : comparaison lexicographique, qui reste
        // préférable à ne rien annoncer du tout — sauf si c'est identique.
        var candidateTexte = Nettoyer(candidate);
        var couranteTexte = Nettoyer(courante);

        return !string.Equals(candidateTexte, couranteTexte, StringComparison.OrdinalIgnoreCase)
            && string.CompareOrdinal(candidateTexte, couranteTexte) > 0;
    }

    /// <summary>« v1.2.3+build » → « 1.2.3 » ; laisse le texte intact s'il n'y a pas de partie numérique.</summary>
    public static string Nettoyer(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return string.Empty;

        var texte = version.Trim();
        if (texte.StartsWith('v') || texte.StartsWith('V'))
            texte = texte[1..];

        // Les métadonnées de build (+) et de préversion (-) ne participent pas à
        // la comparaison numérique.
        var coupe = texte.IndexOfAny(['+', '-']);
        var noyau = coupe >= 0 ? texte[..coupe] : texte;

        return Version.TryParse(noyau, out _) ? noyau : texte;
    }

    private static string? LireChaine(JsonElement racine, string propriete) =>
        racine.TryGetProperty(propriete, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static bool? LireBooleen(JsonElement racine, string propriete) =>
        racine.TryGetProperty(propriete, out var element) &&
        (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
            ? element.GetBoolean()
            : null;

    private static DateTime? LireDate(JsonElement racine, string propriete)
    {
        var texte = LireChaine(racine, propriete);

        return DateTime.TryParse(texte, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date)
            ? date
            : null;
    }
}
