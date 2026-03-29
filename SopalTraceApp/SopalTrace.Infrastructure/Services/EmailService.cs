using System.Net;
using System.Net.Mail;
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

            using (var client = new SmtpClient(smtpServer, port))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(smtpUsername, appPassword);
                client.EnableSsl = true;

                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(senderEmail!, senderName);
                    mailMessage.Subject = "SopalTrace - Code de récupération de mot de passe";
                    mailMessage.Body = $"Bonjour,\n\nVoici votre code de sécurité : {code}\n\nCe code expirera dans 15 minutes.\n\nSi vous n'avez pas demandé cette réinitialisation, veuillez ignorer cet e-mail.";
                    mailMessage.IsBodyHtml = false;
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError($"Erreur SMTP : {smtpEx.Message}");
            throw new Exception($"Erreur lors de l'envoi du code de sécurité. Vérifiez vos paramètres SMTP.", smtpEx);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur inattendue : {ex.Message}");
            throw;
        }
    }
}