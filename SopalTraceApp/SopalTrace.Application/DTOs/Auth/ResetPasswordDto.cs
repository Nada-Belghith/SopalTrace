namespace SopalTrace.Application.DTOs.Auth;

public record ResetPasswordDto(
    string Email,
    string Code,
    string NouveauMotDePasse
);