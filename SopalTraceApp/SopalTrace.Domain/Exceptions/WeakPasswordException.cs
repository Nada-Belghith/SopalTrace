namespace SopalTrace.Domain.Exceptions;

public class WeakPasswordException : AuthException
{
    public WeakPasswordException() : base("Le mot de passe ne respecte pas les critères de sécurité de Sopal.") { }
}
