using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

// Les tests d'intégration partagent le singleton `AppSettings` (chemin de la base de
// données) : ils doivent donc s'exécuter séquentiellement pour garantir l'isolation
// et éviter que plusieurs tests n'utilisent la même base temporaire en parallèle.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

/// <summary>
/// Redirige la base de données et les journaux vers un dossier temporaire avant
/// l'exécution du moindre test : la base réelle de l'utilisateur ne doit jamais être touchée.
/// </summary>
internal static class TestEnvironment
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var dossier = Path.Combine(Path.GetTempPath(), $"fatouradz_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dossier);
        Environment.SetEnvironmentVariable("FATOURADZ_TEST_DB_DIR", dossier);
    }
}
