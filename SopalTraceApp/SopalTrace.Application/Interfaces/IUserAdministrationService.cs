using SopalTrace.Application.DTOs.Administration;

namespace SopalTrace.Application.Interfaces;

/// <summary>Gestion des comptes par l'administrateur. L'administrateur agissant est toujours lu dans le jeton.</summary>
public interface IUserAdministrationService
{
    Task<PagedResult<UserAccountDto>> SearchUsersAsync(UserSearchCriteria criteria);
    Task<List<RoleDto>> GetRolesAsync();
    Task<AdminDashboardDto> GetDashboardAsync();
    Task<PagedResult<ConnectionLogEntryDto>> GetConnectionLogAsync(ConnectionLogCriteria criteria);

    // Demandes d'accès
    Task<InvitationResultDto> ApproveAsync(string matricule, ApproveAccessRequest request);
    Task<UserAccountDto> RejectAsync(string matricule, RejectAccessRequest request);
    Task<InvitationResultDto> ResendInvitationAsync(string matricule);

    // Comptes
    Task<UserAccountDto> ChangeRoleAsync(string matricule, string role);
    Task<InvitationResultDto> SetDisabledAsync(string matricule, bool isDisabled);
    Task<UserAccountDto> UnlockAsync(string matricule);
    Task<UserAccountDto> RevokeSessionsAsync(string matricule);

    // Rôles pré-attribués (avant l'inscription de l'employé)
    Task<List<PreassignedRoleDto>> GetPreassignedRolesAsync();
    Task<PreassignedRoleDto> SetPreassignedRoleAsync(string matricule, string role);
    Task DeletePreassignedRoleAsync(string matricule);
}
