// IAuthService
using SopalTrace.Application.DTOs.Auth;

namespace SopalTrace.Application.Interfaces;

public interface IAuthService
{
    Task<(AuthResponseDto Response, string RefreshToken)> RegisterAsync(RegisterRequestDto request);
    Task<(AuthResponseDto Response, string RefreshToken)> LoginAsync(LoginRequestDto request);
    Task<(AuthResponseDto Response, string RefreshToken)> RefreshTokenAsync(string expiredJwt, string refreshToken);
    Task ForgotPasswordAsync(ForgotPasswordDto request);
    Task ResetPasswordAsync(ResetPasswordDto request);
    Task RevokeUserTokensAsync(Guid userId, string? matricule = null);
}