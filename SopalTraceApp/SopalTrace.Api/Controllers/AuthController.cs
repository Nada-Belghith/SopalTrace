// AuthController
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SopalTrace.Application.DTOs.Auth;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
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

        var (response, refreshToken) = await _authService.RegisterAsync(request);
        if (!string.IsNullOrEmpty(refreshToken))
            SetRefreshTokenCookie(refreshToken);

        return Ok(response);
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

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, [FromServices] IValidator<RefreshTokenRequestDto> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { erreurs = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { error = "Refresh Token manquant dans les cookies." });

        var (response, newRefreshToken) = await _authService.RefreshTokenAsync(request.Token, refreshToken);

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

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(7),
            Secure = true,
            SameSite = SameSiteMode.Strict
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
    
}