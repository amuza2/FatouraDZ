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
        var anneeActuelle = DateTime.Now.Year.ToString();
        var derniereAnnee = await _databaseService.GetConfigurationAsync("derniere_annee_facture");
        var prochainNumeroStr = await _databaseService.GetConfigurationAsync("prochain_numero");

        int prochainNumero;

        if (derniereAnnee != anneeActuelle)
        {
            // Nouvelle année : réinitialiser le compteur
            prochainNumero = 1;
            await _databaseService.SetConfigurationAsync("derniere_annee_facture", anneeActuelle);
        }
        else
        {
            prochainNumero = int.TryParse(prochainNumeroStr, out var num) ? num : 1;
        }

        // Formater le numéro : FAC-YYYY-NNN (sans incrémenter)
        var numeroFacture = $"FAC-{anneeActuelle}-{prochainNumero:D3}";

        return numeroFacture;
    }

    public async Task ConfirmerNumeroFactureAsync()
    {
        var anneeActuelle = DateTime.Now.Year.ToString();
        var prochainNumeroStr = await _databaseService.GetConfigurationAsync("prochain_numero");
        var prochainNumero = int.TryParse(prochainNumeroStr, out var num) ? num : 1;

        // Incrémenter pour la prochaine facture
        await _databaseService.SetConfigurationAsync("prochain_numero", (prochainNumero + 1).ToString());
    }
}
