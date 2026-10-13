using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Services.Security;

/// <summary>
/// État du compte gardé 30 secondes en mémoire : une requête par utilisateur et par demi-minute au plus.
/// Les changements faits par l'administrateur appellent <see cref="Invalidate"/> et s'appliquent aussitôt.
/// </summary>
public class UserSessionValidator : IUserSessionValidator
{
    private static readonly TimeSpan DureeCache = TimeSpan.FromSeconds(30);

    private readonly SopalTraceDbContext _context;
    private readonly IMemoryCache _cache;

    public UserSessionValidator(SopalTraceDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    private sealed record EtatCompte(bool Utilisable, Guid SecurityStamp, string? Role);

    public async Task<bool> IsSessionValidAsync(Guid userId, string? securityStamp, string? role)
    {
        if (!Guid.TryParse(securityStamp, out var tampon)) return false;

        var etat = await _cache.GetOrCreateAsync(Cle(userId), async entree =>
        {
            entree.AbsoluteExpirationRelativeToNow = DureeCache;
            return await _context.UtilisateursApps
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new EtatCompte(u.AccountStatus == AccountStates.Active, u.SecurityStamp, u.RoleApp))
                .FirstOrDefaultAsync();
        });

        // Rôle comparé en plus du tampon : un jeton ne peut jamais porter un autre rôle que celui du compte.
        return etat != null && etat.Utilisable && etat.SecurityStamp == tampon && etat.Role == role;
    }

    public void Invalidate(Guid userId) => _cache.Remove(Cle(userId));

    private static string Cle(Guid userId) => $"session:{userId}";
}
