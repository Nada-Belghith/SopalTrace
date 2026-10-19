using SopalTrace.Application.Helpers;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Cotes de tronçonnage : affichées depuis l'exécution, sans modifier le plan.</summary>
    public class CotesTronconnageTests
    {
        [Theory]
        [InlineData("Longueur = 100", 123.5, null, "Longueur = 123.5")]
        [InlineData("Diamètre = 30", null, 32.0, "Diamètre = 32")]
        [InlineData("Longueur", 120.0, null, "Longueur = 120")]
        [InlineData("Aspect", 120.0, 32.0, "Aspect")]
        [InlineData("Longueur = 100", null, null, "Longueur = 100")]
        public void Appliquer_RemplaceSeulementLaCoteConcernee(string libelle, double? longueur, double? diametre, string attendu)
        {
            Assert.Equal(attendu, CotesTronconnage.Appliquer(libelle, (decimal?)longueur, (decimal?)diametre));
        }

        [Fact]
        public void Lire_ExtraitLaCoteDuPlan()
        {
            Assert.Equal(100.5m, CotesTronconnage.Lire("Longueur = 100.5"));
            Assert.Null(CotesTronconnage.Lire("Longueur"));
        }
    }
}
