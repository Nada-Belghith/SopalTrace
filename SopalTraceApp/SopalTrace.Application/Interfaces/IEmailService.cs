namespace SopalTrace.Application.Interfaces;

public interface IEmailService
{
    Task SendResetCodeEmailAsync(string toEmail, string code);
    Task EnvoyerAsync(string toEmail, string subject, string body, bool isHtml = false);

    /// <summary>Accès accordé : lien d'activation à usage unique pour définir son mot de passe.</summary>
    Task SendAccountInvitationEmailAsync(string toEmail, string nomComplet, string activationLink, DateTime expiresAt);

    /// <summary>Accusé de réception : la demande d'accès attend la décision d'un administrateur.</summary>
    Task SendAccessRequestReceivedEmailAsync(string toEmail, string nomComplet);

    /// <summary>Demande refusée, avec le motif donné par l'administrateur.</summary>
    Task SendAccessRequestRejectedEmailAsync(string toEmail, string nomComplet, string motif);

    /// <summary>Nouvelle inscription alors qu'un compte actif existe déjà (sans révéler cette existence à l'écran).</summary>
    Task SendAccountAlreadyExistsEmailAsync(string toEmail, string nomComplet, string loginLink);

    /// <summary>Notification d'une demande à traiter. L'e-mail n'est qu'un rappel : la file des demandes est dans l'application.</summary>
    Task SendAccessRequestToAdminEmailAsync(string toEmail, string nomEmploye, string matricule, string intituleMetier, string lienDemandes);
}
