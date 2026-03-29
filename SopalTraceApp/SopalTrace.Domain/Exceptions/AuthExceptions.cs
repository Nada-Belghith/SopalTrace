namespace SopalTrace.Domain.Exceptions;

public abstract class AuthException : DomainException
{
    protected AuthException(string message) : base(message) { }
}

public class UserNotFoundInErpException : AuthException
{
    public UserNotFoundInErpException(string matricule)
        : base($"Le matricule {matricule} n'existe pas dans l'ERP Sage X3.") { }
}

public class RoleNotAllowedException : AuthException
{
    public RoleNotAllowedException(string role)
        : base($"Le rôle '{role}' n'est pas autorisé à utiliser cette application.") { }
}

public class UserAlreadyExistsException : AuthException
{
    public UserAlreadyExistsException(string matricule)
        : base($"Un compte existe déjà pour le matricule {matricule}.") { }
}

public class EmailAlreadyUsedException : AuthException
{
    public EmailAlreadyUsedException(string email)
        : base($"Cet e-mail ({email}) est déjà utilisé.") { }
}

public class InvalidCredentialsException : AuthException
{
    public InvalidCredentialsException()
        : base("Matricule ou mot de passe incorrect.") { }
}
