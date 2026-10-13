using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SopalTrace.Application.DTOs.Auth;
using SopalTrace.Application.Interfaces;
using System.Security.Claims;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(PolitiquesDebit.Authentification)]
public class AuthController : ControllerBase
{
    private const string CookieRefresh = "refreshToken";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, [FromServices] IValidator<RegisterRequestDto> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { erreurs = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        await _authService.RegisterAsync(request);

        // Réponse identique dans tous les cas : elle ne confirme ni le matricule, ni l'existence d'un compte.
        return Ok(new { message = "Votre demande a été transmise. Si le matricule et l'e-mail correspondent à votre fiche employé, vous recevrez un e-mail : soit votre lien d'activation, soit la confirmation que votre demande attend la validation d'un administrateur." });
    }

    [HttpPost("setup-password")]
    public async Task<IActionResult> SetupPassword([FromBody] SetupPasswordDto request)
    {
        await _authService.SetupPasswordAsync(request);
        return Ok(new { message = "Mot de passe défini avec succès. Vous pouvez maintenant vous connecter." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, [FromServices] IValidator<LoginRequestDto> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { erreurs = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var (response, refreshToken) = await _authService.LoginAsync(request);
        SetRefreshTokenCookie(refreshToken);
        return Ok(response);
    }

    /// <summary>Le jeton de rafraîchissement est lu dans le cookie HttpOnly ; le corps de la requête n'est pas utilisé.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies[CookieRefresh];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Session expirée. Veuillez vous reconnecter." });

        var (response, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
        SetRefreshTokenCookie(newRefreshToken);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request, [FromServices] IValidator<ForgotPasswordDto> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { erreurs = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        await _authService.ForgotPasswordAsync(request);
        return Ok(new { message = "Si cet email est reconnu, un code de récupération a été envoyé." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request, [FromServices] IValidator<ResetPasswordDto> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { erreurs = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        await _authService.ResetPasswordAsync(request);
        return Ok(new { message = "Mot de passe modifié avec succès." });
    }

    /// <summary>
    /// Accessible sans jeton d'accès valide : la session est retrouvée par le cookie de rafraîchissement,
    /// ce qui permet de se déconnecter proprement même après l'expiration du jeton d'accès.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        Guid? userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
        await _authService.LogoutAsync(Request.Cookies[CookieRefresh], userId, User.FindFirst("matricule")?.Value);

        Response.Cookies.Delete(CookieRefresh);
        return Ok(new { message = "Déconnexion réussie." });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(CookieRefresh, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTimeOffset.Now.AddDays(7),
            Secure = true,
            SameSite = SameSiteMode.Strict
        });
    }
}
