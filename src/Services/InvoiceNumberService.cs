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

    // Format configurable via les paramètres ({ANNEE} et {NUM}), « FAC-YYYY-NNN » par défaut.
    private static string Formater(int annee, int numero)
    {
        var format = AppSettings.Instance.FormatNumeroFacture;
        if (string.IsNullOrWhiteSpace(format))
            format = "FAC-{ANNEE}-{NUM}";

        return format
            .Replace("{ANNEE}", annee.ToString(), StringComparison.Ordinal)
            .Replace("{NUM}", numero.ToString("D3"), StringComparison.Ordinal);
    }
}
