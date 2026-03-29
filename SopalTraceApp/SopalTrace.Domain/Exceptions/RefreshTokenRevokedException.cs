namespace SopalTrace.Domain.Exceptions;

public class RefreshTokenRevokedException : AuthException
{
    public RefreshTokenRevokedException() : base("Ce jeton de renouvellement a été révoqué pour des raisons de sécurité.") { }
}
