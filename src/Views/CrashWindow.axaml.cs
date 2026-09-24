using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FatouraDZ.Services;

namespace FatouraDZ.Views;

/// <summary>
/// Dialogue affiché après un plantage non géré.
///
/// Il ne remplace pas le plantage : il explique ce qui s'est passé, indique où se
/// trouve le rapport, et propose de le joindre à un ticket. Sans serveur de
/// collecte, c'est le seul moyen pour l'utilisateur de transmettre quelque chose
/// d'exploitable.
/// </summary>
public partial class CrashWindow : Window
{
    private readonly RapportCrash? _rapport;

    public CrashWindow()
    {
        InitializeComponent();
    }
    public CrashWindow(RapportCrash? rapport, string origine, string resume) : this()
    {
        _rapport = rapport;

        TitreText.Text = "FatouraDZ a rencontré une erreur";
        OrigineText.Text = $"{origine} — version {AppInfo.Version}";
        ResumeText.Text = resume;

        if (rapport == null)
        {
            CheminText.Text = "Le rapport n'a pas pu être écrit sur le disque.";
            OuvrirDossierButton.IsVisible = false;
            CopierButton.IsVisible = false;
        }
        else
        {
            CheminText.Text = AppPaths.Abreger(rapport.CheminFichier);
            DetailsBox.Text = LireDetails(rapport.CheminFichier);
        }
    }

    private static string LireDetails(string chemin)
    {
        try
        {
            return File.Exists(chemin) ? File.ReadAllText(chemin) : string.Empty;
        }
        catch (Exception ex)
        {
            return $"Impossible de lire le rapport : {ex.Message}";
        }
    }

    private async void OnCopierClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var texte = string.IsNullOrEmpty(DetailsBox.Text)
                ? ResumeText.Text
                : DetailsBox.Text;

            if (Clipboard != null)
                await Clipboard.SetTextAsync(texte);
        }
        catch
        {
            // Un presse-papiers indisponible (pas de session graphique) n'est pas
            // une raison d'ajouter une erreur à un plantage.
        }
    }

    private void OnOuvrirDossierClick(object? sender, RoutedEventArgs e)
    {
        if (_rapport == null) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.GetDirectoryName(_rapport.CheminFichier) ?? AppPaths.DossierRapports,
                UseShellExecute = true
            });
        }
        catch
        {
            // Idem : échouer ici n'apporte rien à l'utilisateur.
        }
    }

    private void OnSignalerClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var corps = Uri.EscapeDataString(
                $"**Version** : {AppInfo.Version}\n" +
                $"**Origine** : {OrigineText.Text}\n" +
                $"**Erreur** : {ResumeText.Text}\n\n" +
                "<!-- Joignez le rapport de plantage indiqué dans le dialogue (bouton « Copier les détails »). -->\n");

            var titre = Uri.EscapeDataString($"[plantage] {ResumeText.Text}");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"{AppInfo.PageSignalerUnProbleme}?title={titre}&body={corps}",
                UseShellExecute = true
            });
        }
        catch
        {
            // Idem.
        }
    }

    private void OnFermerClick(object? sender, RoutedEventArgs e) => Close();
}
