using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Exceptions;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SopalTraceDbContext _context;

    public UserRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByEmailAsync(string email) =>
        await _context.UtilisateursApps.AnyAsync(u => u.Email == email);

    public async Task<List<string>> GetEmailsByRoleAsync(string roleApp) =>
        await _context.UtilisateursApps
            .Where(u => u.RoleApp == roleApp && u.AccountStatus == AccountStates.Active)
            .Select(u => u.Email)
            .ToListAsync();

    public async Task<bool> TryAddUserAsync(UtilisateursApp user, PreassignedRole? preattributionConsommee)
    {
        _context.UtilisateursApps.Add(user);
        if (preattributionConsommee != null) _context.PreassignedRoles.Remove(preattributionConsommee);
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Deux demandes simultanées pour le même matricule ou le même e-mail : la seconde n'écrit rien.
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<PreassignedRole?> GetPreassignedRoleAsync(string matricule) =>
        await _context.PreassignedRoles
            .Include(p => p.RoleCodeNavigation)
            .FirstOrDefaultAsync(p => p.Matricule == matricule);

    public async Task<UtilisateursApp?> GetByActivationTokenHashAsync(string empreinte) =>
        await _context.UtilisateursApps.FirstOrDefaultAsync(u => u.ActivationTokenHash == empreinte);

    public async Task<UtilisateursApp?> GetUserByEmailAsync(string email)
    {
        return await _context.UtilisateursApps
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UtilisateursApp?> GetUserByMatriculeAsync(string matricule)
    {
        return await _context.UtilisateursApps
            .FirstOrDefaultAsync(u => u.Matricule == matricule);
    }

    public async Task<UtilisateursApp?> GetByIdAsync(Guid id) =>
        await _context.UtilisateursApps.FirstOrDefaultAsync(u => u.Id == id);

    public async Task UpdateUserAsync(UtilisateursApp user)
    {
        _context.UtilisateursApps.Update(user);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Le statut a changé depuis la lecture (autre administrateur, activation simultanée).
            throw new ConflitConcurrenceException("Ce compte vient d'être modifié par ailleurs : actualisez la liste puis recommencez.");
        }
    }

    public async Task AddRefreshTokenAsync(Guid userId, string token, DateTime expiration)
    {
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UtilisateurId = userId,
            TokenHash = JetonsSecurite.Empreinte(token),
            JwtId = Guid.NewGuid().ToString(),
            DateCreation = DateTime.Now,
            DateExpiration = expiration,
            EstRevoque = false
        });
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        var hash = JetonsSecurite.Empreinte(token);
        return await _context.RefreshTokens
            .Include(rt => rt.Utilisateur)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken refreshToken)
    {
        refreshToken.EstRevoque = true;
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllTokensForUserAsync(Guid userId)
    {
        await _context.RefreshTokens
            .Where(rt => rt.UtilisateurId == userId && rt.EstRevoque != true)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.EstRevoque, true));
    }
}
