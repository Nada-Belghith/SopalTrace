namespace SopalTrace.Application.Interfaces;

public interface IEmailService
{
    Task SendResetCodeEmailAsync(string toEmail, string code);
}