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
}