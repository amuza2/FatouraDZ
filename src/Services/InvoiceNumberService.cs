using System;
using System.Threading.Tasks;

namespace FatouraDZ.Services;

public class InvoiceNumberService : IInvoiceNumberService
{
    private readonly IDatabaseService _databaseService;

    public InvoiceNumberService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<string> GenererProchainNumeroAsync()
    {
        var annee = DateTime.Now.Year;
        var numero = await _databaseService.LireProchainNumeroFactureAsync(annee);
        return Formater(annee, numero);
    }

    public async Task<string> AllouerNumeroFactureAsync()
    {
        var annee = DateTime.Now.Year;
        var numero = await _databaseService.ReserverProchainNumeroFactureAsync(annee);
        return Formater(annee, numero);
    }

    // Format : FAC-YYYY-NNN (ex. FAC-2026-001). Au-delà de 999, le numéro s'allonge
    // naturellement (FAC-2026-1234) grâce au formatage D3.
    private static string Formater(int annee, int numero) => $"FAC-{annee}-{numero:D3}";
}
