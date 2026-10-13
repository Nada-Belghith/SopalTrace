using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Exceptions;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    public class EtiquetteMagasinTests
    {
        [Theory]
        [InlineData("lot:L123/cd:M1180RP/Rpt:QC-9/qty:250", "M1180RP", "L123", "QC-9", 250.0)]
        [InlineData("cd:M1180RP | lot:L123", "M1180RP", "L123", null, null)]
        [InlineData("M1180RP;L123;QC-9;12,5", "M1180RP", "L123", "QC-9", 12.5)]
        [InlineData("M1180RP;L123", "M1180RP", "L123", null, null)]
        [InlineData("]Q1M1180RP;L123\r\n", "M1180RP", "L123", null, null)]
        public void LireComposant_FormatsAcceptes(string scan, string article, string lot, string? rapport, double? quantite)
        {
            var e = EtiquetteMagasin.LireComposant(scan);

            Assert.Equal((article, lot, rapport), (e.CodeArticle, e.NumeroLot, e.RapportControle));
            Assert.Equal(quantite.HasValue ? (decimal)quantite.Value : null, e.Quantite);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   \r\n")]
        [InlineData("M1180RP")]
        [InlineData("lot:L123")]
        [InlineData("a;b;c;d;e")]
        [InlineData("M1180RP;L123;QC;-5")]
        [InlineData("M1180RP;L123;QC;abc")]
        public void LireComposant_FormatRefuse(string scan)
        {
            Assert.Throws<EtiquetteIllisibleException>(() => EtiquetteMagasin.LireComposant(scan));
        }

        [Fact]
        public void DouchetteEnQwerty_MessageExplicite()
        {
            // « 26070 » tapé par une douchette QWERTY sur un poste AZERTY donne « é§çà ».
            var ex = Assert.Throws<EtiquetteIllisibleException>(() => EtiquetteMagasin.LireNumeroOf("éçàà"));
            Assert.Contains("QWERTY", ex.Message);
        }

        [Theory]
        [InlineData("OF2607ASS01", "OF2607ASS01")]
        [InlineData("of:OF2607ASS01", "OF2607ASS01")]
        [InlineData(" OF2607ASS01\t", "OF2607ASS01")]
        public void LireNumeroOf(string scan, string attendu) => Assert.Equal(attendu, EtiquetteMagasin.LireNumeroOf(scan));

        [Theory]
        [InlineData("OF 26 07")]
        [InlineData("OF;2607")]
        [InlineData("OF26070000000000000000000000000000")]
        public void LireNumeroOf_Invalide(string scan) => Assert.Throws<EtiquetteIllisibleException>(() => EtiquetteMagasin.LireNumeroOf(scan));
    }
}
