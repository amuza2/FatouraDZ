using System;
using System.Reflection;

namespace FatouraDZ.Services;

/// <summary>
/// Identité de l'application : version, dépôt, nom. Source unique partagée par
/// l'interface (écran « À propos », pied de la barre latérale) et par le service
/// de mise à jour, pour qu'aucune de ces vues n'annonce une version différente
/// de celle de l'exécutable réellement installé.
/// </summary>
public static class AppInfo
{
    /// <summary>Dépôt GitHub « propriétaire/nom », utilisé pour les mises à jour et les liens.</summary>
    public const string DepotGitHub = "amuza2/FatouraDZ";

    public const string NomProduit = "FatouraDZ";

    public const string PageReleases = $"https://github.com/{DepotGitHub}/releases/latest";

    public const string PageSignalerUnProbleme = $"https://github.com/{DepotGitHub}/issues/new";

    /// <summary>
    /// Version de l'exécutable (ex. « 0.1.0 »), posée par <c>-p:Version</c> au moment
    /// du build. Jamais codée en dur : une release qui afficherait « 1.0.0 » à côté
    /// d'un artefact nommé « 0.2.0 » est un bug de confiance.
    /// </summary>
    public static string Version { get; } = DeterminerVersion();

    /// <summary>Version préfixée par « v », comme les tags de release.</summary>
    public static string VersionAvecPrefixe => $"v{Version}";

    private static string DeterminerVersion()
    {
        var assembly = typeof(AppInfo).Assembly;

        var informationnelle = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationnelle))
        {
            // Le SDK suffixe la version du commit source ("0.1.0+abcdef") : sans
            // intérêt à l'affichage, et gênant pour comparer deux versions.
            var plus = informationnelle.IndexOf('+');
            return plus >= 0 ? informationnelle[..plus] : informationnelle;
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
