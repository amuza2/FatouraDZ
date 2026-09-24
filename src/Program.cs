using Avalonia;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FatouraDZ;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Les options de démarrage sont lues avant tout le reste : le niveau de
        // journalisation doit être fixé avant que le conteneur de services ne crée
        // le journal, et --version/--help doivent répondre sans ouvrir de fenêtre.
        var options = Services.AppOptions.Analyser(args);

        if (options.AfficherAide)
        {
            Console.WriteLine(Services.AppOptions.Aide);
            return;
        }

        if (options.AfficherVersion)
        {
            Console.WriteLine($"{Services.AppInfo.NomProduit} {Services.AppInfo.Version}");
            return;
        }

        Services.FileLogger.VerbeuxParDefaut = options.Verbeux;

        // La licence QuestPDF doit être définie avant toute génération de document.
        Services.QuestPdfSetup.EnsureLicense();

        // Tout plantage non géré laisse un rapport exploitable sur le disque : sans
        // serveur de collecte, c'est la seule trace que l'utilisateur peut transmettre.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Services.ServiceLocator.CrashReporter.Enregistrer(e.ExceptionObject as Exception, "Exception non gérée (AppDomain)");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            // Volontairement sans dialogue : une tâche non observée n'a pas forcément
            // d'effet visible, mais la trace doit exister.
            Services.ServiceLocator.CrashReporter.Enregistrer(e.Exception, "Exception de tâche non observée");
            e.SetObserved();
        };

        var journal = Services.ServiceLocator.Logger;
        journal.Info($"Démarrage de {Services.AppInfo.NomProduit} {Services.AppInfo.Version}"
            + (options.Verbeux ? " (mode verbeux)" : string.Empty));
        journal.Debug($"Arguments reçus : [{string.Join(", ", args)}]"
            + $", transmis à la couche graphique : [{string.Join(", ", options.AutresArguments)}]");
        // La première question d'un signalement est toujours « quelle version, sur
        // quelle machine ? » : la réponse est dans le journal, pas à demander.
        journal.Debug($"Environnement : {Environment.OSVersion}, .NET {Environment.Version}, "
            + $"{Environment.ProcessorCount} processeurs, 64 bits : {Environment.Is64BitProcess}, "
            + $"culture {System.Globalization.CultureInfo.CurrentCulture.Name}");

        try
        {
            // Les arguments non reconnus seulement : le mode verbeux est déjà traité ici.
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(options.AutresArguments.ToArray());
        }
        catch (Exception ex)
        {
            // Échec au démarrage (affichage, base de données, fichier de paramètres) :
            // le rapport est tout ce qui reste à l'utilisateur pour le signaler.
            var rapport = Services.ServiceLocator.CrashReporter.Enregistrer(ex, "Démarrage");
            Console.Error.WriteLine(rapport is null
                ? $"FatouraDZ n'a pas pu démarrer : {ex}"
                : $"FatouraDZ n'a pas pu démarrer. Rapport : {rapport.CheminFichier}");
            throw;
        }
        finally
        {
            journal.Info($"{Services.AppInfo.NomProduit} terminé.");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
