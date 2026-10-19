using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Calendrier des contrôles de production de l'assemblage (règles pures).</summary>
    public class CalendrierProductionTests
    {
        private static readonly DateTime Debut = new(2026, 10, 13, 8, 0, 0);
        private static readonly Guid Reglage1 = Guid.NewGuid();
        private static readonly Guid Reglage2 = Guid.NewGuid();

        private static ExecProductionPeriod Periode(double? intervalle = 4, int planifies = 0, DateTime? fin = null, string? motif = null) => new()
        {
            StartedAt = Debut, IntervalMinutes = intervalle, PlannedControlCount = planifies, EndedAt = fin, EndReason = motif, PosteCode = "PAS71"
        };

        [Theory]
        [InlineData(15, 4.0)]
        [InlineData(1, 60.0)]
        [InlineData(null, null)]
        [InlineData(0, null)]
        public void IntervalleMinutes_SoixanteSurEffectifParPoste(int? effectif, double? attendu)
        {
            Assert.Equal(attendu, CalendrierProduction.IntervalleMinutes(effectif));
        }

        [Theory]
        [InlineData(4, "HEURE", 1, 15.0)]
        [InlineData(1, "HEURE", 4, 240.0)]
        [InlineData(100, "HEURE", 1, 60.0)]
        [InlineData(5, "SERIE", null, null)]
        public void IntervalleSection_FabricationSelonLaPeriodiciteHoraire(int pieces, string unite, int? heures, double? attendu)
        {
            Assert.Equal(attendu, CalendrierProduction.IntervalleSection(new Periodicite { NbrPieces = pieces, Unite = unite, NbrHeures = heures }));
            Assert.Null(CalendrierProduction.IntervalleSection(null));
        }

        [Fact]
        public void SectionSerie_JamaisPlanifiee()
        {
            Assert.False(CalendrierProduction.EstPlanifiable(new DocumentSection { Periodicite = new Periodicite { Unite = UnitesPeriodicite.Serie } }));
            Assert.True(CalendrierProduction.EstPlanifiable(new DocumentSection { Periodicite = new Periodicite { Unite = UnitesPeriodicite.Heure } }));
            Assert.True(CalendrierProduction.EstPlanifiable(new DocumentSection()));
        }

        [Fact]
        public void ControlesDus_UnParIntervalle_ApresCeuxDejaPlanifies()
        {
            var dus = CalendrierProduction.ControlesDus(Periode(planifies: 1), Debut.AddMinutes(13));

            Assert.Equal(new[] { Debut.AddMinutes(8), Debut.AddMinutes(12) }, dus);
        }

        [Fact]
        public void ControlesDus_RienSansIntervalle()
        {
            Assert.Empty(CalendrierProduction.ControlesDus(Periode(intervalle: null), Debut.AddHours(5)));
        }

        [Fact]
        public void ProchainControle_SeulementPourUnePeriodeOuverte()
        {
            Assert.Equal(Debut.AddMinutes(12), CalendrierProduction.ProchainControle(Periode(planifies: 2)));
            Assert.Null(CalendrierProduction.ProchainControle(Periode(fin: Debut.AddMinutes(5), motif: MotifsFinPeriode.Pause)));
        }

        [Fact]
        public void FinReglage_ChaqueSectionDoitEtreConformeDepuisLeDebut()
        {
            var debut = Debut.AddMinutes(30);
            var reglages = new[] { Reglage1, Reglage2 };
            var saisies = new List<SaisieReglage>
            {
                new(Reglage1, debut.AddMinutes(-10), true),   // avant le réglage : ne compte pas
                new(Reglage1, debut.AddMinutes(2), false),
                new(Reglage2, debut.AddMinutes(3), true)
            };

            Assert.Null(CalendrierProduction.FinReglage(debut, saisies, reglages));

            saisies.Add(new(Reglage1, debut.AddMinutes(9), true));
            Assert.Equal(debut.AddMinutes(9), CalendrierProduction.FinReglage(debut, saisies, reglages));
        }

        [Fact]
        public void ReglageEnCours_TantQueLaDernierePeriodeFermeeSurReglageNEstPasConforme()
        {
            var fin = Debut.AddMinutes(20);
            var periodes = new[] { Periode(planifies: 5, fin: fin, motif: MotifsFinPeriode.Reglage) };

            Assert.True(CalendrierProduction.ReglageEnCours(periodes, new List<SaisieReglage>(), new[] { Reglage1 }));
            Assert.False(CalendrierProduction.ReglageEnCours(periodes, new List<SaisieReglage> { new(Reglage1, fin.AddMinutes(4), true) }, new[] { Reglage1 }));

            var annule = new[] { Periode(fin: fin, motif: MotifsFinPeriode.ReglageAnnule) };
            Assert.False(CalendrierProduction.ReglageEnCours(annule, new List<SaisieReglage>(), new[] { Reglage1 }));
        }

        [Fact]
        public void EnRetard_QuandLeSuivantEstPlanifieOuUnIntervalleEcoule()
        {
            var ouverte = new[] { Periode(planifies: 2) };   // contrôles à 8:04 et 8:08

            Assert.True(CalendrierProduction.EstEnRetard(Debut.AddMinutes(4), ouverte, Debut.AddMinutes(9)));
            Assert.False(CalendrierProduction.EstEnRetard(Debut.AddMinutes(8), ouverte, Debut.AddMinutes(11)));
            Assert.True(CalendrierProduction.EstEnRetard(Debut.AddMinutes(8), ouverte, Debut.AddMinutes(12)));
        }

        [Fact]
        public void EnRetard_LeTempsDePauseNeComptePas()
        {
            var enPause = new[] { Periode(planifies: 2, fin: Debut.AddMinutes(9), motif: MotifsFinPeriode.Pause) };

            Assert.False(CalendrierProduction.EstEnRetard(Debut.AddMinutes(8), enPause, Debut.AddHours(3)));
        }

        [Fact]
        public void SaisiesReglage_RegroupeesParSectionEtParSaisie()
        {
            var session = Guid.NewGuid();
            var ligneA = Guid.NewGuid();
            var ligneB = Guid.NewGuid();
            var t = Debut.AddMinutes(5);
            var reponses = new[]
            {
                new ReponseLot(ligneA, ContextesReponse.Reglage, false, t, session, "E1"),
                new ReponseLot(ligneB, ContextesReponse.Reglage, true, t, session, "E1"),
                new ReponseLot(ligneA, ContextesReponse.Reglage, false, t, Guid.NewGuid(), "E2"),       // autre équipe
                new ReponseLot(ligneA, ContextesReponse.CentPourCent, false, t, session, "E1")          // autre contrôle
            };

            var saisies = CalendrierProduction.SaisiesReglage(reponses, session,
                new Dictionary<Guid, Guid> { [ligneA] = Reglage1, [ligneB] = Reglage1 });

            var saisie = Assert.Single(saisies);
            Assert.Equal((Reglage1, t, false), (saisie.SectionId, saisie.DateSaisie, saisie.Conforme));
        }
    }
}
