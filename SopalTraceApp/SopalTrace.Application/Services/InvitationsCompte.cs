using Microsoft.Extensions.Logging;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Settings;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

/// <summary>Invitation à activer un compte (accès accordé) et envoi des e-mails liés aux comptes.</summary>
internal static class InvitationsCompte
{
    /// <summary>
    /// Nouveau lien d'activation à usage unique (l'ancien devient invalide) et passage au statut INVITED.
    /// Renvoie le jeton en clair : il n'existe qu'en mémoire, le temps de l'envoyer par e-mail.
    /// </summary>
    public static string Preparer(UtilisateursApp user, AuthSettings settings, DateTime maintenant)
    {
        var jeton = JetonsSecurite.Generer();
        user.ActivationTokenHash = JetonsSecurite.Empreinte(jeton);
        user.ActivationTokenExpiresAt = maintenant.AddHours(settings.InvitationHours);
        user.PasserA(AccountStates.Invited);
        return jeton;
    }

    public static Task<bool> EnvoyerAsync(UtilisateursApp user, string jeton, AuthSettings settings, IEmailService email,
        IJournalConnexionRepository journal, ILogger logger)
        => EssayerAsync(() => email.SendAccountInvitationEmailAsync(user.Email, user.NomComplet, settings.ActivationLink(jeton),
                user.ActivationTokenExpiresAt!.Value),
            user.Matricule, "lien d'activation", journal, logger);

    /// <summary>
    /// Un e-mail qui ne part pas (serveur SMTP indisponible) ne bloque jamais l'opération : l'état du compte
    /// est enregistré, l'échec est tracé et l'administrateur peut renvoyer l'invitation depuis l'écran.
    /// </summary>
    public static async Task<bool> EssayerAsync(Func<Task> envoi, string matricule, string objet,
        IJournalConnexionRepository journal, ILogger logger)
    {
        try
        {
            await envoi();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "E-mail « {Objet} » non envoyé pour le matricule {Matricule}", objet, matricule);
            await journal.LogActionAsync(matricule, "EMAIL_NON_ENVOYE", TexteReference.Tronquer($"E-mail « {objet} » non envoyé", 255));
            return false;
        }
    }
}
