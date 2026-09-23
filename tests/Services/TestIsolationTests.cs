using System;
using System.IO;
using FatouraDZ.Services;

namespace FatouraDZ.Tests.Services;

/// <summary>
/// Garde-fou : la suite de tests ne doit jamais écrire dans la base de données réelle
/// de l'utilisateur (voir TestEnvironment / AppSettings.TestDatabaseDirVariable).
/// </summary>
public class TestIsolationTests
{
    [Fact]
    public void BaseDeDonnees_EstRedirigeeVersLeDossierTemporaire()
    {
        var chemin = Path.GetFullPath(AppSettings.Instance.DatabasePath);
        var dossierTemporaire = Path.GetFullPath(Path.GetTempPath());

        Assert.StartsWith(dossierTemporaire, chemin, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VariableDeRedirection_EstPositionnee()
    {
        var dossier = Environment.GetEnvironmentVariable(AppSettings.TestDatabaseDirVariable);

        Assert.False(string.IsNullOrWhiteSpace(dossier));
        Assert.True(Directory.Exists(dossier!));
    }
}
