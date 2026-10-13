using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Administration;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers;

/// <summary>Administration des comptes : réservé au rôle ADMIN. L'administrateur agissant est lu dans le jeton.</summary>
[Authorize(Roles = RolesApp.Admin)]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IUserAdministrationService _service;

    public AdminController(IUserAdministrationService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard() => Ok(await _service.GetDashboardAsync());

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles() => Ok(await _service.GetRolesAsync());

    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers([FromQuery] UserSearchCriteria criteria) => Ok(await _service.SearchUsersAsync(criteria));

    // ── Demandes d'accès ──

    [HttpPost("users/{matricule}/approve")]
    public async Task<IActionResult> Approve(string matricule, [FromBody] ApproveAccessRequest request) =>
        Ok(await _service.ApproveAsync(matricule, request));

    [HttpPost("users/{matricule}/reject")]
    public async Task<IActionResult> Reject(string matricule, [FromBody] RejectAccessRequest request) =>
        Ok(await _service.RejectAsync(matricule, request));

    [HttpPost("users/{matricule}/resend-invitation")]
    public async Task<IActionResult> ResendInvitation(string matricule) => Ok(await _service.ResendInvitationAsync(matricule));

    // ── Comptes ──

    [HttpPut("users/{matricule}/role")]
    public async Task<IActionResult> ChangeRole(string matricule, [FromBody] ChangeUserRoleRequest request) =>
        Ok(await _service.ChangeRoleAsync(matricule, request.Role));

    [HttpPut("users/{matricule}/disabled")]
    public async Task<IActionResult> SetDisabled(string matricule, [FromBody] ChangeUserDisabledRequest request) =>
        Ok(await _service.SetDisabledAsync(matricule, request.IsDisabled));

    [HttpPost("users/{matricule}/unlock")]
    public async Task<IActionResult> Unlock(string matricule) => Ok(await _service.UnlockAsync(matricule));

    [HttpPost("users/{matricule}/revoke-sessions")]
    public async Task<IActionResult> RevokeSessions(string matricule) => Ok(await _service.RevokeSessionsAsync(matricule));

    // ── Rôles pré-attribués ──

    [HttpGet("preassigned-roles")]
    public async Task<IActionResult> GetPreassignedRoles() => Ok(await _service.GetPreassignedRolesAsync());

    [HttpPut("preassigned-roles/{matricule}")]
    public async Task<IActionResult> SetPreassignedRole(string matricule, [FromBody] SetPreassignedRoleRequest request) =>
        Ok(await _service.SetPreassignedRoleAsync(matricule, request.Role));

    [HttpDelete("preassigned-roles/{matricule}")]
    public async Task<IActionResult> DeletePreassignedRole(string matricule)
    {
        await _service.DeletePreassignedRoleAsync(matricule);
        return NoContent();
    }

    [HttpGet("connection-log")]
    public async Task<IActionResult> GetConnectionLog([FromQuery] ConnectionLogCriteria criteria) => Ok(await _service.GetConnectionLogAsync(criteria));
}
