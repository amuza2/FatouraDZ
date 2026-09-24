using System;
using System.Threading;
using System.Threading.Tasks;

namespace FatouraDZ.Services;

/// <summary>
/// Version publiée plus récente que celle en cours d'exécution.
/// </summary>
public sealed record VersionDisponible(
    string Version,
    string NumeroTag,
    string? Titre,
    string PageHtml,
    string? Notes,
    DateTime? PublieeLe);

/// <summary>
/// Vérifie si une version plus récente est publiée.
///
/// L'application se contente de prévenir : elle n'installe rien toute seule. Un
/// remplacement de binaire en cours d'exécution, à travers les formats de
/// distribution supportés (AppImage, .deb, paquet installé par install.sh,
/// installateur Windows), ne peut pas être fait de façon fiable sans vérifier les
/// droits d'écriture et l'intégrité du paquet — et un échec à mi-chemin laisse
/// l'utilisateur sans application. Le téléchargement reste donc explicite.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Retourne la version disponible si elle est plus récente, sinon <c>null</c>.
    /// N'émet jamais d'exception : pas de réseau, pas d'API, pas de mise à jour.
    /// </summary>
    Task<VersionDisponible?> VerifierAsync(CancellationToken cancellationToken = default);
}
