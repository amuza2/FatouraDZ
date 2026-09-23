using System.Collections.Generic;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public interface IValidationService
{
    ValidationResult ValiderBusiness(Business business);
    ValidationResult ValiderFacture(Facture facture);
    ValidationResult ValiderLigneFacture(LigneFacture ligne);
    bool EstTelephoneValide(string telephone);
    bool EstNISValide(string nis);
}

public class ValidationResult
{
    public bool EstValide { get; set; } = true;
    public List<string> Erreurs { get; set; } = new();

    public void AjouterErreur(string erreur)
    {
        EstValide = false;
        Erreurs.Add(erreur);
    }
}
