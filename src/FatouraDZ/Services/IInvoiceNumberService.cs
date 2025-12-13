using System.Threading.Tasks;

namespace FatouraDZ.Services;

public interface IInvoiceNumberService
{
    Task<string> GenererProchainNumeroAsync();
}
