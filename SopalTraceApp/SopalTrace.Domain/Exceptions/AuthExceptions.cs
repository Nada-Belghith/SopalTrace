namespace SopalTrace.Domain.Exceptions;

public abstract class AuthException : DomainException
{
    protected AuthException(string message) : base(message) { }
}

// L'inscription ne lève aucune exception métier : sa réponse est toujours la même (anti-énumération),
// le motif d'un refus est écrit dans le journal des connexions.

public class InvalidCredentialsException : AuthException
{
    public InvalidCredentialsException()
        : base("Matricule ou mot de passe incorrect.") { }
}

public class AccountLockedException : AuthException
{
    public AccountLockedException(int minutesRestantes)
        : base($"Trop d'échecs de connexion : le compte est verrouillé pendant encore {minutesRestantes} min.") { }
}

public class AccountDisabledException : AuthException
{
    public AccountDisabledException()
        : base("Votre compte est désactivé. Contactez l'administrateur.") { }
}
