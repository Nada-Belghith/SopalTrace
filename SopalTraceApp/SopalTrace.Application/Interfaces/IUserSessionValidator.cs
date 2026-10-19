namespace SopalTrace.Application.Interfaces;

/// <summary>
/// Vérifie à chaque requête qu'un jeton d'accès est encore valable : compte actif, tampon de sécurité
/// inchangé et rôle du jeton identique au rôle en base. Un changement de rôle, une désactivation ou un
/// nouveau mot de passe rendent donc les jetons déjà émis inutilisables immédiatement.
/// </summary>
public interface IUserSessionValidator
{
    Task<bool> IsSessionValidAsync(Guid userId, string? securityStamp, string? role);

    /// <summary>À appeler après toute modification du compte pour qu'elle s'applique immédiatement.</summary>
    void Invalidate(Guid userId);
}
