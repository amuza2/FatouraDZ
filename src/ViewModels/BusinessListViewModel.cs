using System.Collections.ObjectModel;
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

    public ObservableCollection<Business> Businesses { get; } = new();

    public event System.Action<Business>? BusinessSelected;
    public event System.Action? CreateBusinessRequested;

    public BusinessListViewModel()
    {
        _databaseService = ServiceLocator.DatabaseService;
    }

    public async Task ChargerBusinessesAsync()
    {
        EstChargement = true;
        MessageErreur = null;

        try
        {
            var businesses = await _databaseService.GetBusinessesAsync();
            Businesses.Clear();
            foreach (var business in businesses)
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
    private async Task DeleteBusinessAsync(Business business)
    {
        try
        {
            await _databaseService.DeleteBusinessAsync(business.Id);
            Businesses.Remove(business);
        }
        catch (System.Exception ex)
        {
            MessageErreur = $"Erreur lors de la suppression : {ex.Message}";
        }
    }
}
