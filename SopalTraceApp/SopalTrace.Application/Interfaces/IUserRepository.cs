using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email);

    /// <summary>
    /// Ajoute le compte et consomme le rôle pré-attribué éventuel, en une seule transaction.
    /// false si un compte existe déjà pour ce matricule ou cet e-mail (deux inscriptions simultanées) : rien n'est écrit.
    /// </summary>
    Task<bool> TryAddUserAsync(UtilisateursApp user, PreassignedRole? preattributionConsommee);

    Task<UtilisateursApp?> GetByIdAsync(Guid id);
    Task<UtilisateursApp?> GetUserByEmailAsync(string email);
    Task<UtilisateursApp?> GetUserByMatriculeAsync(string matricule);
    /// <summary>Compte dont le lien d'activation a cette empreinte (expiré ou non).</summary>
    Task<UtilisateursApp?> GetByActivationTokenHashAsync(string empreinte);

    /// <summary>Lève ConflitConcurrenceException si le statut a été changé entre-temps (autre administrateur).</summary>
    Task UpdateUserAsync(UtilisateursApp user);

    /// <summary>E-mails des comptes ACTIVE ayant ce rôle.</summary>
    Task<List<string>> GetEmailsByRoleAsync(string roleApp);

    Task<PreassignedRole?> GetPreassignedRoleAsync(string matricule);

    // Jetons de rafraîchissement (seule leur empreinte est stockée)
    Task AddRefreshTokenAsync(Guid userId, string token, DateTime expiration);
    /// <summary>Jeton avec son utilisateur, qu'il soit valide, expiré ou révoqué ; null s'il est inconnu.</summary>
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeAllTokensForUserAsync(Guid userId);
}
