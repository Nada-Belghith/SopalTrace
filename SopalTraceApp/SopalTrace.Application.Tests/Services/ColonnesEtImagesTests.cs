using System;
using System.Collections.Generic;
using System.Text;
using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Validators.QualityPlans;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Colonnes propres aux formulaires (types, valeurs proposées) et croquis des lignes.</summary>
    public class ColonnesEtImagesTests
    {
        private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01 };
        private static string DataUrl(string type, byte[] contenu) => $"data:{type};base64,{Convert.ToBase64String(contenu)}";

        [Fact]
        public void Image_Png_StockeeEnBinaireEtRelueEnDataUrl()
        {
            var ligne = new DocumentLigne { Id = Guid.NewGuid() };
            ligne.DefinirImage(DataUrl("image/png", Png));

            Assert.Equal("image/png", ligne.DocumentLineImage!.ContentType);
            Assert.Equal(Png, ligne.DocumentLineImage.Content);
            Assert.Equal(DataUrl("image/png", Png), ligne.EnDataUrl());
        }

        [Fact]
        public void Image_TypeDeduitDuContenuEtNonDeLEnTete()
        {
            // Un PNG annoncé comme JPEG reste enregistré comme PNG.
            Assert.Equal("image/png", ImagesLigne.Creer(DataUrl("image/jpeg", Png))!.ContentType);
        }

        [Fact]
        public void Image_SvgOuScriptDeguise_Refuse()
        {
            var svg = Encoding.UTF8.GetBytes("<svg onload=\"alert(1)\"></svg>");
            Assert.Throws<RegleMetierException>(() => ImagesLigne.Creer(DataUrl("image/svg+xml", svg)));
            Assert.Throws<RegleMetierException>(() => ImagesLigne.Creer(DataUrl("image/png", svg)));
        }

        [Fact]
        public void Image_TropLourdeOuIllisible_Refusee()
        {
            var lourde = new byte[ImagesLigne.TailleMaximaleOctets + 1];
            Png.CopyTo(lourde, 0);
            Assert.Throws<RegleMetierException>(() => ImagesLigne.Creer(DataUrl("image/png", lourde)));
            Assert.Throws<RegleMetierException>(() => ImagesLigne.Creer("data:image/png;base64,%%%"));
            Assert.False(ImagesLigne.EstAcceptee("pas une image"));
        }

        [Fact]
        public void Image_RetireeQuandLaValeurEstVide()
        {
            var ligne = new DocumentLigne { Id = Guid.NewGuid() };
            ligne.DefinirImage(DataUrl("image/png", Png));
            ligne.DefinirImage(null);
            Assert.Null(ligne.DocumentLineImage);
            Assert.Null(ligne.EnDataUrl());
        }

        [Theory]
        [InlineData("Texte", "TEXT")]
        [InlineData("Nombre", "NUMBER")]
        [InlineData("Liste", "LIST")]
        [InlineData("DATE", "DATE")]
        [InlineData(null, "TEXT")]
        public void TypeColonne_AnciensLibellesDuFrontNormalises(string? recu, string attendu)
        {
            Assert.Equal(attendu, TypesValeurColonne.Normaliser(recu));
        }

        [Fact]
        public void ColonneListe_ValeursActivesTransmisesAuFrontDansLOrdre()
        {
            var colonne = new RefFormulaireColonneDef
            {
                CleColonne = "col_etat", LabelAffiche = "État", TypeValeur = TypesValeurColonne.Liste, Actif = true,
                ColumnAllowedValues = new List<ColumnAllowedValue>
                {
                    new() { Value = "Rebut", DisplayOrder = 2, Active = true },
                    new() { Value = "Ancienne", DisplayOrder = 0, Active = false },
                    new() { Value = "Conforme", DisplayOrder = 1, Active = true }
                }
            };

            var options = ColonneJsonMapper.Deserialize(ColonneJsonMapper.Serialize(new[] { colonne }))[0].Options;
            Assert.Equal(new[] { "Conforme", "Rebut" }, options);
        }

        [Fact]
        public void ValidateurVerifMachine_RefuseLibelleTropLongEtRoleDePieceInconnu()
        {
            var demande = new CreateDocumentVerifMachineRequestDto
            {
                Nom = "VM", MachineCode = "MAS19",
                LignesRisques =
                {
                    new CreateDocumentVerifMachineLigneDto
                    {
                        TypeLigne = TypesLigneVerifMachine.Risque, LibelleRisque = new string('x', 251),
                        Echeances = { new CreateDocumentVerifMachineEcheanceDto { PeriodiciteMachineId = Guid.NewGuid(),
                            MatricePieces = { new CreateDocumentVerifMachineMatricePieceDto { RoleVerif = "XYZ" } } } }
                    }
                }
            };

            var resultat = new CreateDocumentVerifMachineRequestDtoValidator().Validate(demande);
            Assert.Equal(2, resultat.Errors.Count);
        }
    }
}
