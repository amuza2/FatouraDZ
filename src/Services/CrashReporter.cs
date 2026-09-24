using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FatouraDZ.Services;

/// <summary>
/// Écrit un rapport de plantage lisible dans le dossier de données, et purge les
/// plus anciens.
///
/// Le rapport est volontairement autonome (version, système, journal récent,
/// exception complète) : un utilisateur qui l'envoie ne doit pas avoir à répondre
/// dix questions avant qu'on puisse reproduire.
/// </summary>
public sealed class CrashReporter : ICrashReporter
{
    public const int NombreMaxRapportsParDefaut = 20;

    private const int LignesJournalIncluses = 40;
    private const string PrefixeFichier = "crash-";

    private readonly IAppLogger _logger;

    public CrashReporter(IAppLogger logger) => _logger = logger;

    /// <summary>Dossier des rapports (surchargé par les tests).</summary>
    public string DossierRapports { get; init; } = AppPaths.DossierRapports;

    /// <summary>Nombre de rapports conservés ; au-delà, les plus anciens sont supprimés.</summary>
    public int NombreMaxRapports { get; init; } = NombreMaxRapportsParDefaut;

    public event Action<RapportCrash>? RapportEnregistre;

    public RapportCrash? Enregistrer(Exception? exception, string origine)
    {
        try
        {
            Directory.CreateDirectory(DossierRapports);

            var maintenant = DateTime.Now;
            var resume = Resume(exception);
            var chemin = CheminUnique(maintenant, origine);

            File.WriteAllText(chemin, ConstruireRapport(exception, origine, maintenant), Encoding.UTF8);

            PurgerAnciensRapports();

            _logger.Error($"Rapport de plantage enregistré ({resume}) : {AppPaths.Abreger(chemin)}");

            var rapport = new RapportCrash(chemin, resume, origine, maintenant);
            RapportEnregistre?.Invoke(rapport);
            return rapport;
        }
        catch (Exception ex)
        {
            // Écrire le rapport est le dernier filet de sécurité : s'il casse, il
            // ne doit surtout pas masquer le plantage d'origine.
            try { _logger.Error("Impossible d'enregistrer le rapport de plantage", ex); } catch { /* rien à faire */ }
            return null;
        }
    }

    /// <summary>Résumé sur une ligne, affichable dans un dialogue.</summary>
    public static string Resume(Exception? exception)
    {
        if (exception == null)
            return "Exception inconnue";

        var message = (exception.Message ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        var texte = $"{exception.GetType().Name} : {message}";

        return texte.Length <= 200 ? texte : texte[..197] + "…";
    }

    private static string NomDeFichierSur(string origine)
    {
        var propre = new StringBuilder();
        foreach (var caractere in origine)
        {
            propre.Append(char.IsLetterOrDigit(caractere) ? char.ToLowerInvariant(caractere) : '-');
        }

        var nom = propre.ToString().Trim('-');
        while (nom.Contains("--", StringComparison.Ordinal))
            nom = nom.Replace("--", "-", StringComparison.Ordinal);

        return string.IsNullOrEmpty(nom) ? "inconnu" : (nom.Length > 40 ? nom[..40] : nom);
    }

    private string CheminUnique(DateTime date, string origine)
    {
        // Les millisecondes suffisent en pratique ; la boucle couvre le cas de deux
        // plantages dans la même milliseconde (thread UI + thread de tâche).
        var baseNom = $"{PrefixeFichier}{date:yyyyMMdd-HHmmss-fff}-{NomDeFichierSur(origine)}";
        var chemin = Path.Combine(DossierRapports, $"{baseNom}.txt");

        for (var suffixe = 2; File.Exists(chemin) && suffixe < 100; suffixe++)
        {
            chemin = Path.Combine(DossierRapports, $"{baseNom}-{suffixe}.txt");
        }

        return chemin;
    }

    private string ConstruireRapport(Exception? exception, string origine, DateTime date)
    {
        var rapport = new StringBuilder();

        rapport.AppendLine("FatouraDZ — rapport de plantage");
        rapport.AppendLine("===============================");
        rapport.AppendLine($"Version           : {AppInfo.Version}");
        rapport.AppendLine($"Origine           : {origine}");
        rapport.AppendLine($"Date              : {date:yyyy-MM-dd HH:mm:ss}");
        rapport.AppendLine($"Système           : {Environment.OSVersion} ({Environment.OSVersion.Platform})");
        rapport.AppendLine($"Runtime           : .NET {Environment.Version}, {Environment.ProcessorCount} processeurs, 64 bits : {Environment.Is64BitProcess}");
        rapport.AppendLine($"Culture           : {System.Globalization.CultureInfo.CurrentCulture.Name}");
        rapport.AppendLine($"Dossier de données: {AppPaths.Abreger(AppPaths.DossierDonnees)}");
        rapport.AppendLine($"Base de données   : {DecrireBaseDeDonnees()}");
        rapport.AppendLine($"Journal           : {DecrireJournal()}");

        rapport.AppendLine();
        rapport.AppendLine("Exception");
        rapport.AppendLine("---------");

        if (exception == null)
        {
            rapport.AppendLine("(aucune exception fournie)");
        }
        else
        {
            // ToString() inclut la chaîne des exceptions internes et les piles :
            // c'est exactement ce qu'il faut pour diagnostiquer.
            rapport.AppendLine(AppPaths.Abreger(exception.ToString()));
        }

        var journal = LireFinDuJournal();
        if (journal.Count > 0)
        {
            rapport.AppendLine();
            rapport.AppendLine($"Dernières lignes du journal ({journal.Count})");
            rapport.AppendLine("-----------------------------");
            foreach (var ligne in journal)
                rapport.AppendLine(ligne);
        }

        return rapport.ToString();
    }

    private static string DecrireBaseDeDonnees()
    {
        try
        {
            var chemin = AppSettings.Instance.DatabasePath;
            if (string.IsNullOrWhiteSpace(chemin))
                return "non configurée";

            if (!File.Exists(chemin))
                return $"{Path.GetFileName(chemin)} (absente) — {AppPaths.Abreger(chemin)}";

            var taille = new FileInfo(chemin).Length / 1024d / 1024d;
            return $"{Path.GetFileName(chemin)} ({taille:F2} Mo) — {AppPaths.Abreger(chemin)}";
        }
        catch (Exception ex)
        {
            return $"(indisponible : {ex.GetType().Name})";
        }
    }

    private static string DecrireJournal()
    {
        try
        {
            var dossier = AppPaths.DossierLogs;
            if (!Directory.Exists(dossier))
                return "aucun";

            var fichiers = Directory.GetFiles(dossier, "fatouradz-*.log");
            return fichiers.Length == 0 ? "aucun" : $"{fichiers.Length} fichier(s) dans {AppPaths.Abreger(dossier)}";
        }
        catch (Exception ex)
        {
            return $"(indisponible : {ex.GetType().Name})";
        }
    }

    private List<string> LireFinDuJournal()
    {
        var lignes = new List<string>();

        try
        {
            var fichier = Path.Combine(AppPaths.DossierLogs, $"fatouradz-{DateTime.Now:yyyyMMdd}.log");
            if (!File.Exists(fichier))
                return lignes;

            // Un fichier de journal peut être volumineux : on ne lit que la fin.
            using var flux = new FileStream(fichier, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var aLire = (int)Math.Min(flux.Length, 64 * 1024);
            flux.Seek(-aLire, SeekOrigin.End);

            using var lecteur = new StreamReader(flux, Encoding.UTF8);
            foreach (var ligne in lecteur.ReadToEnd().Split('\n'))
                lignes.Add(ligne.TrimEnd('\r'));

            lignes = lignes.Where(l => l.Length > 0).ToList();
            if (lignes.Count > LignesJournalIncluses)
                lignes = lignes.Skip(lignes.Count - LignesJournalIncluses).ToList();
        }
        catch
        {
            // Le journal est un bonus : son absence ne doit pas empêcher le rapport.
        }

        return lignes;
    }

    private void PurgerAnciensRapports()
    {
        try
        {
            // Le nom commence par un horodatage : l'ordre lexicographique décroissant
            // est l'ordre chronologique inverse.
            var rapports = Directory.GetFiles(DossierRapports, $"{PrefixeFichier}*.txt")
                .OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal)
                .ToList();

            foreach (var ancien in rapports.Skip(NombreMaxRapports))
            {
                try { File.Delete(ancien); } catch { /* un rapport non supprimable n'est pas un problème */ }
            }
        }
        catch
        {
            // La purge est un nettoyage, jamais une condition de succès.
        }
    }
}
