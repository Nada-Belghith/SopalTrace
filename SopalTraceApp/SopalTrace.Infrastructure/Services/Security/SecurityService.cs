using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SopalTrace.Application.Interfaces;
using System.Security.Cryptography;


namespace SopalTrace.Infrastructure.Services.Security;

public class SecurityService : ISecurityService
{
    private readonly string _jwtSecret;

    public SecurityService(IConfiguration config)
    {
        _jwtSecret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("La clé 'Jwt:Secret' n'est pas configurée.");
    }

    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

    public string GenerateJwtToken(string userId, string matricule, string role, Guid securityStamp)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim("matricule", matricule),
            new Claim(ClaimTypes.Role, role),
            new Claim(SessionClaims.SecurityStamp, securityStamp.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "SopalTraceApi",
            audience: "SopalTraceVueJs",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string GenerateRecoveryCode() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
}
