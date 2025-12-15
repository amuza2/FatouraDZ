using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class HistoriqueFacturesViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly IPdfService _pdfService;

    // Liste des factures
    public ObservableCollection<Facture> Factures { get; } = new();

    // Filtres
    [ObservableProperty]
    private DateTimeOffset? _dateDebut;

    [ObservableProperty]
    private DateTimeOffset? _dateFin;

    [ObservableProperty]
    private int _typeFactureIndex = 0; // 0 = Tous

    [ObservableProperty]
    private int _statutIndex = 0; // 0 = Tous

    [ObservableProperty]
    private string _recherche = string.Empty;

    // Pagination
    [ObservableProperty]
    private int _pageActuelle = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalFactures;

    private const int FacturesParPage = 20;

    // Statistiques
    [ObservableProperty]
    private int _nombreTotalFactures;

    [ObservableProperty]
    private decimal _chiffreAffairesTotal;

    [ObservableProperty]
    private decimal _montantMoyen;

    // États
    [ObservableProperty]
    private bool _estChargement;

    [ObservableProperty]
    private string? _messageInfo;

    [ObservableProperty]
    private Facture? _factureSelectionnee;

    // Tri
    [ObservableProperty]
    private string _colonneTriActuelle = "DateCreation";

    [ObservableProperty]
    private bool _triDescendant = true;

    // Types et statuts pour les filtres
    public string[] TypesFacture { get; } = { "Tous", "Normale", "Avoir", "Annulation" };
    public string[] Statuts { get; } = { "Tous", "En attente", "Payée", "Annulée" };

    // Events
    public event Action<Facture>? DemanderModification;
    public event Action<Facture>? DemanderDuplication;
    public event Action<Facture, Entrepreneur>? DemanderPrevisualisation;
    public event Func<string, Task<IStorageFile?>>? DemanderCheminSauvegarde;
    public event Func<string, string, Task<bool>>? DemanderConfirmation;

    public HistoriqueFacturesViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
        _pdfService = ServiceLocator.PdfService;
    }

    public async Task ChargerDonneesAsync()
    {
        EstChargement = true;
        MessageInfo = null;

        try
        {
            await ChargerStatistiquesAsync();
            await ChargerFacturesAsync();
        }
        finally
        {
            EstChargement = false;
        }
    }

    private async Task ChargerStatistiquesAsync()
    {
        NombreTotalFactures = await _databaseService.GetNombreFacturesAsync();
        ChiffreAffairesTotal = await _databaseService.GetChiffreAffairesTotalAsync();
        MontantMoyen = await _databaseService.GetMontantMoyenAsync();
    }

    private async Task ChargerFacturesAsync()
    {
        TypeFacture? typeFiltre = TypeFactureIndex switch
        {
            1 => TypeFacture.Normale,
            2 => TypeFacture.Avoir,
            3 => TypeFacture.Annulation,
            _ => null
        };

        StatutFacture? statutFiltre = StatutIndex switch
        {
            1 => StatutFacture.EnAttente,
            2 => StatutFacture.Payee,
            3 => StatutFacture.Annulee,
            _ => null
        };

        var toutesFactures = await _databaseService.GetFacturesAsync(
            DateDebut?.DateTime,
            DateFin?.DateTime,
            typeFiltre,
            statutFiltre,
            string.IsNullOrWhiteSpace(Recherche) ? null : Recherche
        );

        // Exclure les factures archivées de l'historique
        toutesFactures = toutesFactures.Where(f => f.Statut != StatutFacture.Archivee).ToList();

        // Tri
        toutesFactures = TrierFactures(toutesFactures);

        // Pagination
        TotalFactures = toutesFactures.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalFactures / FacturesParPage));
        
        if (PageActuelle > TotalPages)
            PageActuelle = TotalPages;

        var facturesPage = toutesFactures
            .Skip((PageActuelle - 1) * FacturesParPage)
            .Take(FacturesParPage)
            .ToList();

        Factures.Clear();
        foreach (var facture in facturesPage)
        {
            Factures.Add(facture);
        }

        if (TotalFactures == 0)
        {
            MessageInfo = "Aucune facture trouvée";
        }
    }

    private System.Collections.Generic.List<Facture> TrierFactures(System.Collections.Generic.List<Facture> factures)
    {
        return ColonneTriActuelle switch
        {
            "NumeroFacture" => TriDescendant 
                ? factures.OrderByDescending(f => f.NumeroFacture).ToList()
                : factures.OrderBy(f => f.NumeroFacture).ToList(),
            "DateFacture" => TriDescendant 
                ? factures.OrderByDescending(f => f.DateFacture).ToList()
                : factures.OrderBy(f => f.DateFacture).ToList(),
            "ClientNom" => TriDescendant 
                ? factures.OrderByDescending(f => f.ClientNom).ToList()
                : factures.OrderBy(f => f.ClientNom).ToList(),
            "MontantTotal" => TriDescendant 
                ? factures.OrderByDescending(f => f.MontantTotal).ToList()
                : factures.OrderBy(f => f.MontantTotal).ToList(),
            "TypeFacture" => TriDescendant 
                ? factures.OrderByDescending(f => f.TypeFacture).ToList()
                : factures.OrderBy(f => f.TypeFacture).ToList(),
            "Statut" => TriDescendant 
                ? factures.OrderByDescending(f => f.Statut).ToList()
                : factures.OrderBy(f => f.Statut).ToList(),
            _ => TriDescendant 
                ? factures.OrderByDescending(f => f.DateCreation).ToList()
                : factures.OrderBy(f => f.DateCreation).ToList()
        };
    }

    [RelayCommand]
    private void TrierPar(string colonne)
    {
        if (ColonneTriActuelle == colonne)
        {
            TriDescendant = !TriDescendant;
        }
        else
        {
            ColonneTriActuelle = colonne;
            TriDescendant = true;
        }
        _ = ChargerFacturesAsync();
    }

    [RelayCommand]
    private async Task RechercherAsync()
    {
        PageActuelle = 1;
        await ChargerFacturesAsync();
    }

    [RelayCommand]
    private async Task ReinitialiserFiltresAsync()
    {
        DateDebut = null;
        DateFin = null;
        TypeFactureIndex = 0;
        StatutIndex = 0;
        Recherche = string.Empty;
        PageActuelle = 1;
        await ChargerFacturesAsync();
    }

    [RelayCommand]
    private async Task PagePrecedenteAsync()
    {
        if (PageActuelle > 1)
        {
            PageActuelle--;
            await ChargerFacturesAsync();
        }
    }

    [RelayCommand]
    private async Task PageSuivanteAsync()
    {
        if (PageActuelle < TotalPages)
        {
            PageActuelle++;
            await ChargerFacturesAsync();
        }
    }

    [RelayCommand]
    private async Task VoirPdfAsync(Facture facture)
    {
        if (facture == null) return;

        // Si un fichier PDF existe, l'ouvrir
        if (!string.IsNullOrEmpty(facture.CheminPDF) && File.Exists(facture.CheminPDF))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = facture.CheminPDF,
                    UseShellExecute = true
                });
                return;
            }
            catch (Exception ex)
            {
                MessageInfo = $"Impossible d'ouvrir le PDF : {ex.Message}";
                return;
            }
        }

        // Sinon, afficher la prévisualisation
        var entrepreneur = await _databaseService.GetEntrepreneurAsync();
        if (entrepreneur != null)
        {
            DemanderPrevisualisation?.Invoke(facture, entrepreneur);
        }
        else
        {
            MessageInfo = "Configuration entrepreneur manquante.";
        }
    }

    [RelayCommand]
    private void Modifier(Facture facture)
    {
        if (facture == null) return;

        if (facture.Statut == StatutFacture.Payee || facture.Statut == StatutFacture.Annulee)
        {
            MessageInfo = "Impossible de modifier une facture payée ou annulée";
            return;
        }

        DemanderModification?.Invoke(facture);
    }

    [RelayCommand]
    private async Task ArchiverAsync(Facture facture)
    {
        if (facture == null) return;

        // Demander confirmation
        bool confirme = true;
        if (DemanderConfirmation != null)
        {
            confirme = await DemanderConfirmation.Invoke(
                "Archiver la facture", 
                $"Êtes-vous sûr de vouloir archiver la facture {facture.NumeroFacture} ?\n\nVous pourrez la retrouver dans la section Archives.");
        }

        if (!confirme) return;

        try
        {
            await _databaseService.UpdateStatutFactureAsync(facture.Id, StatutFacture.Archivee);
            MessageInfo = $"Facture {facture.NumeroFacture} archivée";
            await ChargerDonneesAsync();
        }
        catch (Exception ex)
        {
            MessageInfo = $"Erreur lors de l'archivage : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DupliquerAsync(Facture facture)
    {
        if (facture == null) return;

        try
        {
            var copie = await _databaseService.DupliquerFactureAsync(facture.Id);
            DemanderDuplication?.Invoke(copie);
        }
        catch (Exception ex)
        {
            MessageInfo = $"Erreur lors de la duplication : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExporterPdfAsync(Facture facture)
    {
        if (facture == null) return;

        try
        {
            var entrepreneur = await _databaseService.GetEntrepreneurAsync();
            if (entrepreneur == null)
            {
                MessageInfo = "Configuration entrepreneur non trouvée";
                return;
            }

            // Demander le chemin de sauvegarde
            var nomFichier = $"{facture.NumeroFacture}_{facture.ClientNom.Replace(" ", "_")}.pdf";
            var fichier = DemanderCheminSauvegarde != null ? await DemanderCheminSauvegarde.Invoke(nomFichier) : null;
            if (fichier == null)
            {
                return; // Utilisateur a annulé
            }

            var cheminPdf = fichier.Path.LocalPath;
            await _pdfService.GenererPdfAsync(facture, entrepreneur, cheminPdf);
            
            // Mettre à jour le chemin PDF dans la facture
            facture.CheminPDF = cheminPdf;
            await _databaseService.SaveFactureAsync(facture);
            
            MessageInfo = $"PDF exporté : {Path.GetFileName(cheminPdf)}";
            
            // Ouvrir le PDF
            Process.Start(new ProcessStartInfo
            {
                FileName = cheminPdf,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageInfo = $"Erreur lors de l'export : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task MarquerPayeeAsync(Facture facture)
    {
        if (facture == null) return;
        await ChangerStatutFactureAsync(facture, StatutFacture.Payee, "payée");
    }

    [RelayCommand]
    private async Task MarquerEnAttenteAsync(Facture facture)
    {
        if (facture == null) return;
        await ChangerStatutFactureAsync(facture, StatutFacture.EnAttente, "en attente");
    }

    [RelayCommand]
    private async Task MarquerAnnuleeAsync(Facture facture)
    {
        if (facture == null) return;
        
        // Demander confirmation pour l'annulation
        bool confirme = true;
        if (DemanderConfirmation != null)
        {
            confirme = await DemanderConfirmation.Invoke(
                "Annuler la facture", 
                $"Êtes-vous sûr de vouloir annuler la facture {facture.NumeroFacture} ?\n\nCette action ne peut pas être facilement inversée.");
        }

        if (!confirme) return;
        
        await ChangerStatutFactureAsync(facture, StatutFacture.Annulee, "annulée");
    }

    private async Task ChangerStatutFactureAsync(Facture facture, StatutFacture nouveauStatut, string nomStatut)
    {
        try
        {
            await _databaseService.UpdateStatutFactureAsync(facture.Id, nouveauStatut);
            MessageInfo = $"Facture {facture.NumeroFacture} marquée comme {nomStatut}";
            await ChargerDonneesAsync();
        }
        catch (Exception ex)
        {
            MessageInfo = $"Erreur lors du changement de statut : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ChangerStatutDirectAsync((Facture facture, StatutFacture nouveauStatut) param)
    {
        var (facture, nouveauStatut) = param;
        Console.WriteLine($"[DEBUG] ChangerStatutDirect called: facture={facture?.NumeroFacture}, Id={facture?.Id}, nouveauStatut={nouveauStatut}");
        
        if (facture == null || facture.Id <= 0)
        {
            Console.WriteLine($"[DEBUG] Facture is null or Id <= 0, returning");
            return;
        }

        var nomStatut = nouveauStatut switch
        {
            StatutFacture.EnAttente => "en attente",
            StatutFacture.Payee => "payée",
            StatutFacture.Annulee => "annulée",
            _ => "modifiée"
        };

        try
        {
            Console.WriteLine($"[DEBUG] Calling UpdateStatutFactureAsync with Id={facture.Id}, nouveauStatut={nouveauStatut}");
            await _databaseService.UpdateStatutFactureAsync(facture.Id, nouveauStatut);
            facture.Statut = nouveauStatut; // Update local object
            Console.WriteLine($"[DEBUG] Status updated successfully");
            MessageInfo = $"Facture {facture.NumeroFacture} marquée comme {nomStatut}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error: {ex.Message}");
            MessageInfo = $"Erreur lors du changement de statut : {ex.Message}";
        }
    }

    partial void OnDateDebutChanged(DateTimeOffset? value) => _ = ChargerFacturesAsync();
    partial void OnDateFinChanged(DateTimeOffset? value) => _ = ChargerFacturesAsync();
    partial void OnTypeFactureIndexChanged(int value) => _ = ChargerFacturesAsync();
    partial void OnStatutIndexChanged(int value) => _ = ChargerFacturesAsync();
}
