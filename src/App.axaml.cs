using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FatouraDZ.Services;
using FatouraDZ.ViewModels;
using FatouraDZ.Views;

namespace FatouraDZ;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Une exception sur le fil d'interface : rapport écrit, affiché, puis
            // fermeture propre (voir OnUnhandledUiException).
            Dispatcher.UIThread.UnhandledException += OnUnhandledUiException;

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            // Trace utile au support : sans elle, impossible de distinguer « la fenêtre
            // ne s'est jamais ouverte » de « l'application a échoué plus tard ».
            ServiceLocator.Logger.Debug("Interface graphique initialisée, fenêtre principale créée.");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnUnhandledUiException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var rapport = ServiceLocator.CrashReporter.Enregistrer(e.Exception, "Exception UI non gérée");

        // L'application est désormais dans un état inconnu : on ne l'arrête pas
        // brutalement (l'utilisateur doit pouvoir copier le rapport) mais on ne
        // la laisse pas non plus continuer à écrire dans une base peut-être
        // incohérente. L'utilisateur ferme le dialogue, puis l'application se ferme.
        e.Handled = true;
        _ = AfficherRapportEtFermerAsync(rapport, e.Exception);
    }

    private async Task AfficherRapportEtFermerAsync(RapportCrash? rapport, Exception exception)
    {
        try
        {
            var fenetre = new CrashWindow(rapport, "Exception UI non gérée", CrashReporter.Resume(exception));
            var principale = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (principale != null)
                await fenetre.ShowDialog(principale);
            else
                fenetre.Show();
        }
        catch
        {
            // Le dialogue est un confort : son échec ne doit pas empêcher la fermeture.
        }
        finally
        {
            (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}