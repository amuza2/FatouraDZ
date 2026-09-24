using System;
using System.IO;
using System.Linq;
using System.Text;

namespace FatouraDZ.Services;

/// <summary>
/// Journalisation simple vers un fichier texte quotidien dans le dossier de données
/// de l'application (%LOCALAPPDATA%/FatouraDZ/logs).
///
/// L'intérêt n'est pas le journal en lui-même mais ce qu'il permet : le rapport de
/// plantage y recopie les dernières lignes, ce qui donne un contexte (que faisait
/// l'application juste avant ?) sans lequel une pile d'appels seule est souvent
/// inexploitable.
///
/// Cette implémentation n'échoue jamais : une erreur d'écriture est ignorée
/// silencieusement afin de ne pas faire planter l'application.
/// </summary>
public sealed class FileLogger : IAppLogger
{
    /// <summary>Nombre de jours de journaux conservés (un fichier par jour).</summary>
    public const int JoursConservesParDefaut = 30;

    /// <summary>
    /// Active le niveau DEBUG pour toute nouvelle instance. Positionné au démarrage
    /// par <c>--verbose</c> (voir <see cref="AppOptions"/>), avant la construction du
    /// conteneur de services : c'est une configuration de processus, fixée une fois.
    /// </summary>
    public static bool VerbeuxParDefaut { get; set; }

    private static readonly object Verrou = new();

    public FileLogger()
    {
        DossierLogs = AppPaths.DossierLogs;
        Verbeux = VerbeuxParDefaut;
    }

    /// <summary>Dossier des journaux (surchargé par les tests).</summary>
    public string DossierLogs { get; init; }

    /// <summary>Journaux conservés ; au-delà, les plus anciens sont supprimés.</summary>
    public int JoursConserves { get; init; } = JoursConservesParDefaut;

    /// <summary>Écrit le détail de diagnostic dans le journal.</summary>
    public bool Verbeux { get; init; }

    /// <inheritdoc />
    public bool EstVerbeux => Verbeux;

    // La purge n'a lieu qu'une fois par session : elle est utile au démarrage, pas
    // à chaque message.
    private bool _purgeEffectuee;

    public void Debug(string message, Exception? exception = null)
    {
        // Le niveau est ici et nulle part ailleurs : les appelants peuvent se
        // contenter d'écrire, le coût est nul quand le mode verbeux est inactif.
        if (!Verbeux)
            return;

        Ecrire("DEBUG", message, exception);
    }

    public void Info(string message) => Ecrire("INFO", message, null);

    public void Warning(string message, Exception? exception = null) => Ecrire("WARN", message, exception);

    public void Error(string message, Exception? exception = null) => Ecrire("ERROR", message, exception);

    private void Ecrire(string niveau, string message, Exception? exception)
    {
        try
        {
            var ligne = new StringBuilder()
                .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Append(" [").Append(niveau).Append("] ")
                .Append(message);

            if (exception != null)
                ligne.Append(Environment.NewLine).Append(exception);

            ligne.Append(Environment.NewLine);

            // Un journal est souvent transmis tel quel : on n'y laisse pas le nom de
            // session de l'utilisateur (les chemins apparaissent dans les messages
            // comme dans les exceptions).
            var texte = AppPaths.AbregerDansLeTexte(ligne.ToString());

            lock (Verrou)
            {
                Directory.CreateDirectory(DossierLogs);
                var fichier = Path.Combine(DossierLogs, $"fatouradz-{DateTime.Now:yyyyMMdd}.log");

                File.AppendAllText(fichier, texte);

                if (!_purgeEffectuee)
                {
                    _purgeEffectuee = true;
                    PurgerAnciensJournaux();
                }
            }
        }
        catch
        {
            // La journalisation ne doit jamais faire échouer l'application.
        }
    }

    /// <summary>
    /// Un fichier par jour et aucune limite : sur plusieurs années le dossier
    /// s'alourdit sans raison. Le nom commence par la date, donc l'ordre
    /// lexicographique décroissant est l'ordre chronologique inverse.
    /// </summary>
    private void PurgerAnciensJournaux()
    {
        try
        {
            var journaux = Directory.GetFiles(DossierLogs, "fatouradz-*.log")
                .OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal)
                .ToList();

            foreach (var ancien in journaux.Skip(JoursConserves))
            {
                try { File.Delete(ancien); } catch { /* un journal non supprimable n'est pas un problème */ }
            }
        }
        catch
        {
            // La purge est un nettoyage, jamais une condition d'échec.
        }
    }
}
