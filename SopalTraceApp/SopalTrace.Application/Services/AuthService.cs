using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.Auth;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Exceptions;
using System.Security.Cryptography;

namespace SopalTrace.Application.Services;

public class AuthService : IAuthService
{
    private readonly IErpService _erpService;
    private readonly IUserRepository _userRepository;
    private readonly IJournalConnexionRepository _journalRepository;
    private readonly ISecurityService _securityService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    private readonly string[] _rolesAutorises = { "PROD_GAZ", "QUAL", "MAG", "DI" };

    public AuthService(IErpService erpService, IUserRepository userRepository, IJournalConnexionRepository journalRepository, ISecurityService securityService, IEmailService emailService, ILogger<AuthService> logger)
    {
        _erpService = erpService;
        _userRepository = userRepository;
        _journalRepository = journalRepository;
        _securityService = securityService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<(AuthResponseDto Response, string RefreshToken)> RegisterAsync(RegisterRequestDto request)
    {
        _logger.LogInformation("Tentative d'inscription pour le matricule {Matricule}", request.Matricule);

        await EnsureUserDoesNotExistAsync(request.Matricule, request.Email);
        var employeErp = await GetAndValidateErpUserAsync(request.Matricule);

        string hashedPassword = _securityService.HashPassword(request.MotDePasse);
        await _userRepository.CreateUserAsync(
            employeErp.Matricule, employeErp.NomComplet, request.Email,
            hashedPassword, employeErp.CodeMetier, employeErp.IntituleMetier);

        await _journalRepository.LogActionAsync(request.Matricule, "INSCRIPTION", "Succès");

        return await LoginAsync(new LoginRequestDto(request.Matricule, request.MotDePasse));
    }

    public async Task<(AuthResponseDto Response, string RefreshToken)> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetUserForLoginAsync(request.Matricule);

        if (string.IsNullOrEmpty(user.Id) || !_securityService.VerifyPassword(request.MotDePasse, user.Hash))
        {
            await _journalRepository.LogActionAsync(request.Matricule, "CONNEXION_ECHEC", "Identifiants invalides");
            throw new InvalidCredentialsException();
        }

        string token = _securityService.GenerateJwtToken(user.Id, request.Matricule, user.Role);
        string refreshToken = _securityService.GenerateRefreshToken();

        await _userRepository.AddRefreshTokenAsync(user.Id, refreshToken, Guid.NewGuid().ToString(), DateTime.UtcNow.AddDays(7));
        await _journalRepository.LogActionAsync(request.Matricule, "CONNEXION", "Succès");

        var responseDto = new AuthResponseDto(token, user.Nom, user.Role);
        return (responseDto, refreshToken);
    }

    public async Task<(AuthResponseDto Response, string RefreshToken)> RefreshTokenAsync(string expiredJwt, string refreshToken)
    {
        var user = await _userRepository.GetUserByRefreshTokenAsync(refreshToken);

        // MODIF 1 : Utilisation de InvalidTokenException
        if (user == null)
            throw new InvalidTokenException();

        bool isValid = await _userRepository.ValidateRefreshTokenAsync(refreshToken, user.Id.ToString());

        // MODIF 2 : Si le token est présent mais non valide (révoqué ou expiré)
        if (!isValid)
        {
            _logger.LogWarning("Tentative de refresh invalide pour l'utilisateur {UserId}", user.Id);
            throw new RefreshTokenRevokedException();
        }

        await _userRepository.RevokeRefreshTokenAsync(refreshToken);

        var newJwtToken = _securityService.GenerateJwtToken(user.Id.ToString(), user.Matricule, user.RoleApp);
        var newRefreshToken = _securityService.GenerateRefreshToken();

        await _userRepository.AddRefreshTokenAsync(user.Id.ToString(), newRefreshToken, Guid.NewGuid().ToString(), DateTime.UtcNow.AddDays(7));

        var responseDto = new AuthResponseDto(newJwtToken, user.NomComplet, user.RoleApp);
        return (responseDto, newRefreshToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);
        if (user == null) return;

        string code = new Random().Next(100000, 999999).ToString();

        user.CodeRecuperation = code;
        user.DateExpirationCode = DateTime.UtcNow.AddMinutes(15);
        await _userRepository.UpdateUserAsync(user);

        await _emailService.SendResetCodeEmailAsync(request.Email, code);

        _logger.LogInformation("Email de récupération envoyé avec succès à {Email}", request.Email);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);

        if (user == null ||
            user.CodeRecuperation != request.Code ||
            user.DateExpirationCode < DateTime.UtcNow)
        {
            throw new InvalidTokenException();
        }

        if (request.NouveauMotDePasse.Length < 6)
            throw new WeakPasswordException();

        user.MotDePasseHash = _securityService.HashPassword(request.NouveauMotDePasse);
        user.CodeRecuperation = null;
        user.DateExpirationCode = null;

        await _userRepository.UpdateUserAsync(user);
        _logger.LogInformation("Mot de passe réinitialisé avec succès pour {Email}", request.Email);
    }

    private async Task EnsureUserDoesNotExistAsync(string matricule, string email)
    {
        if (await _userRepository.ExistsByMatriculeAsync(matricule))
            throw new UserAlreadyExistsException(matricule);

        if (await _userRepository.ExistsByEmailAsync(email))
            throw new EmailAlreadyUsedException(email);
    }

    private async Task<EmployeErpDto> GetAndValidateErpUserAsync(string matricule)
    {
        var employe = await _erpService.GetEmployeByMatriculeAsync(matricule);

        if (employe == null || !employe.EstActif)
            throw new UserNotFoundInErpException(matricule);

        if (!_rolesAutorises.Contains(employe.CodeMetier))
            throw new RoleNotAllowedException(employe.IntituleMetier);

        return employe;
    }
}