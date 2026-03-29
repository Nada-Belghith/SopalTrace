namespace SopalTrace.Application.Interfaces;

public interface ISecurityService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string GenerateJwtToken(string userId, string matricule, string role);
    string GenerateRefreshToken();
   
}
