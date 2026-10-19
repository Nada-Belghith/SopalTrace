using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Services;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Settings;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Application.DTOs.Auth;
using SopalTrace.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Tests.Services
{
    public class AuthServiceTests
    {
        private const string Matricule = "55555";
        private const string EmailErp = "op@sopal.tn";

        private readonly Mock<IErpService> _mockErpService = new();
        private readonly Mock<IUserRepository> _mockUserRepository = new();
        private readonly Mock<IJournalConnexionRepository> _mockJournalRepository = new();
        private readonly Mock<ISecurityService> _mockSecurityService = new();
        private readonly Mock<IEmailService> _mockEmailService = new();
        private readonly Mock<IUserSessionValidator> _mockSessionValidator = new();
        private readonly AuthService _authService;
        private UtilisateursApp? _compteAjoute;

        public AuthServiceTests()
        {
            _authService = new AuthService(
                _mockErpService.Object,
                _mockUserRepository.Object,
                _mockJournalRepository.Object,
                _mockSecurityService.Object,
                _mockEmailService.Object,
                _mockSessionValidator.Object,
                new AuthSettings { MaxFailedLogins = 3, LockoutMinutes = 15, InvitationHours = 48, FrontendBaseUrl = "https://sopaltrace.tn" },
                new Mock<ILogger<AuthService>>().Object);

            _mockErpService.Setup(e => e.GetEmployeByMatriculeAsync(Matricule))
                .ReturnsAsync(new EmployeErpDto(Matricule, "Opérateur", RolesApp.Operateur, "Opérateur", true, EmailErp));
            _mockErpService.Setup(e => e.CountActiveEmployeesWithEmailAsync(EmailErp)).ReturnsAsync(1);
            _mockUserRepository.Setup(r => r.GetEmailsByRoleAsync(RolesApp.Admin)).ReturnsAsync(new List<string> { "admin@sopal.tn" });
            _mockUserRepository.Setup(r => r.TryAddUserAsync(It.IsAny<UtilisateursApp>(), It.IsAny<PreassignedRole?>()))
                .Callback<UtilisateursApp, PreassignedRole?>((u, _) => _compteAjoute = u)
                .ReturnsAsync(true);
            _mockSecurityService.Setup(s => s.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>())).Returns("jwt");
            _mockSecurityService.Setup(s => s.GenerateRefreshToken()).Returns("refresh");
            _mockSecurityService.Setup(s => s.HashPassword(It.IsAny<string>())).Returns("empreinte");
        }

        private UtilisateursApp CompteActif()
        {
            var user = new UtilisateursApp
            {
                Id = Guid.NewGuid(), Matricule = Matricule, NomComplet = "Opérateur", Email = EmailErp,
                MotDePasseHash = "hash", RoleApp = RolesApp.Operateur, AccountStatus = AccountStates.Active, SecurityStamp = Guid.NewGuid()
            };
            _mockUserRepository.Setup(r => r.GetUserByMatriculeAsync(Matricule)).ReturnsAsync(user);
            return user;
        }

        private UtilisateursApp CompteInvite(string jeton, DateTime expiration)
        {
            var user = new UtilisateursApp
            {
                Id = Guid.NewGuid(), Matricule = Matricule, NomComplet = "Opérateur", Email = EmailErp, RoleApp = RolesApp.Operateur,
                AccountStatus = AccountStates.Invited, ActivationTokenHash = JetonsSecurite.Empreinte(jeton), ActivationTokenExpiresAt = expiration,
                SecurityStamp = Guid.NewGuid()
            };
            _mockUserRepository.Setup(r => r.GetByActivationTokenHashAsync(JetonsSecurite.Empreinte(jeton))).ReturnsAsync(user);
            _mockUserRepository.Setup(r => r.GetUserByMatriculeAsync(Matricule)).ReturnsAsync(user);
            return user;
        }

        private void MotDePasse(bool correct) =>
            _mockSecurityService.Setup(s => s.VerifyPassword("Secret1!", It.IsAny<string>())).Returns(correct);

        private static RefreshToken Jeton(UtilisateursApp user, bool revoque) =>
            new() { Utilisateur = user, UtilisateurId = user.Id, EstRevoque = revoque, DateExpiration = DateTime.Now.AddDays(1) };

        private void AucunCompteCree() =>
            _mockUserRepository.Verify(r => r.TryAddUserAsync(It.IsAny<UtilisateursApp>(), It.IsAny<PreassignedRole?>()), Times.Never);

        private void AucunEmail() => Assert.Empty(_mockEmailService.Invocations);

        // ───────────── Demande d'accès ─────────────

        [Theory]
        [InlineData("pirate@exemple.com")]   // e-mail différent de l'ERP
        [InlineData(EmailErp)]               // e-mail partagé (voir la mise en place)
        public async Task Register_DemandeNonConforme_SansErreurNiCompteNiEmail(string email)
        {
            _mockErpService.Setup(e => e.CountActiveEmployeesWithEmailAsync(EmailErp)).ReturnsAsync(2);

            await _authService.RegisterAsync(new RegisterRequestDto(Matricule, email));

            AucunCompteCree();
            AucunEmail();
            _mockJournalRepository.Verify(j => j.LogActionAsync(Matricule, "INSCRIPTION_REFUSEE", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Register_MatriculeInconnu_ReponseIdentiqueSansRienCreer()
        {
            await _authService.RegisterAsync(new RegisterRequestDto("99999", "x@sopal.tn"));

            AucunCompteCree();
            AucunEmail();
        }

        [Fact]
        public async Task Register_SansPreattribution_DemandeEnAttenteEtAdministrateursPrevenus()
        {
            await _authService.RegisterAsync(new RegisterRequestDto(Matricule, "  OP@sopal.tn "));

            Assert.NotNull(_compteAjoute);
            Assert.Equal(AccountStates.PendingApproval, _compteAjoute!.AccountStatus);
            Assert.Equal(EmailErp, _compteAjoute.Email);
            // Rôle seulement proposé (code métier ERP) : aucun mot de passe, aucun lien d'activation.
            Assert.Equal(RolesApp.Operateur, _compteAjoute.RoleApp);
            Assert.Null(_compteAjoute.MotDePasseHash);
            Assert.Null(_compteAjoute.ActivationTokenHash);
            _mockEmailService.Verify(e => e.SendAccessRequestReceivedEmailAsync(EmailErp, "Opérateur"), Times.Once);
            _mockEmailService.Verify(e => e.SendAccessRequestToAdminEmailAsync("admin@sopal.tn", "Opérateur", Matricule, "Opérateur",
                "https://sopaltrace.tn/dev/habilitations?status=PENDING_APPROVAL"), Times.Once);
            _mockEmailService.Verify(e => e.SendAccountInvitationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task Register_RolePreattribue_InvitationDirecteAvecLienAUsageUnique()
        {
            var preattribution = new PreassignedRole
            {
                Matricule = Matricule, RoleCode = RolesApp.Magasinier, AssignedBy = "66666",
                RoleCodeNavigation = new AppRole { Code = RolesApp.Magasinier, Label = "Magasinier", Active = true }
            };
            _mockUserRepository.Setup(r => r.GetPreassignedRoleAsync(Matricule)).ReturnsAsync(preattribution);
            string? lien = null;
            _mockEmailService.Setup(e => e.SendAccountInvitationEmailAsync(EmailErp, "Opérateur", It.IsAny<string>(), It.IsAny<DateTime>()))
                .Callback<string, string, string, DateTime>((_, _, l, _) => lien = l).Returns(Task.CompletedTask);

            await _authService.RegisterAsync(new RegisterRequestDto(Matricule, EmailErp));

            Assert.Equal(AccountStates.Invited, _compteAjoute!.AccountStatus);
            Assert.Equal(RolesApp.Magasinier, _compteAjoute.RoleApp);
            Assert.Equal("66666", _compteAjoute.ReviewedBy);
            _mockUserRepository.Verify(r => r.TryAddUserAsync(_compteAjoute, preattribution), Times.Once);
            // Le lien porte le jeton après « # » ; seule son empreinte est en base.
            Assert.StartsWith("https://sopaltrace.tn/auth/setup-password#token=", lien);
            var jeton = Uri.UnescapeDataString(lien!.Split("#token=")[1]);
            Assert.Equal(JetonsSecurite.Empreinte(jeton), _compteAjoute.ActivationTokenHash);
            Assert.True(_compteAjoute.ActivationTokenExpiresAt > DateTime.Now.AddHours(47));
            _mockEmailService.Verify(e => e.SendAccessRequestToAdminEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Register_DemandeDejaEnAttente_PasDeDoublonNiDeNouvelEmail()
        {
            _mockUserRepository.Setup(r => r.GetUserByMatriculeAsync(Matricule)).ReturnsAsync(new UtilisateursApp
            {
                Matricule = Matricule, Email = EmailErp, NomComplet = "Opérateur", AccountStatus = AccountStates.PendingApproval
            });

            await _authService.RegisterAsync(new RegisterRequestDto(Matricule, EmailErp));

            AucunCompteCree();
            AucunEmail();
            _mockJournalRepository.Verify(j => j.LogActionAsync(Matricule, "INSCRIPTION_DOUBLON", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Register_EnvoisSimultanes_LeSecondNeCreeRien()
        {
            _mockUserRepository.Setup(r => r.TryAddUserAsync(It.IsAny<UtilisateursApp>(), It.IsAny<PreassignedRole?>())).ReturnsAsync(false);

            await _authService.RegisterAsync(new RegisterRequestDto(Matricule, EmailErp));

            AucunEmail();
            _mockJournalRepository.Verify(j => j.LogActionAsync(Matricule, "INSCRIPTION_DOUBLON", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Register_CompteDejaActif_EmailDInformationSansRienModifier()
        {
            CompteActif();

            await _authService.RegisterAsync(new RegisterRequestDto(Matricule, EmailErp));

            AucunCompteCree();
            _mockEmailService.Verify(e => e.SendAccountAlreadyExistsEmailAsync(EmailErp, "Opérateur", "https://sopaltrace.tn/login"), Times.Once);
            _mockUserRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UtilisateursApp>()), Times.Never);
        }

        [Fact]
        public async Task Register_InvitationExpiree_NouveauLienEtAncienInvalide()
        {
            var user = CompteInvite("ancien", DateTime.Now.AddHours(-1));

            await _authService.RegisterAsync(new RegisterRequestDto(Matricule, EmailErp));

            Assert.NotEqual(JetonsSecurite.Empreinte("ancien"), user.ActivationTokenHash);
            Assert.True(user.ActivationTokenExpiresAt > DateTime.Now.AddHours(47));
            _mockEmailService.Verify(e => e.SendAccountInvitationEmailAsync(EmailErp, "Opérateur", It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task Register_EmailNonParti_LaDemandeResteEnregistree()
        {
            _mockEmailService.Setup(e => e.SendAccessRequestReceivedEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP indisponible"));

            await _authService.RegisterAsync(new RegisterRequestDto(Matricule, EmailErp));

            Assert.Equal(AccountStates.PendingApproval, _compteAjoute!.AccountStatus);
            _mockJournalRepository.Verify(j => j.LogActionAsync(Matricule, "EMAIL_NON_ENVOYE", It.IsAny<string>()), Times.Once);
        }

        // ───────────── Activation par le lien ─────────────

        [Fact]
        public async Task SetupPassword_LienValide_ActiveEtInvalideLeLien()
        {
            var user = CompteInvite("jeton-valide", DateTime.Now.AddHours(10));

            await _authService.SetupPasswordAsync(new SetupPasswordDto("jeton-valide", "Nouveau1!"));

            Assert.Equal(AccountStates.Active, user.AccountStatus);
            Assert.Equal("empreinte", user.MotDePasseHash);
            Assert.Null(user.ActivationTokenHash);
            Assert.Null(user.ActivationTokenExpiresAt);
            _mockUserRepository.Verify(r => r.UpdateUserAsync(user), Times.Once);
        }

        [Fact]
        public async Task SetupPassword_LienExpire_Refuse()
        {
            CompteInvite("jeton-expire", DateTime.Now.AddMinutes(-1));

            await Assert.ThrowsAsync<InvalidTokenException>(() => _authService.SetupPasswordAsync(new SetupPasswordDto("jeton-expire", "Nouveau1!")));
            _mockUserRepository.Verify(r => r.UpdateUserAsync(It.IsAny<UtilisateursApp>()), Times.Never);
        }

        [Fact]
        public async Task SetupPassword_LienDejaUtiliseOuInconnu_Refuse()
        {
            await Assert.ThrowsAsync<InvalidTokenException>(() => _authService.SetupPasswordAsync(new SetupPasswordDto("inconnu", "Nouveau1!")));
            await Assert.ThrowsAsync<InvalidTokenException>(() => _authService.SetupPasswordAsync(new SetupPasswordDto("  ", "Nouveau1!")));
        }

        [Fact]
        public async Task SetupPassword_MotDePasseFaible_RefuseSansConsommerLeLien()
        {
            var user = CompteInvite("jeton", DateTime.Now.AddHours(10));

            await Assert.ThrowsAsync<WeakPasswordException>(() => _authService.SetupPasswordAsync(new SetupPasswordDto("jeton", "faible")));
            Assert.Equal(AccountStates.Invited, user.AccountStatus);
            Assert.NotNull(user.ActivationTokenHash);
        }

        // ───────────── Connexion ─────────────

        [Fact]
        public async Task Login_MatriculeInconnu_IdentifiantsInvalidesSansErreurServeur()
        {
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => _authService.LoginAsync(new LoginRequestDto("00000", "Secret1!")));
        }

        [Fact]
        public async Task Login_DemandeEnAttente_MemeReponseQuUnMauvaisMotDePasse()
        {
            _mockUserRepository.Setup(r => r.GetUserByMatriculeAsync(Matricule)).ReturnsAsync(new UtilisateursApp
            {
                Matricule = Matricule, Email = EmailErp, NomComplet = "Opérateur", AccountStatus = AccountStates.PendingApproval
            });

            await Assert.ThrowsAsync<InvalidCredentialsException>(() => _authService.LoginAsync(new LoginRequestDto(Matricule, "Secret1!")));
        }

        [Fact]
        public async Task Login_EchecsRepetes_VerrouilleLeCompte()
        {
            var user = CompteActif();
            MotDePasse(false);

            await Assert.ThrowsAsync<InvalidCredentialsException>(() => _authService.LoginAsync(new LoginRequestDto(Matricule, "Secret1!")));
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => _authService.LoginAsync(new LoginRequestDto(Matricule, "Secret1!")));
            await Assert.ThrowsAsync<AccountLockedException>(() => _authService.LoginAsync(new LoginRequestDto(Matricule, "Secret1!")));

            Assert.True(user.LockoutEndsAt > DateTime.Now.AddMinutes(14));
            // Même avec le bon mot de passe, le compte reste verrouillé jusqu'à la fin du délai.
            MotDePasse(true);
            await Assert.ThrowsAsync<AccountLockedException>(() => _authService.LoginAsync(new LoginRequestDto(Matricule, "Secret1!")));
        }

        [Fact]
        public async Task Login_CompteDesactiveParLAdministrateur_Refuse()
        {
            var user = CompteActif();
            user.AccountStatus = AccountStates.Disabled;
            MotDePasse(true);

            await Assert.ThrowsAsync<AccountDisabledException>(() => _authService.LoginAsync(new LoginRequestDto(Matricule, "Secret1!")));
            _mockUserRepository.Verify(r => r.AddRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task Login_EmployeInactifDansLErp_Refuse()
        {
            CompteActif();
            MotDePasse(true);
            _mockErpService.Setup(e => e.GetEmployeByMatriculeAsync(Matricule))
                .ReturnsAsync(new EmployeErpDto(Matricule, "Opérateur", RolesApp.Operateur, "Opérateur", false, EmailErp));

            await Assert.ThrowsAsync<AccountDisabledException>(() => _authService.LoginAsync(new LoginRequestDto(Matricule, "Secret1!")));
        }

        [Fact]
        public async Task Login_Succes_RemetLesEchecsAZeroEtPorteLeTamponDansLeJeton()
        {
            var user = CompteActif();
            user.FailedLoginCount = 2;
            MotDePasse(true);

            var (reponse, refresh) = await _authService.LoginAsync(new LoginRequestDto(Matricule, "Secret1!"));

            Assert.Equal(("jwt", "refresh"), (reponse.Token, refresh));
            Assert.Equal(0, user.FailedLoginCount);
            _mockSecurityService.Verify(s => s.GenerateJwtToken(user.Id.ToString(), Matricule, RolesApp.Operateur, user.SecurityStamp), Times.Once);
        }

        // ───────────── Rafraîchissement et déconnexion ─────────────

        [Fact]
        public async Task Refresh_JetonRevoqueReutilise_FermeToutesLesSessions()
        {
            var user = CompteActif();
            var ancienTampon = user.SecurityStamp;
            _mockUserRepository.Setup(r => r.GetRefreshTokenAsync("vole")).ReturnsAsync(Jeton(user, revoque: true));

            await Assert.ThrowsAsync<RefreshTokenRevokedException>(() => _authService.RefreshTokenAsync("vole"));

            _mockUserRepository.Verify(r => r.RevokeAllTokensForUserAsync(user.Id), Times.Once);
            Assert.NotEqual(ancienTampon, user.SecurityStamp);
            _mockSessionValidator.Verify(v => v.Invalidate(user.Id), Times.Once);
        }

        [Fact]
        public async Task Refresh_CompteDesactiveDepuisLaConnexion_Refuse()
        {
            var user = CompteActif();
            user.AccountStatus = AccountStates.Disabled;
            _mockUserRepository.Setup(r => r.GetRefreshTokenAsync("ok")).ReturnsAsync(Jeton(user, revoque: false));

            await Assert.ThrowsAsync<AccountDisabledException>(() => _authService.RefreshTokenAsync("ok"));
            _mockUserRepository.Verify(r => r.AddRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task Refresh_Valide_RevoqueLAncienEtEnEmetUnNouveau()
        {
            var user = CompteActif();
            var jeton = Jeton(user, revoque: false);
            _mockUserRepository.Setup(r => r.GetRefreshTokenAsync("ok")).ReturnsAsync(jeton);

            var (_, nouveau) = await _authService.RefreshTokenAsync("ok");

            Assert.Equal("refresh", nouveau);
            _mockUserRepository.Verify(r => r.RevokeRefreshTokenAsync(jeton), Times.Once);
            _mockUserRepository.Verify(r => r.AddRefreshTokenAsync(user.Id, "refresh", It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task Logout_SansJetonDAcces_RevoqueParLeCookie()
        {
            var user = CompteActif();
            _mockUserRepository.Setup(r => r.GetRefreshTokenAsync("cookie")).ReturnsAsync(Jeton(user, revoque: false));

            await _authService.LogoutAsync("cookie", userId: null, matricule: null);

            _mockUserRepository.Verify(r => r.RevokeAllTokensForUserAsync(user.Id), Times.Once);
        }

        // ───────────── Mot de passe oublié ─────────────

        private UtilisateursApp CompteAvecCode(int tentatives, DateTime? verrouJusquA = null)
        {
            var user = new UtilisateursApp
            {
                Id = Guid.NewGuid(), Matricule = Matricule, Email = EmailErp, MotDePasseHash = "hash", RoleApp = RolesApp.Operateur,
                AccountStatus = AccountStates.Active, CodeRecuperationHash = "hash123", DateExpirationCode = DateTime.Now.AddMinutes(10),
                NombreTentativesCode = tentatives, LockoutEndsAt = verrouJusquA, SecurityStamp = Guid.NewGuid()
            };
            _mockUserRepository.Setup(x => x.GetUserByEmailAsync(EmailErp)).ReturnsAsync(user);
            return user;
        }

        [Fact]
        public async Task ForgotPassword_CompteNonActif_AucunCodeEnvoye()
        {
            _mockUserRepository.Setup(x => x.GetUserByEmailAsync(EmailErp)).ReturnsAsync(new UtilisateursApp
            {
                Matricule = Matricule, Email = EmailErp, AccountStatus = AccountStates.Invited, RoleApp = RolesApp.Operateur
            });

            await _authService.ForgotPasswordAsync(new ForgotPasswordDto(EmailErp));

            AucunEmail();
        }

        [Fact]
        public async Task ResetPassword_TropDeTentatives_Refuse()
        {
            CompteAvecCode(tentatives: 5);

            await Assert.ThrowsAsync<TooManyAttemptsException>(() => _authService.ResetPasswordAsync(new ResetPasswordDto(EmailErp, "123456", "NewPass123!")));
            _mockSecurityService.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResetPassword_CodeIncorrect_IncrementeLesTentatives()
        {
            var user = CompteAvecCode(tentatives: 2);
            _mockSecurityService.Setup(x => x.VerifyPassword("WrongCode", "hash123")).Returns(false);

            await Assert.ThrowsAsync<InvalidTokenException>(() => _authService.ResetPasswordAsync(new ResetPasswordDto(EmailErp, "WrongCode", "NewPass123!")));

            Assert.Equal(3, user.NombreTentativesCode);
            _mockUserRepository.Verify(x => x.UpdateUserAsync(user), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_Succes_FermeLesSessionsEtDeverrouille()
        {
            var user = CompteAvecCode(tentatives: 0, verrouJusquA: DateTime.Now.AddMinutes(5));
            var ancienTampon = user.SecurityStamp;
            _mockSecurityService.Setup(x => x.VerifyPassword("123456", "hash123")).Returns(true);

            await _authService.ResetPasswordAsync(new ResetPasswordDto(EmailErp, "123456", "Nouveau1!"));

            Assert.Null(user.LockoutEndsAt);
            Assert.NotEqual(ancienTampon, user.SecurityStamp);
            _mockUserRepository.Verify(r => r.RevokeAllTokensForUserAsync(user.Id), Times.Once);
        }
    }
}
