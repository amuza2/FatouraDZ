using System;
using System.Text.RegularExpressions;
using FatouraDZ.Models;

namespace FatouraDZ.Services;

public class ValidationService : IValidationService
{
    public ValidationResult ValiderBusiness(Business business)
    {
        var result = new ValidationResult();

        // Nom complet requis pour Auto-Entrepreneur et Forfait
        if (business.TypeEntreprise != BusinessType.Reel)
        {
            if (string.IsNullOrWhiteSpace(business.NomComplet))
                result.AjouterErreur("Le nom complet est obligatoire");
        }

        // Raison sociale requise pour Reel (société)
        if (business.TypeEntreprise == BusinessType.Reel)
        {
            if (string.IsNullOrWhiteSpace(business.RaisonSociale))
                result.AjouterErreur("La raison sociale est obligatoire pour une société");
            if (string.IsNullOrWhiteSpace(business.CapitalSocial))
                result.AjouterErreur("Le capital social est obligatoire pour une société");
        }

        if (string.IsNullOrWhiteSpace(business.Activite))
            result.AjouterErreur("L'activité est obligatoire");

        if (string.IsNullOrWhiteSpace(business.Adresse))
            result.AjouterErreur("L'adresse est obligatoire");

        if (string.IsNullOrWhiteSpace(business.Ville))
            result.AjouterErreur("La ville est obligatoire");

        if (string.IsNullOrWhiteSpace(business.Wilaya))
            result.AjouterErreur("La wilaya est obligatoire");

        if (string.IsNullOrWhiteSpace(business.Telephone))
            result.AjouterErreur("Le téléphone est obligatoire");
        else if (!EstTelephoneValide(business.Telephone))
            result.AjouterErreur("Le numéro de téléphone est invalide (mobile: 05/06/07XX XX XX XX, fixe: 0XX XX XX XX)");

        // N° Immatriculation requis uniquement pour Auto-Entrepreneur
        if (business.TypeEntreprise == BusinessType.AutoEntrepreneur)
        {
            if (string.IsNullOrWhiteSpace(business.NumeroImmatriculation))
                result.AjouterErreur("Le numéro d'immatriculation est obligatoire");
        }

        // RC requis pour Forfait et Reel
        if (business.TypeEntreprise != BusinessType.AutoEntrepreneur)
        {
            if (string.IsNullOrWhiteSpace(business.RC))
                result.AjouterErreur("Le numéro RC est obligatoire");
        }

        // Champs fiscaux communs à tous les types
        if (string.IsNullOrWhiteSpace(business.NIS))
            result.AjouterErreur("Le numéro NIS est obligatoire");
        else if (!EstNISValide(business.NIS))
            result.AjouterErreur("Le NIS doit contenir 15 chiffres");

        if (string.IsNullOrWhiteSpace(business.NIF))
            result.AjouterErreur("Le numéro NIF est obligatoire");

        if (string.IsNullOrWhiteSpace(business.AI))
            result.AjouterErreur("Le numéro AI est obligatoire");

        if (!string.IsNullOrWhiteSpace(business.Email) && !EstEmailValide(business.Email))
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
            result.AjouterErreur("Le numéro de téléphone du client est invalide (mobile: 05/06/07XX XX XX XX, fixe: 0XX XX XX XX)");

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
        
        // Mobile: 05, 06, 07 suivi de 8 chiffres (10 chiffres au total)
        if (Regex.IsMatch(cleaned, @"^0[567]\d{8}$"))
            return true;
        
        // Fixe: 0 + indicatif wilaya (2 chiffres) + 6 chiffres (9 chiffres au total)
        // Ex: 024 45 65 21 -> 024456521
        if (Regex.IsMatch(cleaned, @"^0[1-4]\d{7}$"))
            return true;
        
        return false;
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
