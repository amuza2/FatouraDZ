using System.Diagnostics;

namespace FatouraDZ.Services;

/// <summary>
/// Ouverture de liens et de dossiers dans l'environnement de bureau de l'utilisateur.
/// </summary>
public static class LiensExternes
{
    /// <summary>
    /// Ouvre une URL dans le navigateur par défaut. Retourne <c>false</c> si
    /// l'environnement n'a pas de gestionnaire de liens (session sans bureau,
    /// conteneur, machine sans navigateur) — jamais d'exception.
    /// </summary>
    public static bool Ouvrir(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
