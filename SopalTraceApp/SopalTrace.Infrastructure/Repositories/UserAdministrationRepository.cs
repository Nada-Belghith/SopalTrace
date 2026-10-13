using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.Administration;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

public class UserAdministrationRepository : IUserAdministrationRepository
{
    private readonly SopalTraceDbContext _context;

    public UserAdministrationRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    /// <summary>Ligne lue en base ; le statut est déduit ensuite (même règle pour la liste et le filtre).</summary>
    private sealed record LigneCompte(string Matricule, string NomComplet, string Email, string? RoleApp, string? RoleLabel, string? IntituleMetier,
        string AccountStatus, DateTime? LockoutEndsAt, DateTime? DateCreation, DateTime? DateDerniereConnexion, int Sessions,
        DateTime? ActivationTokenExpiresAt, string? ReviewedBy, DateTime? ReviewedAt, string? ReviewComment);

    public async Task<PagedResult<UserAccountDto>> SearchUsersAsync(UserSearchCriteria criteria)
    {
        var maintenant = DateTime.Now;
        var requete = _context.UtilisateursApps.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var texte = criteria.Search.Trim();
            requete = requete.Where(u => u.Matricule.Contains(texte) || u.NomComplet.Contains(texte) || u.Email.Contains(texte));
        }
        if (!string.IsNullOrWhiteSpace(criteria.Role))
            requete = requete.Where(u => u.RoleApp == criteria.Role);

        requete = criteria.Status switch
        {
            AccountStatuses.Locked => requete.Where(u => u.AccountStatus == AccountStates.Active && u.LockoutEndsAt > maintenant),
            AccountStatuses.Active => requete.Where(u => u.AccountStatus == AccountStates.Active && (u.LockoutEndsAt == null || u.LockoutEndsAt <= maintenant)),
            { Length: > 0 } statut => requete.Where(u => u.AccountStatus == statut),
            _ => requete
        };

        var total = await requete.CountAsync();
        // Demandes à traiter en tête, les plus anciennes d'abord ; puis les comptes par nom.
        var tri = requete
            .OrderBy(u => u.AccountStatus == AccountStates.PendingApproval ? 0 : 1)
            .ThenBy(u => u.AccountStatus == AccountStates.PendingApproval ? u.DateCreation : null)
            .ThenBy(u => u.NomComplet);
        var lignes = await Projeter(tri, maintenant)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync();

        return new PagedResult<UserAccountDto>(lignes.Select(l => VersDto(l, maintenant)).ToList(), total, criteria.Page, criteria.PageSize);
    }

    public async Task<UserAccountDto?> GetUserAccountAsync(string matricule)
    {
        var maintenant = DateTime.Now;
        var ligne = await Projeter(_context.UtilisateursApps.AsNoTracking().Where(u => u.Matricule == matricule), maintenant).FirstOrDefaultAsync();
        return ligne == null ? null : VersDto(ligne, maintenant);
    }

    public async Task<List<RoleDto>> GetActiveRolesAsync() =>
        await _context.AppRoles.AsNoTracking()
            .Where(r => r.Active)
            .OrderBy(r => r.Label)
            .Select(r => new RoleDto(r.Code, r.Label))
            .ToListAsync();

    public async Task<bool> IsActiveRoleAsync(string roleCode) =>
        await _context.AppRoles.AnyAsync(r => r.Code == roleCode && r.Active);

    public async Task<int> CountUsableAdminsAsync() =>
        await _context.UtilisateursApps.CountAsync(u => u.RoleApp == RolesApp.Admin && u.AccountStatus == AccountStates.Active);

    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        var maintenant = DateTime.Now;
        var hier = maintenant.AddHours(-24);
        var comptes = _context.UtilisateursApps.AsNoTracking();

        var parStatut = await comptes.GroupBy(u => u.AccountStatus)
            .Select(g => new { Statut = g.Key, Nombre = g.Count() })
            .ToDictionaryAsync(g => g.Statut, g => g.Nombre);
        int Compter(string statut) => parStatut.GetValueOrDefault(statut);
        var total = parStatut.Values.Sum();
        var verrouilles = await comptes.CountAsync(u => u.AccountStatus == AccountStates.Active && u.LockoutEndsAt > maintenant);
        var avecSession = await comptes.CountAsync(u => u.RefreshTokens.Any(rt => rt.EstRevoque != true && rt.DateExpiration > maintenant));

        var connexions = await _context.JournalConnexions.AsNoTracking()
            .Where(j => j.DateAction >= hier && (j.Action == "CONNEXION" || j.Action == "CONNEXION_ECHEC"))
            .GroupBy(j => j.Action)
            .Select(g => new { Action = g.Key, Nombre = g.Count() })
            .ToListAsync();

        var parRole = await _context.AppRoles.AsNoTracking()
            .Where(r => r.Active)
            .OrderBy(r => r.Label)
            .Select(r => new RoleCountDto(r.Code, r.Label, r.UtilisateursApps.Count(u => u.AccountStatus == AccountStates.Active)))
            .ToListAsync();

        var activite = await GetConnectionLogAsync(new ConnectionLogCriteria { Page = 1, PageSize = 10 });

        return new AdminDashboardDto(
            total, Compter(AccountStates.Active) - verrouilles, Compter(AccountStates.PendingApproval), Compter(AccountStates.Invited),
            Compter(AccountStates.Rejected), Compter(AccountStates.Disabled), verrouilles, avecSession,
            connexions.FirstOrDefault(c => c.Action == "CONNEXION")?.Nombre ?? 0,
            connexions.FirstOrDefault(c => c.Action == "CONNEXION_ECHEC")?.Nombre ?? 0,
            parRole, activite.Items);
    }

    public async Task<PagedResult<ConnectionLogEntryDto>> GetConnectionLogAsync(ConnectionLogCriteria criteria)
    {
        var requete = _context.JournalConnexions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(criteria.Matricule))
            requete = requete.Where(j => j.Matricule == criteria.Matricule.Trim());
        if (!string.IsNullOrWhiteSpace(criteria.Action))
            requete = requete.Where(j => j.Action == criteria.Action);

        var total = await requete.CountAsync();
        var lignes = await requete
            .OrderByDescending(j => j.DateAction)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(j => new ConnectionLogEntryDto(
                j.DateAction, j.Matricule,
                _context.UtilisateursApps.Where(u => u.Matricule == j.Matricule).Select(u => u.NomComplet).FirstOrDefault(),
                j.Action, j.Details))
            .ToListAsync();

        return new PagedResult<ConnectionLogEntryDto>(lignes, total, criteria.Page, criteria.PageSize);
    }

    // ───────────── Rôles pré-attribués ─────────────

    public async Task<List<PreassignedRoleDto>> GetPreassignedRolesAsync() =>
        await ProjeterPreattributions(_context.PreassignedRoles.AsNoTracking().OrderBy(p => p.MatriculeNavigation.Intusr0))
            .ToListAsync();

    public async Task<PreassignedRoleDto?> GetPreassignedRoleDtoAsync(string matricule) =>
        await ProjeterPreattributions(_context.PreassignedRoles.AsNoTracking().Where(p => p.Matricule == matricule))
            .FirstOrDefaultAsync();

    public async Task SavePreassignedRoleAsync(string matricule, string roleCode, string assignedBy)
    {
        var ligne = await _context.PreassignedRoles.FirstOrDefaultAsync(p => p.Matricule == matricule);
        if (ligne == null)
        {
            ligne = new PreassignedRole { Matricule = matricule };
            _context.PreassignedRoles.Add(ligne);
        }
        ligne.RoleCode = roleCode;
        ligne.AssignedBy = assignedBy;
        ligne.AssignedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeletePreassignedRoleAsync(string matricule) =>
        await _context.PreassignedRoles.Where(p => p.Matricule == matricule).ExecuteDeleteAsync() > 0;

    private static IQueryable<PreassignedRoleDto> ProjeterPreattributions(IQueryable<PreassignedRole> requete) =>
        requete.Select(p => new PreassignedRoleDto(
            p.Matricule, p.MatriculeNavigation.Intusr0, p.MatriculeNavigation.Addeml0, p.MatriculeNavigation.Codmet0,
            p.RoleCode, p.RoleCodeNavigation.Label, p.AssignedBy, p.AssignedAt));

    private static IQueryable<LigneCompte> Projeter(IQueryable<UtilisateursApp> requete, DateTime maintenant) =>
        requete.Select(u => new LigneCompte(
            u.Matricule, u.NomComplet, u.Email, u.RoleApp, u.RoleAppNavigation != null ? u.RoleAppNavigation.Label : null, u.IntituleMetier,
            u.AccountStatus, u.LockoutEndsAt, u.DateCreation, u.DateDerniereConnexion,
            u.RefreshTokens.Count(rt => rt.EstRevoque != true && rt.DateExpiration > maintenant),
            u.ActivationTokenExpiresAt, u.ReviewedBy, u.ReviewedAt, u.ReviewComment));

    private static UserAccountDto VersDto(LigneCompte l, DateTime maintenant)
    {
        // Le verrouillage (temporaire) ne concerne qu'un compte actif ; il s'affiche à la place de « actif ».
        var statut = l.AccountStatus == AccountStates.Active && l.LockoutEndsAt > maintenant ? AccountStatuses.Locked : l.AccountStatus;

        return new UserAccountDto(l.Matricule, l.NomComplet, l.Email, l.RoleApp, l.RoleLabel, l.IntituleMetier, statut,
            l.DateCreation, l.DateDerniereConnexion, statut == AccountStatuses.Locked ? l.LockoutEndsAt : null, l.Sessions,
            l.AccountStatus == AccountStates.Invited ? l.ActivationTokenExpiresAt : null, l.ReviewedBy, l.ReviewedAt, l.ReviewComment);
    }
}
