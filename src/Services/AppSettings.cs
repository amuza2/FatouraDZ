using System;
using System.IO;
using System.Text.Json;

namespace FatouraDZ.Services;

public class AppSettings
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FatouraDZ", "settings.json");

    private static readonly string DefaultDbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FatouraDZ", "fatouradz.db");

    public string DatabasePath { get; set; } = DefaultDbPath;

    // Fiscal Settings - TVA
    public decimal TauxTVAStandard { get; set; } = 19m;
    public decimal TauxTVAReduit { get; set; } = 9m;

    // Fiscal Settings - Timbre Fiscal
    // Barème progressif (Loi de Finances 2025, art. 100 du code du timbre).
    // Applicable aux factures réglées en espèces, calculé sur le montant TTC.
    public decimal TimbreSeuilExoneration { get; set; } = 300m; // Montants <= ce seuil : exonérés
    public decimal TimbreSeuil1 { get; set; } = 30000m;         // Borne haute de la tranche à 1 %
    public decimal TimbreTaux1 { get; set; } = 1m;              // Taux (%) jusqu'à TimbreSeuil1
    public decimal TimbreSeuil2 { get; set; } = 100000m;        // Borne haute de la tranche à 1,5 %
    public decimal TimbreTaux2 { get; set; } = 1.5m;            // Taux (%) jusqu'à TimbreSeuil2
    public decimal TimbreTaux3 { get; set; } = 2m;              // Taux (%) au-delà de TimbreSeuil2
    public decimal TimbreMinimum { get; set; } = 5m;            // Minimum de perception (DZD)

    // Fiscal Settings - Retenue à la Source
    public decimal TauxRetenueSourceDefaut { get; set; } = 5m;

    // Invoice Settings
    public string FormatNumeroFacture { get; set; } = "FAC-{ANNEE}-{NUM}";
    public int DelaiPaiementDefaut { get; set; } = 30; // Days

    private static AppSettings? _instance;
    public static AppSettings Instance => _instance ??= Load();

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch
        {
            // If loading fails, use defaults
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Silently fail if we can't save
        }
    }

    public static string GetDefaultDatabasePath() => DefaultDbPath;

    public static void ReloadSettings()
    {
        _instance = Load();
    }
}
