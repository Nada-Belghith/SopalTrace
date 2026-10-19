using SopalTrace.Domain.Constants;

namespace SopalTrace.Application.DTOs.Administration;

/// <summary>
/// Statut affiché d'un compte : le statut stocké (<see cref="AccountStates"/>), plus LOCKED pour un compte
/// actif temporairement verrouillé après trop d'échecs de connexion.
/// </summary>
public static class AccountStatuses
{
    public const string PendingApproval = AccountStates.PendingApproval;
    public const string Invited = AccountStates.Invited;
    public const string Active = AccountStates.Active;
    public const string Rejected = AccountStates.Rejected;
    public const string Disabled = AccountStates.Disabled;
    public const string Locked = "LOCKED";

    public static readonly string[] All = { PendingApproval, Invited, Active, Rejected, Disabled, Locked };
}

public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);

/// <summary>
/// Role / RoleLabel sont vides pour une demande sans rôle proposé. ReviewedBy / ReviewedAt / ReviewComment :
/// décision de l'administrateur sur la demande d'accès (motif du refus, commentaire d'approbation).
/// </summary>
public record UserAccountDto(
    string Matricule,
    string FullName,
    string Email,
    string? Role,
    string? RoleLabel,
    string? JobTitle,
    string Status,
    DateTime? CreatedAt,
    DateTime? LastLoginAt,
    DateTime? LockoutEndsAt,
    int ActiveSessions,
    DateTime? InvitationExpiresAt,
    string? ReviewedBy,
    DateTime? ReviewedAt,
    string? ReviewComment);

public record RoleDto(string Code, string Label);

public class UserSearchCriteria
{
    /// <summary>Recherche dans le matricule, le nom et l'e-mail.</summary>
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public record ChangeUserRoleRequest(string Role);

public record ChangeUserDisabledRequest(bool IsDisabled);

/// <summary>Accorde l'accès : rôle attribué et commentaire facultatif (tracé).</summary>
public record ApproveAccessRequest(string Role, string? Comment);

/// <summary>Refuse la demande : le motif est obligatoire, tracé et communiqué à l'employé.</summary>
public record RejectAccessRequest(string Reason);

/// <summary>EmailSent = false : le compte est à jour mais l'e-mail n'est pas parti (à renvoyer depuis l'écran).</summary>
public record InvitationResultDto(UserAccountDto Account, bool EmailSent);

public record PreassignedRoleDto(string Matricule, string FullName, string? Email, string? JobTitle, string Role, string RoleLabel,
    string AssignedBy, DateTime AssignedAt);

public record SetPreassignedRoleRequest(string Role);

public record RoleCountDto(string Role, string RoleLabel, int Users);

public record ConnectionLogEntryDto(DateTime? Date, string Matricule, string? FullName, string Action, string? Details);

public class ConnectionLogCriteria
{
    public string? Matricule { get; set; }
    public string? Action { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>UsersWithOpenSession : utilisateurs ayant au moins un jeton de rafraîchissement valide.</summary>
public record AdminDashboardDto(
    int TotalAccounts,
    int ActiveAccounts,
    int PendingApprovalAccounts,
    int InvitedAccounts,
    int RejectedAccounts,
    int DisabledAccounts,
    int LockedAccounts,
    int UsersWithOpenSession,
    int SuccessfulLogins24h,
    int FailedLogins24h,
    List<RoleCountDto> UsersByRole,
    List<ConnectionLogEntryDto> RecentActivity);
