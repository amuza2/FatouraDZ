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
        // Journaliser les exceptions non gérées plutôt que de laisser l'application
        // se fermer silencieusement sans trace exploitable.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Services.ServiceLocator.Logger.Error("Exception non gérée", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Services.ServiceLocator.Logger.Error("Exception de tâche non observée", e.Exception);
            e.SetObserved();
        };

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
