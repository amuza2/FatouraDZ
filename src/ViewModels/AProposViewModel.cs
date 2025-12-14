using CommunityToolkit.Mvvm.ComponentModel;

namespace FatouraDZ.ViewModels;

public partial class AProposViewModel : ViewModelBase
{
    public string NomApplication => "FatouraDZ";
    public string Version => "1.0.0";
    public string Description => "Application de facturation pour entrepreneurs individuels et auto-entrepreneurs en Algérie, conforme aux exigences légales algériennes.";
    public string Auteur => "FatouraDZ Team";
    public string Annee => "2025";
    public string Technologies => "Avalonia UI • .NET • SQLite • QuestPDF";
}
