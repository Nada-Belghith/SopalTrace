namespace SopalTrace.Application.Settings;

/// <summary>Paramètres de l'authentification lus dans la configuration (section « Auth »).</summary>
public class AuthSettings
{
    /// <summary>Adresse du front, utilisée dans les liens envoyés par e-mail (ex. https://sopaltrace.exemple.tn).</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>Échecs de connexion consécutifs avant verrouillage du compte.</summary>
    public int MaxFailedLogins { get; set; } = 5;

    /// <summary>Durée du verrouillage après trop d'échecs.</summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Validité du lien d'activation envoyé quand l'accès est accordé.</summary>
    public int InvitationHours { get; set; } = 48;

    /// <summary>
    /// Lien d'activation à usage unique. Le jeton est placé après « # » : le navigateur ne l'envoie jamais
    /// au serveur, il n'apparaît donc ni dans les journaux du serveur web ni dans l'en-tête Referer.
    /// </summary>
    public string ActivationLink(string jeton) =>
        $"{Base}/auth/setup-password#token={Uri.EscapeDataString(jeton)}";

    public string LoginLink => $"{Base}/login";

    /// <summary>Écran des habilitations filtré sur les demandes à traiter (e-mail aux administrateurs).</summary>
    public string PendingRequestsLink => $"{Base}/dev/habilitations?status=PENDING_APPROVAL";

    private string Base => FrontendBaseUrl.TrimEnd('/');
}
