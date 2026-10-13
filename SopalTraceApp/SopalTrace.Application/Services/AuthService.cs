using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.Auth;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Settings;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services;

public class AuthService : IAuthService
{
    /// <summary>Tentatives de saisie du code de récupération (mot de passe oublié).</summary>
    private const int MaxTentativesCode = 5;
    private const int DureeSessionJours = 7;

    /// <summary>Délai minimal avant qu'une nouvelle inscription renvoie un lien d'activation encore valable.</summary>
    private static readonly TimeSpan DelaiRenvoiInvitation = TimeSpan.FromMinutes(15);

    /// <summary>Empreinte BCrypt d'un mot de passe quelconque : un matricule inconnu coûte le même temps de vérification qu'un connu.</summary>
    private const string EmpreinteFactice = "$2a$11$5KSlHmpZivVdH6OGivIAfeNvT7RICw8InLlbEiIFb1LYFdzgJbm62";

    private readonly IErpService _erpService;
    private readonly IUserRepository _userRepository;
    private readonly IJournalConnexionRepository _journalRepository;
    private readonly ISecurityService _securityService;
    private readonly IEmailService _emailService;
    private readonly IUserSessionValidator _sessionValidator;
    private readonly AuthSettings _settings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IErpService erpService, IUserRepository userRepository, IJournalConnexionRepository journalRepository,
        ISecurityService securityService, IEmailService emailService, IUserSessionValidator sessionValidator,
        AuthSettings settings, ILogger<AuthService> logger)
    {
        _erpService = erpService;
        _userRepository = userRepository;
        _journalRepository = journalRepository;
        _securityService = securityService;
        _emailService = emailService;
        _sessionValidator = sessionValidator;
        _settings = settings;
        _logger = logger;
    }

    // ───────────────────────────── Demande d'accès et activation ─────────────────────────────

    /// <summary>
    /// Demande d'accès. La réponse est toujours la même (anti-énumération : elle ne révèle ni l'existence
    /// du matricule, ni celle d'un compte) ; le motif réel est écrit dans le journal des connexions.
    /// <list type="bullet">
    /// <item>Matricule et e-mail conformes à l'ERP, rôle pré-attribué par l'administrateur : invitation immédiate.</item>
    /// <item>Sinon : demande PENDING_APPROVAL, visible dans l'écran des habilitations, administrateurs prévenus par e-mail.</item>
    /// <item>Demande déjà déposée : rien de plus (pas de doublon, pas de nouvel e-mail).</item>
    /// </list>
    /// </summary>
    public async Task RegisterAsync(RegisterRequestDto request)
    {
        var matricule = request.Matricule.Trim();
        var email = request.Email.Trim();
        _logger.LogInformation("Demande d'accès pour le matricule {Matricule}", matricule);

        var employe = await _erpService.GetEmployeByMatriculeAsync(matricule);
        if (employe == null || !employe.EstActif)
        {
            await _journalRepository.LogActionAsync(matricule, "INSCRIPTION_REFUSEE", "Matricule inconnu ou inactif dans l'ERP");
            return;
        }

        // L'adresse doit être celle de l'ERP : connaître un matricule ne suffit pas à s'approprier le compte.
        var emailErp = employe.Email?.Trim();
        if (string.IsNullOrEmpty(emailErp) || !string.Equals(emailErp, email, StringComparison.OrdinalIgnoreCase))
        {
            await _journalRepository.LogActionAsync(matricule, "INSCRIPTION_REFUSEE", "E-mail différent de celui de l'ERP");
            return;
        }

        // Boîte partagée par plusieurs employés : celui qui la lit pourrait activer le compte d'un autre.
        if (await _erpService.CountActiveEmployeesWithEmailAsync(emailErp) > 1)
        {
            await _journalRepository.LogActionAsync(matricule, "INSCRIPTION_REFUSEE", "E-mail ERP partagé par plusieurs employés");
            return;
        }

        var maintenant = DateTime.Now;
        var existant = await _userRepository.GetUserByMatriculeAsync(employe.Matricule);
        if (existant != null)
        {
            await TraiterNouvelleDemandeAsync(existant, maintenant);
            return;
        }
        if (await _userRepository.ExistsByEmailAsync(emailErp))
        {
            await _journalRepository.LogActionAsync(matricule, "INSCRIPTION_REFUSEE", "E-mail déjà utilisé par un autre compte");
            return;
        }

        var user = new UtilisateursApp
        {
            Id = Guid.NewGuid(),
            Matricule = employe.Matricule,
            NomComplet = employe.NomComplet,
            Email = emailErp,
            IntituleMetier = employe.IntituleMetier,
            SecurityStamp = Guid.NewGuid(),
            DateCreation = maintenant
        };

        var preattribution = await _userRepository.GetPreassignedRoleAsync(employe.Matricule);
        string? jeton = null;
        if (preattribution?.RoleCodeNavigation?.Active == true)
        {
            user.RoleApp = preattribution.RoleCode;
            user.ReviewedBy = preattribution.AssignedBy;
            user.ReviewedAt = maintenant;
            user.ReviewComment = "Rôle pré-attribué avant l'inscription";
            jeton = InvitationsCompte.Preparer(user, _settings, maintenant);
        }
        else
        {
            // Simple proposition pour l'administrateur : le code métier de l'ERP ne donne aucun droit à lui seul.
            user.RoleApp = RolesApp.TousLesRolesAutorises.Contains(employe.CodeMetier) ? employe.CodeMetier : null;
        }

        if (!await _userRepository.TryAddUserAsync(user, jeton != null ? preattribution : null))
        {
            await _journalRepository.LogActionAsync(matricule, "INSCRIPTION_DOUBLON", "Demande déjà enregistrée (envoi simultané)");
            return;
        }

        if (jeton != null)
        {
            await _journalRepository.LogActionAsync(user.Matricule, "INSCRIPTION",
                $"Rôle pré-attribué {user.RoleApp} par {user.ReviewedBy} : lien d'activation envoyé");
            await InvitationsCompte.EnvoyerAsync(user, jeton, _settings, _emailService, _journalRepository, _logger);
            return;
        }

        await _journalRepository.LogActionAsync(user.Matricule, "INSCRIPTION", "Demande d'accès en attente de validation");
        await InvitationsCompte.EssayerAsync(() => _emailService.SendAccessRequestReceivedEmailAsync(user.Email, user.NomComplet),
            user.Matricule, "accusé de réception", _journalRepository, _logger);
        await NotifierAdministrateursAsync(user);
    }

    /// <summary>Nouvelle demande d'un matricule qui a déjà un compte : idempotente, sans doublon.</summary>
    private async Task TraiterNouvelleDemandeAsync(UtilisateursApp user, DateTime maintenant)
    {
        switch (user.AccountStatus)
        {
            case AccountStates.Invited:
                // Lien perdu ou expiré : un nouveau lien, au plus un par quart d'heure.
                var emisLe = user.ActivationTokenExpiresAt?.AddHours(-_settings.InvitationHours);
                if (emisLe == null || maintenant - emisLe >= DelaiRenvoiInvitation)
                {
                    var jeton = InvitationsCompte.Preparer(user, _settings, maintenant);
                    await _userRepository.UpdateUserAsync(user);
                    await _journalRepository.LogActionAsync(user.Matricule, "INVITATION_RENVOYEE", "Nouveau lien à la demande de l'employé");
                    await InvitationsCompte.EnvoyerAsync(user, jeton, _settings, _emailService, _journalRepository, _logger);
                }
                else
                {
                    await _journalRepository.LogActionAsync(user.Matricule, "INSCRIPTION_DOUBLON", "Lien d'activation envoyé il y a moins de 15 min");
                }
                break;

            case AccountStates.Active:
                await _journalRepository.LogActionAsync(user.Matricule, "INSCRIPTION_DOUBLON", "Compte déjà actif : e-mail d'information envoyé");
                await InvitationsCompte.EssayerAsync(
                    () => _emailService.SendAccountAlreadyExistsEmailAsync(user.Email, user.NomComplet, _settings.LoginLink),
                    user.Matricule, "compte déjà existant", _journalRepository, _logger);
                break;

            case AccountStates.PendingApproval:
                await _journalRepository.LogActionAsync(user.Matricule, "INSCRIPTION_DOUBLON", "Demande déjà en attente de validation");
                break;

            default:
                await _journalRepository.LogActionAsync(user.Matricule, "INSCRIPTION_REFUSEE",
                    $"Compte {AccountStates.Libelle(user.AccountStatus)} : seul un administrateur peut rouvrir l'accès");
                break;
        }
    }

    private async Task NotifierAdministrateursAsync(UtilisateursApp demandeur)
    {
        foreach (var email in await _userRepository.GetEmailsByRoleAsync(RolesApp.Admin))
        {
            await InvitationsCompte.EssayerAsync(
                () => _emailService.SendAccessRequestToAdminEmailAsync(email, demandeur.NomComplet, demandeur.Matricule,
                    demandeur.IntituleMetier ?? string.Empty, _settings.PendingRequestsLink),
                demandeur.Matricule, "notification administrateur", _journalRepository, _logger);
        }
    }

    /// <summary>Activation par le lien reçu : jeton à usage unique, valable <see cref="AuthSettings.InvitationHours"/> heures.</summary>
    public async Task SetupPasswordAsync(SetupPasswordDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new InvalidTokenException();

        var user = await _userRepository.GetByActivationTokenHashAsync(JetonsSecurite.Empreinte(request.Token.Trim()));
        if (user == null || user.AccountStatus != AccountStates.Invited || !(user.ActivationTokenExpiresAt > DateTime.Now))
            throw new InvalidTokenException();

        if (!EstMotDePasseRobuste(request.NouveauMotDePasse))
            throw new WeakPasswordException();

        user.MotDePasseHash = _securityService.HashPassword(request.NouveauMotDePasse);
        user.ActivationTokenHash = null;
        user.ActivationTokenExpiresAt = null;
        user.FailedLoginCount = 0;
        user.LockoutEndsAt = null;
        user.SecurityStamp = Guid.NewGuid();
        user.PasserA(AccountStates.Active);

        await _userRepository.UpdateUserAsync(user);
        await _journalRepository.LogActionAsync(user.Matricule, "ACTIVATION_COMPTE", "Mot de passe défini, compte activé");

        _logger.LogInformation("Compte activé pour le matricule {Matricule}", user.Matricule);
    }

    // ───────────────────────────── Connexion ─────────────────────────────

    public async Task<(AuthResponseDto Response, string RefreshToken)> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetUserByMatriculeAsync(request.Matricule);

        // Compte inconnu ou sans mot de passe (demande en attente, invitation non utilisée) : même réponse
        // et même temps de calcul qu'un mauvais mot de passe.
        if (user?.MotDePasseHash == null)
        {
            _securityService.VerifyPassword(request.MotDePasse, EmpreinteFactice);
            await _journalRepository.LogActionAsync(request.Matricule, "CONNEXION_ECHEC", user == null ? "Matricule inconnu" : "Compte sans mot de passe défini");
            throw new InvalidCredentialsException();
        }

        var maintenant = DateTime.Now;
        if (user.LockoutEndsAt > maintenant)
        {
            await _journalRepository.LogActionAsync(user.Matricule, "CONNEXION_ECHEC", "Compte verrouillé");
            throw new AccountLockedException(MinutesRestantes(user.LockoutEndsAt.Value, maintenant));
        }

        if (!_securityService.VerifyPassword(request.MotDePasse, user.MotDePasseHash))
        {
            await EnregistrerEchecConnexionAsync(user, maintenant);
            if (user.LockoutEndsAt > maintenant)
                throw new AccountLockedException(MinutesRestantes(user.LockoutEndsAt.Value, maintenant));
            throw new InvalidCredentialsException();
        }

        if (!AccountStates.PeutSeConnecter(user.AccountStatus))
        {
            await _journalRepository.LogActionAsync(user.Matricule, "CONNEXION_ECHEC", $"Compte {AccountStates.Libelle(user.AccountStatus)}");
            throw new AccountDisabledException();
        }

        if (!await EstActifDansErpAsync(user.Matricule))
        {
            await _journalRepository.LogActionAsync(user.Matricule, "CONNEXION_ECHEC", "Employé inactif dans l'ERP");
            throw new AccountDisabledException();
        }

        user.FailedLoginCount = 0;
        user.LockoutEndsAt = null;
        user.DateDerniereConnexion = maintenant;
        await _userRepository.UpdateUserAsync(user);

        var reponse = await OuvrirSessionAsync(user);
        await _journalRepository.LogActionAsync(user.Matricule, "CONNEXION", "Succès");
        return reponse;
    }

    private async Task EnregistrerEchecConnexionAsync(UtilisateursApp user, DateTime maintenant)
    {
        user.FailedLoginCount++;
        string details = $"Mot de passe incorrect ({user.FailedLoginCount}/{_settings.MaxFailedLogins})";
        if (user.FailedLoginCount >= _settings.MaxFailedLogins)
        {
            user.LockoutEndsAt = maintenant.AddMinutes(_settings.LockoutMinutes);
            user.FailedLoginCount = 0;
            details = $"Compte verrouillé {_settings.LockoutMinutes} min après {_settings.MaxFailedLogins} échecs";
        }
        await _userRepository.UpdateUserAsync(user);
        await _journalRepository.LogActionAsync(user.Matricule, "CONNEXION_ECHEC", details);
    }

    public async Task<(AuthResponseDto Response, string RefreshToken)> RefreshTokenAsync(string refreshToken)
    {
        var jeton = await _userRepository.GetRefreshTokenAsync(refreshToken)
            ?? throw new InvalidTokenException();
        var user = jeton.Utilisateur;

        // Un jeton déjà utilisé ou révoqué qui revient : il a probablement été volé. Toutes les sessions sont fermées.
        if (jeton.EstRevoque == true)
        {
            await FermerToutesLesSessionsAsync(user);
            await _journalRepository.LogActionAsync(user.Matricule, "SESSION_REVOQUEE", "Réutilisation d'un jeton de rafraîchissement révoqué");
            _logger.LogWarning("Réutilisation d'un jeton de rafraîchissement révoqué pour le matricule {Matricule}", user.Matricule);
            throw new RefreshTokenRevokedException();
        }

        if (jeton.DateExpiration < DateTime.Now)
            throw new InvalidTokenException();

        // Le compte a pu être désactivé (ici ou dans l'ERP) depuis la connexion.
        if (!AccountStates.PeutSeConnecter(user.AccountStatus) || !await EstActifDansErpAsync(user.Matricule))
        {
            await FermerToutesLesSessionsAsync(user);
            throw new AccountDisabledException();
        }

        await _userRepository.RevokeRefreshTokenAsync(jeton);
        return await OuvrirSessionAsync(user);
    }

    public async Task LogoutAsync(string? refreshToken, Guid? userId, string? matricule)
    {
        UtilisateursApp? user = null;
        if (!string.IsNullOrEmpty(refreshToken))
            user = (await _userRepository.GetRefreshTokenAsync(refreshToken))?.Utilisateur;
        if (user == null && userId.HasValue)
            user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return;

        await _userRepository.RevokeAllTokensForUserAsync(user.Id);
        await _journalRepository.LogActionAsync(user.Matricule, "DECONNEXION", "Succès");
    }

    // ───────────────────────────── Mot de passe oublié ─────────────────────────────

    public async Task ForgotPasswordAsync(ForgotPasswordDto request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);
        if (user == null || user.AccountStatus != AccountStates.Active) return;

        // Une seule demande par minute : un code émis il y a moins d'une minute expire dans plus de 14 minutes.
        if (user.DateExpirationCode > DateTime.Now.AddMinutes(14))
            return;

        string code = _securityService.GenerateRecoveryCode();

        user.CodeRecuperationHash = _securityService.HashPassword(code);
        user.DateExpirationCode = DateTime.Now.AddMinutes(15);
        user.NombreTentativesCode = 0;
        await _userRepository.UpdateUserAsync(user);

        await _emailService.SendResetCodeEmailAsync(user.Email, code);
        await _journalRepository.LogActionAsync(user.Matricule, "MOT_DE_PASSE_OUBLIE", "Code de récupération envoyé");
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);

        if (user == null ||
            user.AccountStatus != AccountStates.Active ||
            string.IsNullOrEmpty(user.CodeRecuperationHash) ||
            user.DateExpirationCode < DateTime.Now)
        {
            throw new InvalidTokenException();
        }

        if (user.NombreTentativesCode >= MaxTentativesCode)
        {
            throw new TooManyAttemptsException();
        }

        if (!_securityService.VerifyPassword(request.Code, user.CodeRecuperationHash))
        {
            user.NombreTentativesCode++;
            await _userRepository.UpdateUserAsync(user);
            throw new InvalidTokenException();
        }

        if (!EstMotDePasseRobuste(request.NouveauMotDePasse))
            throw new WeakPasswordException();

        user.MotDePasseHash = _securityService.HashPassword(request.NouveauMotDePasse);
        user.CodeRecuperationHash = null;
        user.DateExpirationCode = null;
        user.NombreTentativesCode = 0;
        user.FailedLoginCount = 0;
        user.LockoutEndsAt = null;

        await FermerToutesLesSessionsAsync(user);
        await _journalRepository.LogActionAsync(user.Matricule, "MOT_DE_PASSE_REINITIALISE", "Sessions fermées");
    }

    // ───────────────────────────── Outils ─────────────────────────────

    /// <summary>Appelé seulement pour un compte ACTIVE, qui a toujours un rôle (contrainte CK_UtilisateursApp_RoleRequired).</summary>
    private async Task<(AuthResponseDto, string)> OuvrirSessionAsync(UtilisateursApp user)
    {
        var role = user.RoleApp ?? throw new AccountDisabledException();
        string token = _securityService.GenerateJwtToken(user.Id.ToString(), user.Matricule, role, user.SecurityStamp);
        string refreshToken = _securityService.GenerateRefreshToken();
        await _userRepository.AddRefreshTokenAsync(user.Id, refreshToken, DateTime.Now.AddDays(DureeSessionJours));
        return (new AuthResponseDto(token, user.NomComplet, role), refreshToken);
    }

    private Task FermerToutesLesSessionsAsync(UtilisateursApp user) =>
        SessionsUtilisateur.FermerAsync(user, _userRepository, _sessionValidator);

    private async Task<bool> EstActifDansErpAsync(string matricule)
    {
        var employe = await _erpService.GetEmployeByMatriculeAsync(matricule);
        return employe?.EstActif == true;
    }

    private static int MinutesRestantes(DateTime fin, DateTime maintenant) =>
        Math.Max(1, (int)Math.Ceiling((fin - maintenant).TotalMinutes));

    internal static bool EstMotDePasseRobuste(string mdp) =>
        mdp.Length >= 8 && mdp.Any(char.IsUpper) && mdp.Any(char.IsDigit);
}
