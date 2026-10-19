using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SopalTrace.Application.DTOs.Administration;
using SopalTrace.Application.DTOs.Auth;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Services;
using SopalTrace.Application.Settings;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    public class UserAdministrationServiceTests
    {
        private const string Admin = "66666";

        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IUserAdministrationRepository> _administration = new();
        private readonly Mock<IJournalConnexionRepository> _journal = new();
        private readonly Mock<IUserSessionValidator> _sessions = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<IEmailService> _email = new();
        private readonly Mock<IErpService> _erp = new();
        private readonly UserAdministrationService _service;

        public UserAdministrationServiceTests()
        {
            _service = new UserAdministrationService(_users.Object, _administration.Object, _journal.Object, _sessions.Object,
                _currentUser.Object, _email.Object, _erp.Object, new AuthSettings { FrontendBaseUrl = "https://sopaltrace.tn", InvitationHours = 48 },
                new Mock<ILogger<UserAdministrationService>>().Object);

            _currentUser.Setup(c => c.MatriculeRequis).Returns(Admin);
            _administration.Setup(a => a.IsActiveRoleAsync(It.IsIn(RolesApp.TousLesRolesAutorises))).ReturnsAsync(true);
            _administration.Setup(a => a.GetUserAccountAsync(It.IsAny<string>()))
                .ReturnsAsync((string m) => new UserAccountDto(m, "Nom", "e@s.tn", "R", "R", null, AccountStatuses.Active, null, null, null, 0,
                    null, null, null, null));
        }

        private UtilisateursApp Compte(string matricule, string? role, string statut = AccountStates.Active)
        {
            var user = new UtilisateursApp
            {
                Id = Guid.NewGuid(), Matricule = matricule, NomComplet = "Nom", Email = $"{matricule}@s.tn", RoleApp = role,
                AccountStatus = statut, MotDePasseHash = statut == AccountStates.Active ? "hash" : null, SecurityStamp = Guid.NewGuid()
            };
            _users.Setup(u => u.GetUserByMatriculeAsync(matricule)).ReturnsAsync(user);
            return user;
        }

        // ───────────── Demandes d'accès ─────────────

        [Fact]
        public async Task Approuver_AttribueLeRoleInviteEtTraceLaDecision()
        {
            var user = Compte("77777", null, AccountStates.PendingApproval);
            string? lien = null;
            _email.Setup(e => e.SendAccountInvitationEmailAsync("77777@s.tn", "Nom", It.IsAny<string>(), It.IsAny<DateTime>()))
                .Callback<string, string, string, DateTime>((_, _, l, _) => lien = l).Returns(Task.CompletedTask);

            var resultat = await _service.ApproveAsync("77777", new ApproveAccessRequest("responsable_di", "Nouveau chef de projet"));

            Assert.True(resultat.EmailSent);
            Assert.Equal((AccountStates.Invited, RolesApp.ResponsableDI), (user.AccountStatus, user.RoleApp));
            Assert.Equal((Admin, "Nouveau chef de projet"), (user.ReviewedBy, user.ReviewComment));
            Assert.NotNull(user.ReviewedAt);
            Assert.Equal(JetonsSecurite.Empreinte(Uri.UnescapeDataString(lien!.Split("#token=")[1])), user.ActivationTokenHash);
            _users.Verify(u => u.UpdateUserAsync(user), Times.Once);
            _journal.Verify(j => j.LogActionAsync("77777", "DEMANDE_APPROUVEE",
                It.Is<string>(d => d.Contains(RolesApp.ResponsableDI) && d.Contains(Admin))), Times.Once);
        }

        [Fact]
        public async Task Approuver_SansRoleValide_Refuse()
        {
            var user = Compte("77777", null, AccountStates.PendingApproval);

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.ApproveAsync("77777", new ApproveAccessRequest("", null)));
            Assert.Equal(AccountStates.PendingApproval, user.AccountStatus);
            _users.Verify(u => u.UpdateUserAsync(It.IsAny<UtilisateursApp>()), Times.Never);
        }

        [Fact]
        public async Task Approuver_CompteDejaActif_TransitionInterdite()
        {
            Compte("55555", RolesApp.Operateur);

            var ex = await Assert.ThrowsAsync<RegleMetierException>(() => _service.ApproveAsync("55555", new ApproveAccessRequest(RolesApp.Magasinier, null)));
            Assert.Contains("actif", ex.Message);
        }

        [Fact]
        public async Task Approuver_DemandeDejaTraiteeParUnAutreAdministrateur_DecisionConservee()
        {
            var user = Compte("77777", RolesApp.Magasinier, AccountStates.Invited);
            user.ReviewedBy = "66667";

            var ex = await Assert.ThrowsAsync<RegleMetierException>(() => _service.ApproveAsync("77777", new ApproveAccessRequest(RolesApp.Operateur, null)));

            Assert.Contains("déjà été traitée", ex.Message);
            Assert.Equal((RolesApp.Magasinier, "66667"), (user.RoleApp, user.ReviewedBy));
            _users.Verify(u => u.UpdateUserAsync(It.IsAny<UtilisateursApp>()), Times.Never);
        }

        [Fact]
        public async Task Approuver_EmailNonParti_CompteInviteEtSignale()
        {
            Compte("77777", null, AccountStates.PendingApproval);
            _email.Setup(e => e.SendAccountInvitationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("SMTP"));

            var resultat = await _service.ApproveAsync("77777", new ApproveAccessRequest(RolesApp.Operateur, null));

            Assert.False(resultat.EmailSent);
            _journal.Verify(j => j.LogActionAsync("77777", "EMAIL_NON_ENVOYE", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Refuser_MotifObligatoire()
        {
            var user = Compte("77777", null, AccountStates.PendingApproval);

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.RejectAsync("77777", new RejectAccessRequest("  ")));
            Assert.Equal(AccountStates.PendingApproval, user.AccountStatus);
        }

        [Fact]
        public async Task Refuser_TraceLeMotifEtPrevientLEmploye()
        {
            var user = Compte("77777", null, AccountStates.PendingApproval);

            await _service.RejectAsync("77777", new RejectAccessRequest("Stagiaire sans besoin d'accès"));

            Assert.Equal(AccountStates.Rejected, user.AccountStatus);
            Assert.Equal((Admin, "Stagiaire sans besoin d'accès"), (user.ReviewedBy, user.ReviewComment));
            _email.Verify(e => e.SendAccessRequestRejectedEmailAsync("77777@s.tn", "Nom", "Stagiaire sans besoin d'accès"), Times.Once);
            _journal.Verify(j => j.LogActionAsync("77777", "DEMANDE_REFUSEE", It.Is<string>(d => d.Contains("Stagiaire"))), Times.Once);
        }

        [Fact]
        public async Task Refuser_UnRefusPeutEtreReconsidere()
        {
            var user = Compte("77777", null, AccountStates.Rejected);

            await _service.ApproveAsync("77777", new ApproveAccessRequest(RolesApp.Operateur, null));

            Assert.Equal(AccountStates.Invited, user.AccountStatus);
        }

        [Fact]
        public async Task TraiterSaPropreDemande_Refuse()
        {
            Compte(Admin, null, AccountStates.PendingApproval);
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.ApproveAsync(Admin, new ApproveAccessRequest(RolesApp.Admin, null)));
        }

        [Fact]
        public async Task RenvoyerInvitation_NouveauLienLAncienDevientInvalide()
        {
            var user = Compte("55555", RolesApp.Operateur, AccountStates.Invited);
            user.ActivationTokenHash = JetonsSecurite.Empreinte("ancien");

            var resultat = await _service.ResendInvitationAsync("55555");

            Assert.True(resultat.EmailSent);
            Assert.NotEqual(JetonsSecurite.Empreinte("ancien"), user.ActivationTokenHash);
            Assert.True(user.ActivationTokenExpiresAt > DateTime.Now.AddHours(47));
        }

        [Fact]
        public async Task RenvoyerInvitation_CompteDejaActif_Refuse()
        {
            Compte("55555", RolesApp.Operateur);
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.ResendInvitationAsync("55555"));
        }

        // ───────────── Comptes ─────────────

        [Fact]
        public async Task ChangeRole_FermeLesSessionsEtTrace()
        {
            var user = Compte("55555", RolesApp.Operateur);
            var tampon = user.SecurityStamp;

            await _service.ChangeRoleAsync("55555", "magasinier");

            Assert.Equal(RolesApp.Magasinier, user.RoleApp);
            Assert.NotEqual(tampon, user.SecurityStamp);
            _users.Verify(u => u.RevokeAllTokensForUserAsync(user.Id), Times.Once);
            _sessions.Verify(s => s.Invalidate(user.Id), Times.Once);
            _journal.Verify(j => j.LogActionAsync("55555", "ROLE_MODIFIE", It.Is<string>(d => d.Contains(Admin))), Times.Once);
        }

        [Fact]
        public async Task ChangeRole_DemandeEnAttente_PasserParApprouver()
        {
            Compte("77777", null, AccountStates.PendingApproval);
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.ChangeRoleAsync("77777", RolesApp.Operateur));
        }

        [Fact]
        public async Task ChangeRole_RoleInconnu_Refuse()
        {
            Compte("55555", RolesApp.Operateur);
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.ChangeRoleAsync("55555", "DIRECTEUR"));
            _users.Verify(u => u.UpdateUserAsync(It.IsAny<UtilisateursApp>()), Times.Never);
        }

        [Fact]
        public async Task ChangeRole_SurSonPropreCompte_Refuse()
        {
            Compte(Admin, RolesApp.Admin);
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.ChangeRoleAsync(Admin, RolesApp.Operateur));
        }

        [Fact]
        public async Task ChangeRole_DernierAdministrateur_Refuse()
        {
            Compte("77777", RolesApp.Admin);
            _administration.Setup(a => a.CountUsableAdminsAsync()).ReturnsAsync(1);

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.ChangeRoleAsync("77777", RolesApp.Operateur));
        }

        [Fact]
        public async Task ChangeRole_AutreAdministrateurRestant_Accepte()
        {
            var user = Compte("77777", RolesApp.Admin);
            _administration.Setup(a => a.CountUsableAdminsAsync()).ReturnsAsync(2);

            await _service.ChangeRoleAsync("77777", RolesApp.Operateur);

            Assert.Equal(RolesApp.Operateur, user.RoleApp);
        }

        [Fact]
        public async Task Desactiver_FermeLesSessions_ReactiverRendLAccesSansNouvelleInvitation()
        {
            var user = Compte("55555", RolesApp.Operateur);

            await _service.SetDisabledAsync("55555", true);
            Assert.Equal(AccountStates.Disabled, user.AccountStatus);
            _users.Verify(u => u.RevokeAllTokensForUserAsync(user.Id), Times.Once);

            var resultat = await _service.SetDisabledAsync("55555", false);
            Assert.Equal(AccountStates.Active, user.AccountStatus);
            Assert.False(resultat.EmailSent);
            _journal.Verify(j => j.LogActionAsync("55555", "COMPTE_REACTIVE", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Reactiver_CompteSansMotDePasse_NouvelleInvitation()
        {
            var user = Compte("55555", RolesApp.Operateur, AccountStates.Disabled);

            var resultat = await _service.SetDisabledAsync("55555", false);

            Assert.Equal(AccountStates.Invited, user.AccountStatus);
            Assert.True(resultat.EmailSent);
            Assert.NotNull(user.ActivationTokenHash);
        }

        [Fact]
        public async Task Desactiver_UneDemandeEnAttente_TransitionInterdite()
        {
            Compte("77777", null, AccountStates.PendingApproval);
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SetDisabledAsync("77777", true));
        }

        [Fact]
        public async Task Desactiver_SonPropreCompte_Refuse()
        {
            Compte(Admin, RolesApp.Admin);
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SetDisabledAsync(Admin, true));
        }

        [Fact]
        public async Task Deverrouiller_RemetLeCompteurAZero()
        {
            var user = Compte("55555", RolesApp.Operateur);
            user.FailedLoginCount = 3;
            user.LockoutEndsAt = DateTime.Now.AddMinutes(10);

            await _service.UnlockAsync("55555");

            Assert.Equal(0, user.FailedLoginCount);
            Assert.Null(user.LockoutEndsAt);
        }

        // ───────────── Rôles pré-attribués ─────────────

        [Fact]
        public async Task Preattribuer_EmployeActifSansCompte_EnregistreEtTrace()
        {
            _erp.Setup(e => e.GetEmployeByMatriculeAsync("88888")).ReturnsAsync(new EmployeErpDto("88888", "Nouveau", "X", "X", true, "n@s.tn"));
            _administration.Setup(a => a.GetPreassignedRoleDtoAsync("88888"))
                .ReturnsAsync(new PreassignedRoleDto("88888", "Nouveau", "n@s.tn", "X", RolesApp.Magasinier, "Magasinier", Admin, DateTime.Now));

            await _service.SetPreassignedRoleAsync(" 88888 ", "magasinier");

            _administration.Verify(a => a.SavePreassignedRoleAsync("88888", RolesApp.Magasinier, Admin), Times.Once);
            _journal.Verify(j => j.LogActionAsync("88888", "ROLE_PREATTRIBUE", It.Is<string>(d => d.Contains(Admin))), Times.Once);
        }

        [Fact]
        public async Task Preattribuer_MatriculeHorsErp_Refuse()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SetPreassignedRoleAsync("00000", RolesApp.Operateur));
            _administration.Verify(a => a.SavePreassignedRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Preattribuer_CompteDejaExistant_Refuse()
        {
            _erp.Setup(e => e.GetEmployeByMatriculeAsync("55555")).ReturnsAsync(new EmployeErpDto("55555", "Op", "X", "X", true, "o@s.tn"));
            Compte("55555", RolesApp.Operateur);

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SetPreassignedRoleAsync("55555", RolesApp.Magasinier));
        }

        [Fact]
        public async Task SupprimerPreattribution_Inexistante_404()
        {
            await Assert.ThrowsAsync<RessourceIntrouvableException>(() => _service.DeletePreassignedRoleAsync("88888"));
        }

        // ───────────── Lectures ─────────────

        [Fact]
        public async Task CompteInconnu_404()
        {
            await Assert.ThrowsAsync<RessourceIntrouvableException>(() => _service.UnlockAsync("00000"));
        }

        [Fact]
        public async Task Recherche_StatutInconnu_Refuse_EtPaginationBornee()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SearchUsersAsync(new UserSearchCriteria { Status = "X" }));

            var criteres = new UserSearchCriteria { Page = 0, PageSize = 5000 };
            await _service.SearchUsersAsync(criteres);
            Assert.Equal((1, 100), (criteres.Page, criteres.PageSize));
        }
    }
}
