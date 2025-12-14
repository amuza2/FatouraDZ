using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class ArchiveFacturesViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;

    // Liste des factures archivées
    public ObservableCollection<Facture> Factures { get; } = new();

    // Recherche
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

    // États
    [ObservableProperty]
    private bool _estChargement;

    [ObservableProperty]
    private string? _messageInfo;

    // Events
    public event Func<string, string, Task<bool>>? DemanderConfirmation;
    public event Action<Facture, Entrepreneur>? DemanderPrevisualisation;

    public ArchiveFacturesViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
    }

    public async Task ChargerDonneesAsync()
    {
        EstChargement = true;
        MessageInfo = null;

        try
        {
            await ChargerFacturesAsync();
        }
        finally
        {
            EstChargement = false;
        }
    }

    private async Task ChargerFacturesAsync()
    {
        var toutesFactures = await _databaseService.GetFacturesAsync(
            null, null, null, StatutFacture.Archivee,
            string.IsNullOrWhiteSpace(Recherche) ? null : Recherche
        );

        // Tri par date d'archivage (modification) décroissante
        toutesFactures = toutesFactures
            .OrderByDescending(f => f.DateModification ?? f.DateCreation)
            .ToList();

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
            MessageInfo = "Aucune facture archivée";
        }
    }

    [RelayCommand]
    private async Task RechercherAsync()
    {
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
    private async Task RestaurerAsync(Facture facture)
    {
        if (facture == null) return;

        // Demander confirmation
        bool confirme = true;
        if (DemanderConfirmation != null)
        {
            confirme = await DemanderConfirmation.Invoke(
                "Restaurer la facture", 
                $"Voulez-vous restaurer la facture {facture.NumeroFacture} ?\n\nElle sera remise dans l'historique avec le statut 'En attente'.");
        }

        if (!confirme) return;

        try
        {
            await _databaseService.UpdateStatutFactureAsync(facture.Id, StatutFacture.EnAttente);
            MessageInfo = $"Facture {facture.NumeroFacture} restaurée";
            await ChargerDonneesAsync();
        }
        catch (Exception ex)
        {
            MessageInfo = $"Erreur lors de la restauration : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SupprimerAsync(Facture facture)
    {
        if (facture == null) return;

        // Demander confirmation
        var confirme = DemanderConfirmation != null 
            ? await DemanderConfirmation.Invoke(
                "Supprimer définitivement", 
                $"⚠️ ATTENTION ⚠️\n\nVoulez-vous supprimer définitivement la facture {facture.NumeroFacture} ?\n\nCette action est irréversible !")
            : true;

        if (!confirme) return;

        try
        {
            await _databaseService.DeleteFactureAsync(facture.Id);
            
            // Supprimer le fichier PDF si existant
            if (!string.IsNullOrEmpty(facture.CheminPDF) && File.Exists(facture.CheminPDF))
            {
                try { File.Delete(facture.CheminPDF); } catch { }
            }

            MessageInfo = $"Facture {facture.NumeroFacture} supprimée définitivement";
            await ChargerDonneesAsync();
        }
        catch (Exception ex)
        {
            MessageInfo = $"Erreur lors de la suppression : {ex.Message}";
        }
    }
}
