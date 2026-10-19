using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.Administration;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Settings;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services;

/// <summary>
/// Gestion des comptes par l'administrateur : demandes d'accès (validation ou refus motivé), rôles pré-attribués,
/// rôle, désactivation, déverrouillage, sessions, renvoi de l'invitation.
/// Chaque changement de statut passe par la machine à états (<see cref="AccountStates"/>), ferme les sessions
/// concernées et laisse une trace dans le journal des connexions (qui, quand, quel rôle, quel motif) ;
/// les valeurs avant / après sont en plus écrites dans AuditLog par l'intercepteur d'audit.
/// </summary>
public class UserAdministrationService : IUserAdministrationService
{
    private const int TaillePageMax = 100;
    private const int LongueurMinimaleMotif = 5;
    private const int LongueurMaximaleCommentaire = 500;

    private readonly IUserRepository _userRepository;
    private readonly IUserAdministrationRepository _administrationRepository;
    private readonly IJournalConnexionRepository _journalRepository;
    private readonly IUserSessionValidator _sessionValidator;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;
    private readonly IErpService _erpService;
    private readonly AuthSettings _settings;
    private readonly ILogger<UserAdministrationService> _logger;

    public UserAdministrationService(IUserRepository userRepository, IUserAdministrationRepository administrationRepository,
        IJournalConnexionRepository journalRepository, IUserSessionValidator sessionValidator, ICurrentUserService currentUser,
        IEmailService emailService, IErpService erpService, AuthSettings settings, ILogger<UserAdministrationService> logger)
    {
        _userRepository = userRepository;
        _administrationRepository = administrationRepository;
        _journalRepository = journalRepository;
        _sessionValidator = sessionValidator;
        _currentUser = currentUser;
        _emailService = emailService;
        _erpService = erpService;
        _settings = settings;
        _logger = logger;
    }

    // ───────────────────────────── Lectures ─────────────────────────────

    public Task<PagedResult<UserAccountDto>> SearchUsersAsync(UserSearchCriteria criteria)
    {
        if (!string.IsNullOrEmpty(criteria.Status) && !AccountStatuses.All.Contains(criteria.Status))
            throw new RegleMetierException("Statut de compte inconnu.");
        (criteria.Page, criteria.PageSize) = Pagination(criteria.Page, criteria.PageSize);
        return _administrationRepository.SearchUsersAsync(criteria);
    }

    public Task<List<RoleDto>> GetRolesAsync() => _administrationRepository.GetActiveRolesAsync();

    public Task<AdminDashboardDto> GetDashboardAsync() => _administrationRepository.GetDashboardAsync();

    public Task<PagedResult<ConnectionLogEntryDto>> GetConnectionLogAsync(ConnectionLogCriteria criteria)
    {
        (criteria.Page, criteria.PageSize) = Pagination(criteria.Page, criteria.PageSize);
        return _administrationRepository.GetConnectionLogAsync(criteria);
    }

    // ───────────────────────────── Demandes d'accès ─────────────────────────────

    /// <summary>Accorde l'accès (demande en attente, ou refus reconsidéré) : rôle attribué et lien d'activation envoyé.</summary>
    public async Task<InvitationResultDto> ApproveAsync(string matricule, ApproveAccessRequest request)
    {
        var user = await CompteCibleAsync(matricule);
        // Une demande déjà traitée (accès accordé entre-temps par un autre administrateur) ne se réapprouve pas :
        // la décision prise ne doit pas être écrasée. Le renvoi du lien passe par ResendInvitationAsync.
        if (user.AccountStatus is not (AccountStates.PendingApproval or AccountStates.Rejected))
            throw new RegleMetierException(
                $"Cette demande a déjà été traitée : le compte est « {AccountStates.Libelle(user.AccountStatus)} ».");

        var role = await RoleActifAsync(request.Role);
        var commentaire = Commentaire(request.Comment);
        var ancienStatut = user.AccountStatus;

        user.RoleApp = role;
        var jeton = Decider(user, AccountStates.Invited, commentaire, InvitationsCompte.Preparer);

        await _userRepository.UpdateUserAsync(user);
        await TracerAsync(user, "DEMANDE_APPROUVEE",
            $"Rôle {role}{(ancienStatut == AccountStates.Rejected ? " (refus reconsidéré)" : "")}{(commentaire != null ? $" : {commentaire}" : "")}");

        var envoye = await InvitationsCompte.EnvoyerAsync(user, jeton!, _settings, _emailService, _journalRepository, _logger);
        return new InvitationResultDto(await CompteAsync(matricule), envoye);
    }

    /// <summary>Refuse la demande ; le motif est obligatoire, tracé et communiqué à l'employé.</summary>
    public async Task<UserAccountDto> RejectAsync(string matricule, RejectAccessRequest request)
    {
        var motif = Commentaire(request.Reason);
        if (motif == null || motif.Length < LongueurMinimaleMotif)
            throw new RegleMetierException($"Le motif du refus est obligatoire ({LongueurMinimaleMotif} caractères minimum) : il est communiqué à l'employé.");

        var user = await CompteCibleAsync(matricule);
        Decider(user, AccountStates.Rejected, motif, null);

        await _userRepository.UpdateUserAsync(user);
        await TracerAsync(user, "DEMANDE_REFUSEE", motif);
        await InvitationsCompte.EssayerAsync(() => _emailService.SendAccessRequestRejectedEmailAsync(user.Email, user.NomComplet, motif),
            user.Matricule, "refus de la demande", _journalRepository, _logger);
        return await CompteAsync(matricule);
    }

    /// <summary>Nouveau lien d'activation (lien expiré, perdu, ou e-mail non parti) ; l'ancien lien devient invalide.</summary>
    public async Task<InvitationResultDto> ResendInvitationAsync(string matricule)
    {
        var user = await CompteCibleAsync(matricule);
        if (user.AccountStatus != AccountStates.Invited)
            throw new RegleMetierException(user.AccountStatus == AccountStates.Active
                ? "Ce compte est déjà activé : l'utilisateur peut utiliser « Mot de passe oublié »."
                : $"Aucune invitation à renvoyer : le compte est « {AccountStates.Libelle(user.AccountStatus)} ».");

        var jeton = InvitationsCompte.Preparer(user, _settings, DateTime.Now);
        await _userRepository.UpdateUserAsync(user);
        await TracerAsync(user, "INVITATION_RENVOYEE", "");

        var envoye = await InvitationsCompte.EnvoyerAsync(user, jeton, _settings, _emailService, _journalRepository, _logger);
        return new InvitationResultDto(await CompteAsync(matricule), envoye);
    }

    // ───────────────────────────── Comptes ─────────────────────────────

    /// <summary>Rôle d'un compte auquel l'accès a déjà été accordé ; une demande se traite par Approuver.</summary>
    public async Task<UserAccountDto> ChangeRoleAsync(string matricule, string role)
    {
        var user = await CompteCibleAsync(matricule);
        role = (role ?? "").Trim().ToUpperInvariant();
        if (!AccountStates.RoleObligatoire(user.AccountStatus))
            throw new RegleMetierException("Cette demande n'est pas encore acceptée : choisissez le rôle en l'approuvant.");
        if (user.RoleApp == role) return await CompteAsync(matricule);

        role = await RoleActifAsync(role);
        InterdireSurSoiMeme(user, "modifier votre propre rôle");
        if (user.RoleApp == RolesApp.Admin)
            await GarderUnAdministrateurAsync(user, "retirer le rôle administrateur");

        var ancienRole = user.RoleApp;
        user.RoleApp = role;
        await SessionsUtilisateur.FermerAsync(user, _userRepository, _sessionValidator);
        await TracerAsync(user, "ROLE_MODIFIE", $"{ancienRole} -> {role}");
        return await CompteAsync(matricule);
    }

    /// <summary>
    /// Désactivation (compte actif ou invité) ou réactivation : ACTIVE si le mot de passe est déjà défini,
    /// sinon nouvelle invitation (EmailSent indique si le lien est parti).
    /// </summary>
    public async Task<InvitationResultDto> SetDisabledAsync(string matricule, bool isDisabled)
    {
        var user = await CompteCibleAsync(matricule);
        if ((user.AccountStatus == AccountStates.Disabled) == isDisabled)
            return new InvitationResultDto(await CompteAsync(matricule), false);

        string? jeton = null;
        if (isDisabled)
        {
            InterdireSurSoiMeme(user, "désactiver votre propre compte");
            if (user.RoleApp == RolesApp.Admin)
                await GarderUnAdministrateurAsync(user, "désactiver ce compte administrateur");
            user.PasserA(AccountStates.Disabled);
            user.ActivationTokenHash = null;
            user.ActivationTokenExpiresAt = null;
        }
        else if (user.MotDePasseHash != null)
        {
            user.PasserA(AccountStates.Active);
        }
        else
        {
            jeton = InvitationsCompte.Preparer(user, _settings, DateTime.Now);
        }

        await SessionsUtilisateur.FermerAsync(user, _userRepository, _sessionValidator);
        await TracerAsync(user, isDisabled ? "COMPTE_DESACTIVE" : "COMPTE_REACTIVE",
            jeton != null ? "Mot de passe jamais défini : nouvelle invitation" : "");

        var envoye = jeton != null && await InvitationsCompte.EnvoyerAsync(user, jeton, _settings, _emailService, _journalRepository, _logger);
        return new InvitationResultDto(await CompteAsync(matricule), envoye);
    }

    public async Task<UserAccountDto> UnlockAsync(string matricule)
    {
        var user = await CompteCibleAsync(matricule);
        user.FailedLoginCount = 0;
        user.LockoutEndsAt = null;
        await _userRepository.UpdateUserAsync(user);
        await TracerAsync(user, "COMPTE_DEVERROUILLE", "");
        return await CompteAsync(matricule);
    }

    public async Task<UserAccountDto> RevokeSessionsAsync(string matricule)
    {
        var user = await CompteCibleAsync(matricule);
        await SessionsUtilisateur.FermerAsync(user, _userRepository, _sessionValidator);
        await TracerAsync(user, "SESSIONS_REVOQUEES", "");
        return await CompteAsync(matricule);
    }

    // ───────────────────────────── Rôles pré-attribués ─────────────────────────────

    public Task<List<PreassignedRoleDto>> GetPreassignedRolesAsync() => _administrationRepository.GetPreassignedRolesAsync();

    /// <summary>
    /// Rôle choisi avant l'inscription : quand l'employé fera sa demande (matricule et e-mail de l'ERP),
    /// il recevra directement son lien d'activation.
    /// </summary>
    public async Task<PreassignedRoleDto> SetPreassignedRoleAsync(string matricule, string role)
    {
        matricule = (matricule ?? "").Trim();
        var employe = await _erpService.GetEmployeByMatriculeAsync(matricule);
        if (employe == null || !employe.EstActif)
            throw new RegleMetierException($"Le matricule {matricule} n'existe pas ou n'est pas actif dans l'ERP.");
        if (await _userRepository.GetUserByMatriculeAsync(employe.Matricule) != null)
            throw new RegleMetierException($"Un compte existe déjà pour {employe.Matricule} : modifiez son rôle ou traitez sa demande dans la liste des comptes.");

        role = await RoleActifAsync(role);
        await _administrationRepository.SavePreassignedRoleAsync(employe.Matricule, role, _currentUser.MatriculeRequis);
        await TracerMatriculeAsync(employe.Matricule, "ROLE_PREATTRIBUE", $"Rôle {role}");
        return await _administrationRepository.GetPreassignedRoleDtoAsync(employe.Matricule)
            ?? throw new RessourceIntrouvableException($"Pré-attribution introuvable pour {employe.Matricule}.");
    }

    public async Task DeletePreassignedRoleAsync(string matricule)
    {
        if (!await _administrationRepository.DeletePreassignedRoleAsync(matricule))
            throw new RessourceIntrouvableException($"Aucun rôle pré-attribué pour le matricule {matricule}.");
        await TracerMatriculeAsync(matricule, "PREATTRIBUTION_SUPPRIMEE", "");
    }

    // ───────────────────────────── Règles ─────────────────────────────

    /// <summary>Décision sur une demande : transition contrôlée, administrateur et date tracés sur le compte.</summary>
    private string? Decider(UtilisateursApp user, string nouveauStatut, string? commentaire,
        Func<UtilisateursApp, AuthSettings, DateTime, string>? preparer)
    {
        InterdireSurSoiMeme(user, "traiter votre propre demande");
        var maintenant = DateTime.Now;
        string? jeton = null;
        if (preparer != null) jeton = preparer(user, _settings, maintenant);
        else user.PasserA(nouveauStatut);

        user.ReviewedBy = _currentUser.MatriculeRequis;
        user.ReviewedAt = maintenant;
        user.ReviewComment = commentaire;
        return jeton;
    }

    private async Task<string> RoleActifAsync(string? role)
    {
        role = (role ?? "").Trim().ToUpperInvariant();
        if (role.Length == 0 || !await _administrationRepository.IsActiveRoleAsync(role))
            throw new RegleMetierException($"Le rôle « {role} » n'existe pas ou n'est plus actif.");
        return role;
    }

    private static string? Commentaire(string? texte)
    {
        var t = texte?.Trim();
        if (string.IsNullOrEmpty(t)) return null;
        return t.Length <= LongueurMaximaleCommentaire ? t : t[..LongueurMaximaleCommentaire];
    }

    private async Task<UtilisateursApp> CompteCibleAsync(string matricule) =>
        await _userRepository.GetUserByMatriculeAsync(matricule)
        ?? throw new RessourceIntrouvableException($"Aucun compte pour le matricule {matricule}.");

    private async Task<UserAccountDto> CompteAsync(string matricule) =>
        await _administrationRepository.GetUserAccountAsync(matricule)
        ?? throw new RessourceIntrouvableException($"Aucun compte pour le matricule {matricule}.");

    /// <summary>Un administrateur ne peut pas se retirer ses propres droits : il risquerait de se fermer l'accès.</summary>
    private void InterdireSurSoiMeme(UtilisateursApp user, string action)
    {
        if (string.Equals(user.Matricule, _currentUser.MatriculeRequis, StringComparison.OrdinalIgnoreCase))
            throw new RegleMetierException($"Vous ne pouvez pas {action}.");
    }

    /// <summary>L'application doit toujours garder au moins un administrateur utilisable.</summary>
    private async Task GarderUnAdministrateurAsync(UtilisateursApp user, string action)
    {
        if (user.AccountStatus == AccountStates.Active && await _administrationRepository.CountUsableAdminsAsync() <= 1)
            throw new RegleMetierException($"Impossible de {action} : c'est le dernier administrateur.");
    }

    private Task TracerAsync(UtilisateursApp user, string action, string details) => TracerMatriculeAsync(user.Matricule, action, details);

    private async Task TracerMatriculeAsync(string matricule, string action, string details)
    {
        var admin = _currentUser.MatriculeRequis;
        var texte = string.IsNullOrEmpty(details) ? $"Par l'administrateur {admin}" : $"{details} (par l'administrateur {admin})";
        await _journalRepository.LogActionAsync(matricule, action, texte);
        _logger.LogInformation("Administration : {Action} sur {Matricule} par {Admin} {Details}", action, matricule, admin, details);
    }

    private static (int Page, int PageSize) Pagination(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, TaillePageMax));
}
