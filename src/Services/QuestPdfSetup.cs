using QuestPDF.Infrastructure;

namespace FatouraDZ.Services;

/// <summary>
/// Configuration globale de QuestPDF.
/// La licence doit être définie <b>avant</b> toute génération de document, sinon QuestPDF
/// lève une exception — d'où l'appel au démarrage de l'application et non seulement à la
/// construction du service.
/// </summary>
public static class QuestPdfSetup
{
    private static bool _initialise;

    public static void EnsureLicense()
    {
        if (_initialise)
            return;

        QuestPDF.Settings.License = LicenseType.Community;
        _initialise = true;
    }
}
