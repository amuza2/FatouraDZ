using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FatouraDZ.Models;
using FatouraDZ.Services;

namespace FatouraDZ.ViewModels;

public partial class BusinessListViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private bool _estChargement;

    [ObservableProperty]
    private string? _messageErreur;

    [ObservableProperty]
    private int _filterIndex = 0; // 0 = Active, 1 = Archived

    public ObservableCollection<Business> Businesses { get; } = new();

    public event System.Action<Business>? BusinessSelected;
    public event System.Action? CreateBusinessRequested;

    // Dynamic button text/background based on filter
    public string ArchiveButtonText => FilterIndex == 0 ? "📦 Archiver" : "♻️ Restaurer";
    public string ArchiveButtonBackground => FilterIndex == 0 ? "#FEF3C7" : "#D1FAE5";

    public BusinessListViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
    }

    partial void OnFilterIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ArchiveButtonText));
        OnPropertyChanged(nameof(ArchiveButtonBackground));
        _ = ChargerBusinessesAsync();
    }

    public async Task ChargerBusinessesAsync()
    {
        EstChargement = true;
        MessageErreur = null;

        try
        {
            var allBusinesses = await _databaseService.GetBusinessesAsync();
            var showArchived = FilterIndex == 1;
            var filtered = allBusinesses.Where(b => b.IsArchived == showArchived).ToList();
            
            Businesses.Clear();
            foreach (var business in filtered)
            {
                Businesses.Add(business);
            }
        }
        catch (System.Exception ex)
        {
            MessageErreur = $"Erreur lors du chargement : {ex.Message}";
        }
        finally
        {
            EstChargement = false;
        }
    }

    [RelayCommand]
    private void SelectBusiness(Business business)
    {
        BusinessSelected?.Invoke(business);
    }

    [RelayCommand]
    private void CreateBusiness()
    {
        CreateBusinessRequested?.Invoke();
    }

    [RelayCommand]
    private async Task ToggleArchiveAsync(Business business)
    {
        try
        {
            business.IsArchived = !business.IsArchived;
            await _databaseService.SaveBusinessAsync(business);
            Businesses.Remove(business);
        }
        catch (System.Exception ex)
        {
            MessageErreur = $"Erreur : {ex.Message}";
        }
    }

}
