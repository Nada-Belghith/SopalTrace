using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using Xunit;

namespace SopalTrace.Domain.Tests.Entities
{
    /// <summary>Machine à états du cycle de vie d'un compte.</summary>
    public class AccountStatesTests
    {
        [Theory]
        [InlineData(AccountStates.PendingApproval, AccountStates.Invited)]
        [InlineData(AccountStates.PendingApproval, AccountStates.Rejected)]
        [InlineData(AccountStates.Rejected, AccountStates.Invited)]
        [InlineData(AccountStates.Invited, AccountStates.Invited)]
        [InlineData(AccountStates.Invited, AccountStates.Active)]
        [InlineData(AccountStates.Invited, AccountStates.Disabled)]
        [InlineData(AccountStates.Active, AccountStates.Disabled)]
        [InlineData(AccountStates.Disabled, AccountStates.Active)]
        [InlineData(AccountStates.Disabled, AccountStates.Invited)]
        public void Transitions_autorisees(string depuis, string vers) => Assert.True(AccountStates.PeutPasser(depuis, vers));

        [Theory]
        [InlineData(AccountStates.PendingApproval, AccountStates.Active)]   // jamais actif sans invitation ni mot de passe
        [InlineData(AccountStates.PendingApproval, AccountStates.Disabled)]
        [InlineData(AccountStates.Rejected, AccountStates.Active)]
        [InlineData(AccountStates.Active, AccountStates.PendingApproval)]
        [InlineData(AccountStates.Active, AccountStates.Invited)]
        [InlineData(AccountStates.Active, AccountStates.Rejected)]
        [InlineData(AccountStates.Disabled, AccountStates.Rejected)]
        public void Transitions_interdites(string depuis, string vers) => Assert.False(AccountStates.PeutPasser(depuis, vers));

        [Fact]
        public void Nouveau_compte_est_une_demande_en_attente()
            => Assert.Equal(AccountStates.PendingApproval, new UtilisateursApp().AccountStatus);

        [Fact]
        public void Acces_accorde_sans_role_refuse()
        {
            var compte = new UtilisateursApp { Matricule = "77777" };
            var ex = Assert.Throws<RegleMetierException>(() => compte.PasserA(AccountStates.Invited));
            Assert.Contains("rôle", ex.Message);
        }

        [Fact]
        public void Compte_actif_sans_mot_de_passe_refuse()
        {
            var compte = new UtilisateursApp { Matricule = "77777", RoleApp = RolesApp.Operateur, AccountStatus = AccountStates.Invited };
            Assert.Throws<RegleMetierException>(() => compte.PasserA(AccountStates.Active));
            compte.MotDePasseHash = "hash";
            compte.PasserA(AccountStates.Active);
            Assert.Equal(AccountStates.Active, compte.AccountStatus);
        }

        [Fact]
        public void Transition_interdite_message_explicite()
        {
            var compte = new UtilisateursApp { Matricule = "55555", RoleApp = RolesApp.Operateur, AccountStatus = AccountStates.Active, MotDePasseHash = "h" };
            var ex = Assert.Throws<RegleMetierException>(() => compte.PasserA(AccountStates.Rejected));
            Assert.Contains("actif", ex.Message);
            Assert.Contains("demande refusée", ex.Message);
        }

        [Fact]
        public void Seul_un_compte_actif_peut_se_connecter()
        {
            foreach (var statut in AccountStates.All)
                Assert.Equal(statut == AccountStates.Active, AccountStates.PeutSeConnecter(statut));
        }
    }
}
