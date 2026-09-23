using System;
using System.IO;
using System.Text;

namespace FatouraDZ.Services;

/// <summary>
/// Journalisation simple vers un fichier texte quotidien dans le dossier de données
/// de l'application (%LOCALAPPDATA%/FatouraDZ/logs).
/// Cette implémentation n'échoue jamais : une erreur d'écriture est ignorée silencieusement
/// afin de ne pas faire planter l'application.
/// </summary>
public sealed class FileLogger : IAppLogger
{
    private static readonly object Verrou = new();
    private readonly string _dossierLogs;

    public FileLogger()
    {
        // Pendant les tests, les journaux vont aussi dans le dossier temporaire.
        var dossierTest = Environment.GetEnvironmentVariable(AppSettings.TestDatabaseDirVariable);

        _dossierLogs = !string.IsNullOrWhiteSpace(dossierTest)
            ? Path.Combine(dossierTest, "logs")
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FatouraDZ",
                "logs");
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

            lock (Verrou)
            {
                Directory.CreateDirectory(_dossierLogs);
                var fichier = Path.Combine(_dossierLogs, $"fatouradz-{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(fichier, ligne.ToString());
            }
        }
        catch
        {
            // La journalisation ne doit jamais faire échouer l'application.
        }
    }
}
