using System.Collections.Generic;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.Validators.QualityPlans;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Les limites de la base sont vérifiées avant l'enregistrement (400 explicite au lieu d'une erreur technique).</summary>
    public class ValidationDocumentsQualiteTests
    {
        private static CreateDocumentRequestDto Document(string nom = "Plan PAS71", string libelleLigne = "Serrage", string cle = "col_a") => new()
        {
            TypeDocumentCode = "PLAN_ASS",
            Nom = nom,
            Sections = new List<CreateDocumentSectionDto>
            {
                new() { OrdreAffiche = 1, LibelleSection = "Contrôle", Lignes = new List<CreateDocumentLigneDto>
                {
                    new() { OrdreAffiche = 1, LibelleAffiche = libelleLigne, ExtraColonnes = new List<CreateDocumentExtraColonneDto> { new() { CleColonne = cle, ValeurColonne = "x" } } }
                } }
            }
        };

        [Fact]
        public void DocumentValide_Accepte() => Assert.True(new CreateDocumentRequestValidator().Validate(Document()).IsValid);

        [Fact]
        public void NomVide_OuTropLong_Refuse()
        {
            Assert.False(new CreateDocumentRequestValidator().Validate(Document(nom: "")).IsValid);
            Assert.False(new CreateDocumentRequestValidator().Validate(Document(nom: new string('n', 141))).IsValid);
        }

        [Fact]
        public void LigneEtColonneVerifieesJusquAuBout()
        {
            var resultat = new CreateDocumentRequestValidator().Validate(Document(libelleLigne: new string('l', 251), cle: ""));
            Assert.Contains(resultat.Errors, e => e.ErrorMessage.Contains("Libellé de la ligne"));
            Assert.Contains(resultat.Errors, e => e.ErrorMessage.Contains("Colonne : obligatoire"));
        }

        [Fact]
        public void ModeleSansCode_Refuse()
        {
            var modele = new CreateModeleRequestDto { Code = "", Libelle = "M", TypeRobinetCode = null, NatureComposantCode = "CORPS", OperationCode = "TRONC" };
            Assert.False(new CreateModeleRequestValidator().Validate(modele).IsValid);
        }
    }
}
