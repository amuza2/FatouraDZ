using System;
using System.IO;

namespace FatouraDZ.Services;

/// <summary>
/// Emplacements des données de l'application (journal, rapports de plantage).
///
/// Source unique pour que les tests puissent tout rediriger vers un dossier
/// temporaire via <see cref="AppSettings.TestDatabaseDirVariable"/>, sans que la
/// machine de l'utilisateur reçoive le moindre fichier.
/// </summary>
public static class AppPaths
{
    private const string NomDossier = "FatouraDZ";

    /// <summary>Dossier de données de l'utilisateur (%LOCALAPPDATA%/FatouraDZ, ou le dossier de test).</summary>
    public static string DossierDonnees
    {
        get
        {
            var dossierTest = Environment.GetEnvironmentVariable(AppSettings.TestDatabaseDirVariable);

            return !string.IsNullOrWhiteSpace(dossierTest)
                ? dossierTest
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    NomDossier);
        }
    }

    public static string DossierLogs => Path.Combine(DossierDonnees, "logs");

    public static string DossierRapports => Path.Combine(DossierDonnees, "crashes");

    /// <summary>
    /// Remplace le dossier personnel par « ~ ». Un rapport de plantage finit
    /// souvent en pièce jointe publique : il ne doit pas publier au passage le nom
    /// de la session de l'utilisateur.
    /// </summary>
    public static string Abreger(string chemin)
    {
        if (string.IsNullOrEmpty(chemin))
            return chemin;

        var personnel = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(personnel) && chemin.StartsWith(personnel, StringComparison.Ordinal))
            return "~" + chemin[personnel.Length..];

        return chemin;
    }
}
