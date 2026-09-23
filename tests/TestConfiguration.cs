using Xunit;

// Les tests d'intégration partagent le singleton `AppSettings` (chemin de la base de
// données) : ils doivent donc s'exécuter séquentiellement pour garantir l'isolation
// et éviter que plusieurs tests n'utilisent la même base temporaire en parallèle.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
