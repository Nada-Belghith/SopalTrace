using SopalTrace.Application.DTOs.Administration;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces;

/// <summary>Lectures de l'écran d'administration des comptes (listes, compteurs, journal) et rôles pré-attribués.</summary>
public interface IUserAdministrationRepository
{
    Task<PagedResult<UserAccountDto>> SearchUsersAsync(UserSearchCriteria criteria);
    Task<UserAccountDto?> GetUserAccountAsync(string matricule);
    Task<List<RoleDto>> GetActiveRolesAsync();
    Task<bool> IsActiveRoleAsync(string roleCode);

    /// <summary>Administrateurs pouvant se connecter (compte ACTIVE).</summary>
    Task<int> CountUsableAdminsAsync();

    Task<AdminDashboardDto> GetDashboardAsync();
    Task<PagedResult<ConnectionLogEntryDto>> GetConnectionLogAsync(ConnectionLogCriteria criteria);

    Task<List<PreassignedRoleDto>> GetPreassignedRolesAsync();
    Task<PreassignedRoleDto?> GetPreassignedRoleDtoAsync(string matricule);
    /// <summary>Crée ou remplace la pré-attribution du matricule.</summary>
    Task SavePreassignedRoleAsync(string matricule, string roleCode, string assignedBy);
    /// <summary>false si aucune pré-attribution n'existait.</summary>
    Task<bool> DeletePreassignedRoleAsync(string matricule);
}
