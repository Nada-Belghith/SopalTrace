namespace SopalTrace.Application.Interfaces;

public interface IEmailService
{
    Task SendResetCodeEmailAsync(string toEmail, string code);
    Task EnvoyerAsync(string toEmail, string subject, string body, bool isHtml = false);
}