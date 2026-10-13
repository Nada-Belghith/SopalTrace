namespace SopalTrace.Application.Interfaces;

public interface ISecurityService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);

    /// <summary>Jeton d'accès ; le tampon de sécurité permet de le révoquer avant son expiration.</summary>
    string GenerateJwtToken(string userId, string matricule, string role, Guid securityStamp);
    string GenerateRefreshToken();

    /// <summary>Code de récupération à 6 chiffres (générateur cryptographique).</summary>
    string GenerateRecoveryCode();
}
