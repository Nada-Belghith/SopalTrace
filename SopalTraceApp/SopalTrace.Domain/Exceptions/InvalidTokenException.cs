namespace SopalTrace.Domain.Exceptions;

public class InvalidTokenException : AuthException
{
    public InvalidTokenException() : base("Le jeton de sécurité ou le code de récupération est invalide ou expiré.") { }
}
