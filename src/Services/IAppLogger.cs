using System;

namespace FatouraDZ.Services;

/// <summary>
/// Abstraction de journalisation de l'application.
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// Vrai si le journal détaillé est actif. Permet d'éviter de construire un
    /// message coûteux (sérialisation, énumération) quand personne ne le lira :
    /// <c>if (logger.EstVerbeux) logger.Debug(...)</c>.
    /// </summary>
    bool EstVerbeux { get; }

    /// <summary>
    /// Détail de diagnostic, écrit uniquement en mode verbeux
    /// (<c>--verbose</c>, voir <see cref="AppOptions"/>). Destiné au support et non à
    /// l'utilisateur : une exécution normale n'y écrit rien.
    /// </summary>
    void Debug(string message, Exception? exception = null);

    void Info(string message);

    void Warning(string message, Exception? exception = null);

    void Error(string message, Exception? exception = null);
}
