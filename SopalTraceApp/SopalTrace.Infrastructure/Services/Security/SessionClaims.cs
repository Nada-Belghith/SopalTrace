namespace SopalTrace.Infrastructure.Services.Security;

/// <summary>Revendications propres à SopalTrace dans le jeton d'accès.</summary>
public static class SessionClaims
{
    /// <summary>Tampon de sécurité du compte au moment de l'émission du jeton.</summary>
    public const string SecurityStamp = "stamp";
}
