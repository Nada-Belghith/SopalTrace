using SopalTrace.Application.DTOs.Auth;

namespace SopalTrace.Application.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequestDto request);
    Task SetupPasswordAsync(SetupPasswordDto request);
    Task<(AuthResponseDto Response, string RefreshToken)> LoginAsync(LoginRequestDto request);
    Task<(AuthResponseDto Response, string RefreshToken)> RefreshTokenAsync(string refreshToken);
    Task ForgotPasswordAsync(ForgotPasswordDto request);
    Task ResetPasswordAsync(ResetPasswordDto request);

    /// <summary>Déconnexion : révoque les sessions du compte, retrouvé par le jeton de rafraîchissement (même si le jeton d'accès a expiré) ou par l'utilisateur connecté.</summary>
    Task LogoutAsync(string? refreshToken, Guid? userId, string? matricule);
}
