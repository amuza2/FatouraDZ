using Avalonia;
using System;
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

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
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
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
