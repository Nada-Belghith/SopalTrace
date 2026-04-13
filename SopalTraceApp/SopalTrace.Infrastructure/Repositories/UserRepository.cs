using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities; // <-- LA CORRECTION EST ICI !
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SopalTraceDbContext _context;

    public UserRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByMatriculeAsync(string matricule) =>
        await _context.UtilisateursApps.AnyAsync(u => u.Matricule == matricule);

    public async Task<bool> ExistsByEmailAsync(string email) =>
        await _context.UtilisateursApps.AnyAsync(u => u.Email == email);

    public async Task CreateUserAsync(string matricule, string nomComplet, string email, string passwordHash, string roleApp, string intituleMetier)
    {
        var user = new UtilisateursApp
        {
            Id = Guid.NewGuid(),
            Matricule = matricule,
            NomComplet = nomComplet,
            Email = email,
            MotDePasseHash = passwordHash,
            RoleApp = roleApp,
            IntituleMetier = intituleMetier,
            EstActif = true,
            DateCreation = DateTime.UtcNow
        };
        _context.UtilisateursApps.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task<(string Id, string Hash, string Role, string Nom)> GetUserForLoginAsync(string matricule)
    {
        var user = await _context.UtilisateursApps
            .FirstOrDefaultAsync(u => u.Matricule == matricule);

        if (user == null)
        {
            throw new InvalidOperationException($"Utilisateur avec le matricule '{matricule}' introuvable.");
        }

        return (user.Id.ToString(), user.MotDePasseHash, user.RoleApp, user.NomComplet);
    }

    public async Task<UtilisateursApp?> GetUserByRefreshTokenAsync(string refreshToken)
    {
        return await _context.UtilisateursApps
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken));
    }

    public async Task<UtilisateursApp?> GetUserByEmailAsync(string email)
    {
        return await _context.UtilisateursApps
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UtilisateursApp?> GetByIdAsync(Guid id) =>
        await _context.UtilisateursApps.FirstOrDefaultAsync(u => u.Id == id);

    public async Task UpdateUserAsync(UtilisateursApp user)
    {
        _context.UtilisateursApps.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task AddRefreshTokenAsync(string userId, string token, string jwtId, DateTime expiration)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UtilisateurId = Guid.Parse(userId),
            Token = token,
            JwtId = jwtId,
            DateCreation = DateTime.UtcNow,
            DateExpiration = expiration,
            EstRevoque = false
        };
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token, string userId)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token && rt.UtilisateurId == Guid.Parse(userId));

        if (refreshToken == null)
            return false;

        if (refreshToken.DateExpiration < DateTime.UtcNow)
            return false;

        if (refreshToken.EstRevoque == true)
            return false;

        return true;
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken != null)
        {
            refreshToken.EstRevoque = true;
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllTokensForUserAsync(Guid userId)
    {
        // ExecuteUpdateAsync est excellent pour les perfs ! (EF Core 7+)
        await _context.RefreshTokens
            .Where(rt => rt.UtilisateurId == userId && rt.EstRevoque != true)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.EstRevoque, true));
    }
}