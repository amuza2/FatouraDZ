using System.Threading.Tasks;

namespace FatouraDZ.Services;

public interface IInvoiceNumberService
{
    /// <summary>
    /// Aperçu (sans effet de bord) du prochain numéro de facture pour l'année en cours,
    /// au format FAC-YYYY-NNN. Utilisé pour l'affichage dans le formulaire.
    /// </summary>
    Task<string> GenererProchainNumeroAsync();

    /// <summary>
    /// Réserve de façon atomique le prochain numéro de facture pour l'année en cours et
    /// le retourne au format FAC-YYYY-NNN. À appeler au moment de la sauvegarde afin de
    /// garantir l'unicité (aucun doublon même si plusieurs formulaires sont ouverts).
    /// </summary>
    Task<string> AllouerNumeroFactureAsync();
}
