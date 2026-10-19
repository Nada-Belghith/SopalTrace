namespace SopalTrace.Application.DTOs.Auth;

/// <summary>Activation du compte : jeton du lien reçu par e-mail (usage unique) et mot de passe choisi.</summary>
public record SetupPasswordDto(string Token, string NouveauMotDePasse);
