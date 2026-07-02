namespace SopalTrace.Domain.Exceptions;

public class TooManyAttemptsException : AuthException
{
    public TooManyAttemptsException() : base("Trop de tentatives. Veuillez demander un nouveau code.") { }
}
