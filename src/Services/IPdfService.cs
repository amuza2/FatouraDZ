using System.Threading.Tasks;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public interface IPdfService
{
    Task<string> GenererPdfAsync(Facture facture, Business business, string cheminDestination);

    /// <summary>
    /// Génère un aperçu PNG (première page) rendu à partir du même document que le PDF.
    /// </summary>
    Task<byte[]> GenererApercuPngAsync(Facture facture, Business business);
}
