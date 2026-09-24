using System;
using System.Collections.Generic;

namespace FatouraDZ.Services;

/// <summary>
/// Options de démarrage, déduites de la ligne de commande.
///
/// L'application est graphique et n'a pas de véritable interface en ligne de
/// commande : les seules options utiles sont celles qui aident à diagnostiquer un
/// problème sur la machine de quelqu'un d'autre (<c>--verbose</c>) ou à répondre à
/// la première question de tout signalement (« quelle version avez-vous ? »).
/// </summary>
public sealed record AppOptions
{
    /// <summary>Exécution normale : journal standard, fenêtre ouverte.</summary>
    public static readonly AppOptions ParDefaut = new();

    /// <summary>
    /// Écrit le détail de diagnostic dans le journal (niveau DEBUG). À demander à un
    /// utilisateur qui signale un problème : le journal est alors beaucoup plus
    /// bavard, et c'est lui qu'on joint au ticket.
    /// </summary>
    public bool Verbeux { get; init; }

    /// <summary>Imprime l'aide sur la sortie standard ; l'application ne démarre pas.</summary>
    public bool AfficherAide { get; init; }

    /// <summary>Imprime la version sur la sortie standard ; l'application ne démarre pas.</summary>
    public bool AfficherVersion { get; init; }

    /// <summary>
    /// Arguments non reconnus, conservés pour être transmis à Avalonia (elle accepte
    /// ses propres options, par exemple <c>--fbdev</c>).
    /// </summary>
    public IReadOnlyList<string> AutresArguments { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Analyse les arguments. Les options inconnues ne sont pas des erreurs : Avalonia
    /// en reçoit d'autres, et refuser de démarrer pour un argument non reconnu ferait
    /// plus de dégâts que de bien.
    /// </summary>
    public static AppOptions Analyser(string[]? arguments)
    {
        if (arguments == null || arguments.Length == 0)
            return ParDefaut;

        var verbeux = false;
        var aide = false;
        var version = false;
        var autres = new List<string>();

        foreach (var argument in arguments)
        {
            // Comparaison insensible à la casse et tolérante au préfixe « / »
            // (convention Windows) : « --verbose », « -v », « /VERBOSE ».
            switch (argument.TrimStart('-', '/').ToLowerInvariant())
            {
                case "v":
                case "verbose":
                    verbeux = true;
                    break;
                case "h":
                case "help":
                case "?":
                    aide = true;
                    break;
                case "version":
                    version = true;
                    break;
                default:
                    if (!string.IsNullOrWhiteSpace(argument))
                        autres.Add(argument);
                    break;
            }
        }

        return new AppOptions
        {
            Verbeux = verbeux,
            AfficherAide = aide,
            AfficherVersion = version,
            AutresArguments = autres
        };
    }

    /// <summary>Texte d'aide affiché par <c>--help</c>.</summary>
    public static string Aide =>
        $"""
        {AppInfo.NomProduit} {AppInfo.Version} — application de facturation.

        Usage : {AppInfo.NomProduit.ToLowerInvariant()} [options]

        Options :
          -v, --verbose   Écrit le détail de diagnostic dans le journal
                          (dossier indiqué par « Paramètres → Données »).
          --version       Affiche la version et quitte.
          -h, --help      Affiche cette aide et quitte.

        Sans option, l'application démarre normalement. Toute autre option est
        transmise à la couche graphique.
        """;
}
