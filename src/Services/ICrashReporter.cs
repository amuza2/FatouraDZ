using System;

namespace FatouraDZ.Services;

/// <summary>
/// Rapport de plantage écrit sur le disque, tel qu'il est présenté à l'utilisateur.
/// </summary>
public sealed record RapportCrash(string CheminFichier, string Resume, string Origine, DateTime Date);

/// <summary>
/// Enregistre les plantages non gérés dans un fichier exploitable.
///
/// Aucun serveur de collecte n'est utilisé : le rapport reste chez l'utilisateur,
/// qui décide de le joindre ou non à un ticket. C'est volontaire — une application
/// de facturation ne doit pas envoyer de données à l'extérieur sans action
/// explicite.
/// </summary>
public interface ICrashReporter
{
    /// <summary>Dossier contenant les rapports.</summary>
    string DossierRapports { get; }

    /// <summary>Déclenché après l'écriture d'un rapport (permet à l'interface de prévenir l'utilisateur).</summary>
    event Action<RapportCrash>? RapportEnregistre;

    /// <summary>
    /// Écrit un rapport pour <paramref name="exception"/> et retourne ses
    /// informations, ou <c>null</c> si l'écriture a échoué. N'émet jamais
    /// d'exception : un plantage ne doit pas se transformer en second plantage.
    /// </summary>
    RapportCrash? Enregistrer(Exception? exception, string origine);
}
