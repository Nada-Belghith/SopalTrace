using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Équipes et heures du contrôle au poste, définies dans le document par le superviseur.</summary>
    public class EquipesControlePosteTests
    {
        private static readonly string[] Codes = { "E1", "E2", "E3" };

        [Fact]
        public void Tranches_EquipeDeJourEtDeNuit()
        {
            Assert.Equal(new[] { "06-07", "07-08", "08-09" }, EquipesControlePoste.Tranches(new EquipePoste("E1", "Matin", 6, 9, 1)));
            Assert.Equal(new[] { "22-23", "23-00", "00-01" }, EquipesControlePoste.Tranches(new EquipePoste("E3", "Nuit", 22, 1, 3)));
        }

        [Fact]
        public void Depuis_SansEquipesDefinies_LesTroisEquipesStandard()
        {
            Assert.Same(EquipesControlePoste.ParDefaut, EquipesControlePoste.Depuis(new List<RefFormulaireEquipe>()));

            var definies = EquipesControlePoste.Depuis(new[]
            {
                new RefFormulaireEquipe { TeamCode = "E2", NomEquipe = "Après-midi", HeureDebut = 13, HeureFin = 21, OrdreAffiche = 2, Actif = true },
                new RefFormulaireEquipe { TeamCode = "E1", NomEquipe = "Matin", HeureDebut = 5, HeureFin = 13, OrdreAffiche = 1, Actif = true },
                new RefFormulaireEquipe { TeamCode = "E3", NomEquipe = "Ancienne", HeureDebut = 21, HeureFin = 5, OrdreAffiche = 3, Actif = false }
            });
            Assert.Equal(new[] { "E1", "E2" }, definies.Select(e => e.Code));
            Assert.Equal(5, EquipesControlePoste.HeureDebutJournee(definies));
        }

        [Theory]
        [InlineData("2026-10-14T02:00:00", "2026-10-13")]
        [InlineData("2026-10-14T06:00:00", "2026-10-14")]
        [InlineData("2026-10-14T23:30:00", "2026-10-14")]
        public void JourProduction_LEquipeDeNuitResteSurLeMemeDocument(string instant, string jour)
        {
            Assert.Equal(DateTime.Parse(jour), EquipesControlePoste.JourProduction(DateTime.Parse(instant), 6));
        }

        [Theory]
        [InlineData("06-07", true)]
        [InlineData("23-00", true)]
        [InlineData("08h-09h", false)]
        [InlineData("24-01", false)]
        public void EstTranche_FormatHeureHeure(string tranche, bool attendu)
        {
            Assert.Equal(attendu, EquipesControlePoste.EstTranche(tranche));
        }

        [Fact]
        public void Erreur_ConfigurationValideOuNon()
        {
            Assert.Null(EquipesControlePoste.Erreur(EquipesControlePoste.ParDefaut.ToList(), Codes));
            Assert.Contains("couverte par deux équipes", EquipesControlePoste.Erreur(new[]
            {
                new EquipePoste("E1", "Matin", 6, 14, 1), new EquipePoste("E2", "Soir", 13, 22, 2)
            }, Codes));
            Assert.Contains("définie deux fois", EquipesControlePoste.Erreur(new[]
            {
                new EquipePoste("E1", "Matin", 6, 10, 1), new EquipePoste("E1", "Midi", 10, 14, 2)
            }, Codes));
            Assert.Contains("équipe connue", EquipesControlePoste.Erreur(new[] { new EquipePoste("E9", "Inconnue", 6, 14, 1) }, Codes));
            Assert.Contains("heures différentes", EquipesControlePoste.Erreur(new[] { new EquipePoste("E1", "Matin", 6, 6, 1) }, Codes));
        }
    }
}
