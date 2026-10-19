using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendResetCodeEmailAsync(string toEmail, string code)
    {
        try
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var portString = _config["EmailSettings:Port"];
            var smtpUsername = _config["EmailSettings:SmtpUsername"];
            var port = string.IsNullOrEmpty(portString) ? 587 : int.Parse(portString);
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var appPassword = _config["EmailSettings:AppPassword"];
            var senderName = _config["EmailSettings:SenderName"];

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
            {
                throw new Exception($"CRITIQUE : Les identifiants SMTP sont vides ! Email = '{senderEmail}', Le mot de passe est-il vide ? = {string.IsNullOrEmpty(appPassword)}");
            }

            using (var client = new SmtpClient())
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = "SopalTrace - Code de récupération de mot de passe";

                message.Body = new TextPart("plain")
                {
                    Text = $"Bonjour,\n\nVoici votre code de sécurité : {code}\n\nCe code expirera dans 15 minutes.\n\nSi vous n'avez pas demandé cette réinitialisation, veuillez ignorer cet e-mail."
                };

                await client.ConnectAsync(smtpServer!, port, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUsername!, appPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de l'envoi de l'email : {ex.Message}");
            throw new Exception($"Erreur lors de l'envoi du code de sécurité. Vérifiez vos paramètres SMTP.", ex);
        }
    }

    public async Task EnvoyerAsync(string toEmail, string subject, string body, bool isHtml = false)
    {
        try
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var portString = _config["EmailSettings:Port"];
            var smtpUsername = _config["EmailSettings:SmtpUsername"];
            var port = string.IsNullOrEmpty(portString) ? 587 : int.Parse(portString);
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var appPassword = _config["EmailSettings:AppPassword"];
            var senderName = _config["EmailSettings:SenderName"];

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
            {
                throw new Exception($"CRITIQUE : Les identifiants SMTP sont vides ! Email = '{senderEmail}'");
            }

            using (var client = new SmtpClient())
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder();
                if (isHtml)
                {
                    builder.HtmlBody = body;
                    // Ajouter une version texte brut pour les filtres anti-spam (Outlook)
                    builder.TextBody = System.Text.RegularExpressions.Regex.Replace(body, "<.*?>", string.Empty);
                }
                else
                {
                    builder.TextBody = body;
                }
                
                message.Body = builder.ToMessageBody();

                await client.ConnectAsync(smtpServer!, port, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUsername!, appPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de l'envoi de l'email à {toEmail} : {ex.Message}");
            // We usually don't throw for alerts to avoid blocking the workflow, but since it's a dedicated service it's up to the caller to catch.
            throw;
        }
    }
    // ───────────────────────────── Comptes ─────────────────────────────
    // Toute valeur insérée dans le HTML est échappée (le nom vient de l'ERP, le motif est saisi par un administrateur).

    public Task SendAccountInvitationEmailAsync(string toEmail, string nomComplet, string activationLink, DateTime expiresAt)
    {
        var corps = $@"
      <p style='color:#475569; font-size:14px; line-height:1.7;'>
        Votre accès à la plateforme <strong>SopalTrace</strong> a été accordé.<br>
        Pour activer votre compte, choisissez votre mot de passe personnel en suivant ce lien :
      </p>
      {Bouton(activationLink, "Définir mon mot de passe")}
      <p style='color:#475569; font-size:13px;'>Ce lien est personnel, utilisable <strong>une seule fois</strong> et valable jusqu'au
        <strong>{Echapper(expiresAt.ToString("dd/MM/yyyy 'à' HH:mm"))}</strong>. Ne le transférez à personne.</p>
      <p style='color:#94a3b8; font-size:12px;'>Lien expiré ? Refaites une demande d'inscription ou contactez l'administrateur.</p>";
        return EnvoyerAsync(toEmail, "SopalTrace — Activez votre compte", Gabarit("Activation de votre compte", nomComplet, corps), isHtml: true);
    }

    public Task SendAccessRequestReceivedEmailAsync(string toEmail, string nomComplet)
    {
        var corps = @"
      <p style='color:#475569; font-size:14px; line-height:1.7;'>
        Votre demande d'accès à <strong>SopalTrace</strong> a bien été reçue. Elle doit être validée par un administrateur,
        qui vous attribuera un rôle.<br>Vous recevrez ensuite un e-mail contenant votre lien d'activation.
      </p>
      <p style='color:#94a3b8; font-size:12px;'>Aucune action n'est nécessaire de votre part pour le moment.</p>";
        return EnvoyerAsync(toEmail, "SopalTrace — Demande d'accès reçue", Gabarit("Demande d'accès reçue", nomComplet, corps), isHtml: true);
    }

    public Task SendAccessRequestRejectedEmailAsync(string toEmail, string nomComplet, string motif)
    {
        var corps = $@"
      <p style='color:#475569; font-size:14px; line-height:1.7;'>Votre demande d'accès à <strong>SopalTrace</strong> n'a pas été acceptée.</p>
      <div style='background:#fef2f2; border-left:4px solid #dc2626; border-radius:8px; padding:14px 18px; margin:20px 0;'>
        <p style='margin:0 0 4px; color:#991b1b; font-size:12px; text-transform:uppercase; letter-spacing:1px;'>Motif</p>
        <p style='margin:0; color:#1e293b; font-size:14px;'>{Echapper(motif)}</p>
      </div>
      <p style='color:#94a3b8; font-size:12px;'>Pour toute question, adressez-vous à votre responsable ou à l'administrateur de l'application.</p>";
        return EnvoyerAsync(toEmail, "SopalTrace — Demande d'accès", Gabarit("Réponse à votre demande d'accès", nomComplet, corps), isHtml: true);
    }

    public Task SendAccountAlreadyExistsEmailAsync(string toEmail, string nomComplet, string loginLink)
    {
        var corps = $@"
      <p style='color:#475569; font-size:14px; line-height:1.7;'>
        Une nouvelle demande d'inscription a été faite avec votre matricule, alors que votre compte <strong>SopalTrace</strong> est déjà actif.
        Connectez-vous normalement ; si vous avez oublié votre mot de passe, utilisez « Mot de passe oublié » sur la page de connexion.
      </p>
      {Bouton(loginLink, "Se connecter")}
      <p style='color:#94a3b8; font-size:12px;'>Si vous n'êtes pas à l'origine de cette demande, prévenez l'administrateur.</p>";
        return EnvoyerAsync(toEmail, "SopalTrace — Votre compte existe déjà", Gabarit("Votre compte existe déjà", nomComplet, corps), isHtml: true);
    }

    public Task SendAccessRequestToAdminEmailAsync(string toEmail, string nomEmploye, string matricule, string intituleMetier, string lienDemandes)
    {
        var corps = $@"
      <p style='color:#475569; font-size:14px; line-height:1.7;'>Une nouvelle demande d'accès attend votre décision :</p>
      <div style='background:#f1f5f9; border-left:4px solid #2563eb; border-radius:8px; padding:14px 18px; margin:20px 0;'>
        <p style='margin:0; color:#1e293b; font-size:15px;'><strong>{Echapper(nomEmploye)}</strong> — matricule {Echapper(matricule)}</p>
        <p style='margin:4px 0 0; color:#64748b; font-size:13px;'>Fonction dans l'ERP : {Echapper(string.IsNullOrWhiteSpace(intituleMetier) ? "non renseignée" : intituleMetier)}</p>
      </div>
      {Bouton(lienDemandes, "Traiter la demande")}
      <p style='color:#94a3b8; font-size:12px;'>Les demandes en attente sont aussi listées en tête de l'écran « Gestion des habilitations ».</p>";
        return EnvoyerAsync(toEmail, $"SopalTrace — Demande d'accès de {nomEmploye}", Gabarit("Nouvelle demande d'accès", "Administrateur", corps), isHtml: true);
    }

    private static string Echapper(string? texte) => System.Net.WebUtility.HtmlEncode(texte ?? string.Empty);

    private static string Bouton(string lien, string libelle) => $@"
      <div style='text-align:center; margin:28px 0;'>
        <a href='{Echapper(lien)}' style='display:inline-block; background-color:#2563eb; color:#ffffff !important; text-decoration:none; padding:14px 32px; border-radius:8px; font-weight:bold; font-size:15px;'>{Echapper(libelle)}</a>
      </div>
      <p style='color:#64748b; font-size:12px; text-align:center; margin:0;'>Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur :</p>
      <p style='text-align:center; margin:6px 0 20px;'><a href='{Echapper(lien)}' style='color:#2563eb; font-size:12px; word-break:break-all;'>{Echapper(lien)}</a></p>";

    private static string Gabarit(string titre, string destinataire, string corps) => $@"<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'></head>
<body style='font-family: Arial, sans-serif; background:#f4f4f4; margin:0; padding:0;'>
  <div style='max-width:600px; margin:40px auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
    <div style='background: linear-gradient(135deg, #1e3a5f 0%, #2563eb 100%); padding:32px 30px; text-align:center;'>
      <h1 style='color:#ffffff; margin:0; font-size:24px; letter-spacing:1px;'>SopalTrace</h1>
      <p style='color:#93c5fd; margin:8px 0 0; font-size:14px;'>{Echapper(titre)}</p>
    </div>
    <div style='padding:32px 30px;'>
      <p style='color:#1e293b; font-size:16px;'>Bonjour <strong>{Echapper(destinataire)}</strong>,</p>
      {corps}
    </div>
    <div style='background:#f8fafc; padding:20px 30px; text-align:center; border-top:1px solid #e2e8f0;'>
      <p style='margin:0; color:#94a3b8; font-size:12px;'>© SOPAL — SopalTrace Plateforme Qualité</p>
    </div>
  </div>
</body>
</html>";
}
