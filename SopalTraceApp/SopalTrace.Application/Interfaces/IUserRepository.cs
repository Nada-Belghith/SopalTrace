namespace SopalTrace.Application.Interfaces;

using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

public interface IUserRepository
{
    Task<bool> ExistsByMatriculeAsync(string matricule);
    Task<bool> ExistsByEmailAsync(string email);
    Task CreateUserAsync(string matricule, string nomComplet, string email, string passwordHash, string roleApp, string intituleMetier);
    Task<(string Id, string Hash, string Role, string Nom)> GetUserForLoginAsync(string matricule);

    Task<UtilisateursApp?> GetByIdAsync(Guid id);
    Task<UtilisateursApp?> GetUserByRefreshTokenAsync(string refreshToken);
    Task UpdateUserAsync(UtilisateursApp user);

    // Refresh token helpers
    Task AddRefreshTokenAsync(string userId, string token, string jwtId, DateTime expiration);
    Task<bool> ValidateRefreshTokenAsync(string token, string userId);
    Task RevokeRefreshTokenAsync(string token);
    Task<UtilisateursApp?> GetUserByEmailAsync(string email);
}
