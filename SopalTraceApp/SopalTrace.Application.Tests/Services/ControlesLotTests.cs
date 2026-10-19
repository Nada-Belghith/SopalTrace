using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Contrôles de lot par poste : chaque poste vit comme si l'OF n'en avait qu'un.</summary>
    public class ControlesLotTests
    {
        private static readonly DateTime Hier = DateTime.Today.AddDays(-1);
        private readonly ExecControleOfSession _e1Hier = new() { Id = Guid.NewGuid(), Equipe = "E1", Statut = StatutsSession.Termine };
        private readonly ExecControleOfSession _e2 = new() { Id = Guid.NewGuid(), Equipe = "E2", Statut = StatutsSession.EnCours };

        [Fact]
        public void PremierDemarrageDuPoste_DebutDOfEtDebutDePosteAttendus()
        {
            var e = ContexteEquipe.Pour(_e2, "PAS72", new[] { new DemarragePoste(_e1Hier.Id, "PAS71", Hier), new DemarragePoste(_e2.Id, "PAS72", DateTime.Now) });

            Assert.True(e.PremierDemarrage);
            Assert.True(ControlesLot.PremierePieceRequise(SectionTypes.LotOf, e));
            Assert.True(ControlesLot.PremierePieceRequise(SectionTypes.LotPoste, e));
        }

        [Fact]
        public void PosteDejaDemarreParUneEquipePrecedente_DebutDePosteSeulement()
        {
            var e = ContexteEquipe.Pour(_e2, "PAS71", new[] { new DemarragePoste(_e1Hier.Id, "PAS71", Hier), new DemarragePoste(_e2.Id, "PAS71", DateTime.Now) });

            Assert.False(ControlesLot.PremierePieceRequise(SectionTypes.LotOf, e));
            Assert.True(ControlesLot.PremierePieceRequise(SectionTypes.LotPoste, e));
        }

        [Fact]
        public void FinDePosteDemandeeSurUnPoste_NeConcernePasLesAutresPostesDeLEquipe()
        {
            var demarrages = new[]
            {
                new DemarragePoste(_e2.Id, "PAS71", DateTime.Now.AddHours(-2), "E2", ClosingRequestedAt: DateTime.Now),
                new DemarragePoste(_e2.Id, "PAS72", DateTime.Now.AddHours(-2), "E2")
            };

            var pas71 = ContexteEquipe.Pour(_e2, "PAS71", demarrages);
            var pas72 = ContexteEquipe.Pour(_e2, "PAS72", demarrages);

            Assert.Equal((DemandesCloture.FinPoste, (string?)null), (pas71.DemandeCloture, pas72.DemandeCloture));
            Assert.True(ControlesLot.DernierePieceRequise(SectionTypes.LotPoste, pas71));
            Assert.False(ControlesLot.DernierePieceRequise(SectionTypes.LotPoste, pas72));
        }

        [Fact]
        public void PosteTermineParLEquipe_SesAutresPostesContinuent()
        {
            var demarrages = new[]
            {
                new DemarragePoste(_e2.Id, "PAS71", DateTime.Now.AddHours(-2), "E2", DateTime.Now.AddMinutes(-20), DateTime.Now.AddMinutes(-10)),
                new DemarragePoste(_e2.Id, "PAS72", DateTime.Now.AddHours(-2), "E2")
            };

            var pas71 = ContexteEquipe.Pour(_e2, "PAS71", demarrages);
            var pas72 = ContexteEquipe.Pour(_e2, "PAS72", demarrages);

            Assert.True(pas71.PosteTermine);
            Assert.Equal(DemandesCloture.FinPoste, pas71.DemandeCloture);
            Assert.False(pas72.PosteTermine);
            Assert.True(demarrages[1].EnCours);
            Assert.False(demarrages[0].EnCours);
        }

        [Fact]
        public void PosteNonDemarreParLEquipe_RienEnFinDePoste()
        {
            var e = ContexteEquipe.Pour(_e2, "PAS71", new[]
            {
                new DemarragePoste(_e1Hier.Id, "PAS71", Hier),
                new DemarragePoste(_e2.Id, "PAS72", DateTime.Now, "E2", ClosingRequestedAt: DateTime.Now)
            });

            Assert.False(e.PosteDemarre);
            Assert.False(ControlesLot.PremierePieceRequise(SectionTypes.LotPoste, e));
            Assert.False(ControlesLot.DernierePieceRequise(SectionTypes.LotPoste, e));
            Assert.False(ControlesLot.DernierePieceRequise(SectionTypes.LotOf, e));
        }

        [Fact]
        public void ClotureSeule_FinDOfSurChaquePosteDejaDemarreSurLOf()
        {
            _e2.ClosingRequest = DemandesCloture.FinOf;
            var demarrages = new[] { new DemarragePoste(_e1Hier.Id, "PAS71", Hier) };

            var pas71 = ContexteEquipe.Pour(_e2, "PAS71", demarrages);
            var pas72 = ContexteEquipe.Pour(_e2, "PAS72", demarrages);

            Assert.True(ControlesLot.DernierePieceRequise(SectionTypes.LotOf, pas71));
            Assert.False(ControlesLot.DernierePieceRequise(SectionTypes.LotPoste, pas71));
            Assert.False(ControlesLot.DernierePieceRequise(SectionTypes.LotOf, pas72));
        }

        [Fact]
        public void PosteDemarreEnClotureDOf_FinDePosteEtFinDOf()
        {
            _e2.ClosingRequest = DemandesCloture.FinOf;
            var e = ContexteEquipe.Pour(_e2, "PAS71", new[] { new DemarragePoste(_e2.Id, "PAS71", DateTime.Now) });

            Assert.True(ControlesLot.DernierePieceRequise(SectionTypes.LotPoste, e));
            Assert.True(ControlesLot.DernierePieceRequise(SectionTypes.LotOf, e));
        }

        [Fact]
        public void SansSession_RienNEstAttendu()
        {
            var e = ContexteEquipe.Pour(null, "PAS71", new[] { new DemarragePoste(_e1Hier.Id, "PAS71", Hier) });

            Assert.Same(ContexteEquipe.SansSession, e);
            Assert.False(ControlesLot.PremierePieceRequise(SectionTypes.LotPoste, e));
        }
    }
}
