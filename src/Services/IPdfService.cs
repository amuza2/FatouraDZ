using System.Threading.Tasks;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public interface IPdfService
{
    Task<string> GenererPdfAsync(Facture facture, Business business, string cheminDestination);
}
