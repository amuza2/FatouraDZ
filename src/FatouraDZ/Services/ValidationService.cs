using System;
using System.Text.RegularExpressions;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public class ValidationService : IValidationService
{
    public ValidationResult ValiderEntrepreneur(Entrepreneur entrepreneur)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(entrepreneur.NomComplet))
            result.AjouterErreur("Le nom complet est obligatoire");

        if (string.IsNullOrWhiteSpace(entrepreneur.Adresse))
            result.AjouterErreur("L'adresse est obligatoire");

        if (string.IsNullOrWhiteSpace(entrepreneur.Ville))
            result.AjouterErreur("La ville est obligatoire");

        if (string.IsNullOrWhiteSpace(entrepreneur.Wilaya))
            result.AjouterErreur("La wilaya est obligatoire");

        if (string.IsNullOrWhiteSpace(entrepreneur.Telephone))
            result.AjouterErreur("Le téléphone est obligatoire");
        else if (!EstTelephoneValide(entrepreneur.Telephone))
            result.AjouterErreur("Le numéro de téléphone doit commencer par 05 ou 07 (format: 05XX XX XX XX)");

        if (string.IsNullOrWhiteSpace(entrepreneur.RC))
            result.AjouterErreur("Le numéro RC est obligatoire");

        if (string.IsNullOrWhiteSpace(entrepreneur.NIS))
            result.AjouterErreur("Le numéro NIS est obligatoire");
        else if (!EstNISValide(entrepreneur.NIS))
            result.AjouterErreur("Le NIS doit contenir 15 chiffres");

        if (string.IsNullOrWhiteSpace(entrepreneur.NIF))
            result.AjouterErreur("Le numéro NIF est obligatoire");

        if (string.IsNullOrWhiteSpace(entrepreneur.AI))
            result.AjouterErreur("Le numéro AI est obligatoire");

        if (string.IsNullOrWhiteSpace(entrepreneur.NumeroImmatriculation))
            result.AjouterErreur("Le numéro d'immatriculation est obligatoire");

        if (!string.IsNullOrWhiteSpace(entrepreneur.Email) && !EstEmailValide(entrepreneur.Email))
            result.AjouterErreur("Le format de l'email est invalide");

        return result;
    }

    public ValidationResult ValiderFacture(Facture facture)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(facture.ClientNom))
            result.AjouterErreur("Le nom du client est obligatoire");

        if (string.IsNullOrWhiteSpace(facture.ClientAdresse))
            result.AjouterErreur("L'adresse du client est obligatoire");

        if (string.IsNullOrWhiteSpace(facture.ClientTelephone))
            result.AjouterErreur("Le téléphone du client est obligatoire");
        else if (!EstTelephoneValide(facture.ClientTelephone))
            result.AjouterErreur("Le numéro de téléphone du client doit commencer par 05 ou 07");

        if (string.IsNullOrWhiteSpace(facture.ModePaiement))
            result.AjouterErreur("Le mode de paiement est obligatoire");

        if (facture.DateEcheance < facture.DateFacture)
            result.AjouterErreur("La date d'échéance doit être postérieure ou égale à la date de facture");

        if (facture.Lignes == null || facture.Lignes.Count == 0)
            result.AjouterErreur("La facture doit contenir au moins une ligne");

        if (!string.IsNullOrWhiteSpace(facture.ClientEmail) && !EstEmailValide(facture.ClientEmail))
            result.AjouterErreur("Le format de l'email du client est invalide");

        return result;
    }

    public ValidationResult ValiderLigneFacture(LigneFacture ligne)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(ligne.Designation))
            result.AjouterErreur("La désignation est obligatoire");

        if (ligne.Quantite <= 0)
            result.AjouterErreur("La quantité doit être supérieure à 0");

        if (ligne.PrixUnitaire < 0)
            result.AjouterErreur("Le prix unitaire ne peut pas être négatif");

        return result;
    }

    public bool EstTelephoneValide(string telephone)
    {
        if (string.IsNullOrWhiteSpace(telephone))
            return false;

        var cleaned = Regex.Replace(telephone, @"[\s\-\.]", "");
        return Regex.IsMatch(cleaned, @"^0[567]\d{8}$");
    }

    public bool EstNISValide(string nis)
    {
        if (string.IsNullOrWhiteSpace(nis))
            return false;

        var cleaned = Regex.Replace(nis, @"\s", "");
        return Regex.IsMatch(cleaned, @"^\d{15}$");
    }

    private static bool EstEmailValide(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
