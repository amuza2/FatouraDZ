using CommunityToolkit.Mvvm.ComponentModel;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class AProposViewModel : ViewModelBase
{
    public string NomApplication => AppInfo.NomProduit;
    public string Version => AppInfo.Version;
    public string Description => "Application de facturation pour entrepreneurs individuels et auto-entrepreneurs en Algérie, conforme aux exigences légales algériennes.";
    public string Auteur => "FatouraDZ Team";
    public string Annee => "2026";
    public string Technologies => "Avalonia UI • .NET • SQLite • QuestPDF";
}
