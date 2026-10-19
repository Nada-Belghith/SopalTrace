using System;
using System.Collections.Generic;
using SopalTrace.Application.Rapports;
using SopalTrace.Domain.Constants;
using Xunit;

namespace SopalTrace.Application.Tests.Rapports;

/// <summary>
/// Modèle d'un document : celui de la version de son formulaire en vigueur à sa création (ordre de la base). Les
/// versions consécutives aux mêmes colonnes partagent leurs modèles ; une version dont les colonnes changent n'hérite
/// de rien : son modèle doit être rattaché.
/// </summary>
public class ResolutionModeleRapportTests
{
    private const string Colonnes = "FREQUENCE|Fréquence|TEXT";
    private const string AvecTest = "FREQUENCE|Fréquence|TEXT\nTEST|Test|TEXT";

    // Formulaire FE-ASS-SOUP : V1 et V2 mêmes colonnes ; V3 ajoute une colonne ; V4 mêmes colonnes que V3. Lignes reliées
    // par leur groupe, dans leur ordre de création ; FE0816.1 est l'ancien code de la version 1.
    private static readonly Guid Formulaire = Guid.NewGuid();
    private static readonly VersionFormulaire V1 = new(Guid.NewGuid(), "FE0816", 1, Colonnes, Formulaire, 1);
    private static readonly VersionFormulaire V2 = new(Guid.NewGuid(), "FE-ASS-SOUP", 2, Colonnes, Formulaire, 3);
    private static readonly VersionFormulaire V3 = new(Guid.NewGuid(), "FE-ASS-SOUP", 3, AvecTest, Formulaire, 5);
    private static readonly VersionFormulaire V4 = new(Guid.NewGuid(), "FE-ASS-SOUP", 4, AvecTest, Formulaire, 7);
    // Autre formulaire, même s'il porte un code proche : jamais d'héritage.
    private static readonly VersionFormulaire Autre = new(Guid.NewGuid(), "FE0816", 2, Colonnes, Guid.NewGuid(), 2);
    private static readonly List<VersionFormulaire> Versions = new() { V1, V2, V3, V4, Autre };

    private static AffectationModele Structure(int version, VersionFormulaire formulaire, long ordre) =>
        new(Guid.NewGuid(), "PLAN_ASS", version, TypesAffectationModele.StructureFormulaire, formulaire.Id, ordre);
    private static AffectationModele Revision(int version, VersionFormulaire formulaire, long ordre) =>
        new(Guid.NewGuid(), "PLAN_ASS", version, TypesAffectationModele.RevisionFormulaire, formulaire.Id, ordre);
    private static AffectationModele ParDefaut(int version, long ordre) =>
        new(Guid.NewGuid(), "REGISTRE_TRACA", version, TypesAffectationModele.ParDefaut, null, ordre);

    private static ResolutionModele Document(long ordre, VersionFormulaire formulaire, params AffectationModele[] affectations) =>
        ResolutionModeleRapport.PourFormulaire(ordre, formulaire, Versions, affectations);

    [Fact]
    public void ModeleDuFormulaire_EnVigueurALaCreation_UnModeleRattacheApresNeChangeRien()
    {
        var a = Structure(1, V1, 10);
        var revision = Revision(2, V1, 30);

        Assert.Equal((1, RegleModele.EnVigueurALaCreation), Version(Document(20, V1, a, revision)));
        Assert.Equal((2, RegleModele.EnVigueurALaCreation), Version(Document(40, V1, a, revision)));
    }

    [Fact]
    public void MemesColonnes_LaNouvelleVersionGardeLeModeleDeLaPrecedente()
    {
        var a = Structure(1, V1, 10);

        Assert.Equal((1, RegleModele.HeriteDeLaVersionPrecedente), Version(Document(50, V2, a)));
        // Un autre formulaire n'hérite jamais.
        Assert.Equal(ModeleManquant.AucunModele, Document(50, Autre, a).Manque);
    }

    [Fact]
    public void MemesColonnes_LeSuperviseurPeutRattacherUnNouveauModeleALaNouvelleVersion()
    {
        var a = Structure(1, V1, 10);
        var pourV2 = Structure(4, V2, 60);

        Assert.Equal(4, Version(Document(70, V2, a, pourV2)).Numero);
        // La version 1 garde son modèle.
        Assert.Equal(1, Version(Document(70, V1, a, pourV2)).Numero);
    }

    [Fact]
    public void ColonnesChangees_AucunHeritage_ModeleARattacher_PuisIlVautPourLesDocumentsDejaCrees()
    {
        var a = Structure(1, V1, 10);

        var sansModele = Document(50, V3, a);
        Assert.Null(sansModele.Modele);
        Assert.Equal(ModeleManquant.StructureModifiee, sansModele.Manque);

        // Le modèle mis à jour est rattaché à V3 après la création du document (ordre 60) : il lui revient.
        var pourV3 = Structure(5, V3, 60);
        Assert.Equal((5, RegleModele.ConcuPourLaVersionDuFormulaire), Version(Document(50, V3, a, pourV3)));
        // V4 (mêmes colonnes que V3) en hérite ; elle n'hérite jamais du modèle de V1.
        Assert.Equal((5, RegleModele.HeriteDeLaVersionPrecedente), Version(Document(80, V4, a, pourV3)));
        Assert.Equal(ModeleManquant.StructureModifiee, Document(80, V3, a).Manque);
    }

    [Fact]
    public void Revision_SeulementPourLesDocumentsCreesApres()
    {
        var pourV3 = Structure(5, V3, 40);
        var logo = Revision(6, V3, 70);

        Assert.Equal(5, Version(Document(60, V3, pourV3, logo)).Numero);
        Assert.Equal(6, Version(Document(90, V3, pourV3, logo)).Numero);
        // Une révision rattachée à une version sans modèle propre ne touche pas ses documents déjà créés.
        Assert.Equal(1, Version(Document(60, V2, Structure(1, V1, 10), Revision(7, V2, 70))).Numero);
    }

    [Fact]
    public void DocumentCreeAvantToutModele_PremierModeleDuGroupeDeMemesColonnes()
    {
        // Documents de V1 créés avant le rattachement de départ, fait sur la version en cours V2 (mêmes colonnes).
        var depart = Structure(1, V2, 30);

        Assert.Equal((1, RegleModele.PremierModele), Version(Document(5, V1, depart)));
        // V3 (autres colonnes) ne le reçoit pas.
        Assert.Equal(ModeleManquant.StructureModifiee, Document(5, V3, depart).Manque);
    }

    [Fact]
    public void ChangementDeCode_MemeVersionMemesColonnes_LeNouveauCodeHeriteDuModele()
    {
        // FEMANU.1 (ordre 10) remplacé par DEMANU.1 (ordre 20, même version, mêmes colonnes) ; logo : DEMANU.2 (ordre 40).
        var groupe = Guid.NewGuid();
        var femanu1 = new VersionFormulaire(Guid.NewGuid(), "FEMANU", 1, Colonnes, groupe, 10);
        var demanu1 = new VersionFormulaire(Guid.NewGuid(), "DEMANU", 1, Colonnes, groupe, 20);
        var demanu2 = new VersionFormulaire(Guid.NewGuid(), "DEMANU", 2, Colonnes, groupe, 40);
        var lignes = new List<VersionFormulaire> { femanu1, demanu1, demanu2 };
        var depart = Structure(1, femanu1, 5);
        var nouveauLogo = Structure(2, demanu2, 35);

        Assert.Equal((1, RegleModele.EnVigueurALaCreation), Version(ResolutionModeleRapport.PourFormulaire(12, femanu1, lignes, new[] { depart, nouveauLogo })));
        Assert.Equal((1, RegleModele.HeriteDeLaVersionPrecedente), Version(ResolutionModeleRapport.PourFormulaire(25, demanu1, lignes, new[] { depart, nouveauLogo })));
        Assert.Equal((2, RegleModele.EnVigueurALaCreation), Version(ResolutionModeleRapport.PourFormulaire(41, demanu2, lignes, new[] { depart, nouveauLogo })));
    }

    [Fact]
    public void GroupeDeMemesColonnes_VersionsConsecutivesSeulement()
    {
        Assert.Equal(new[] { V1, V2 }, ResolutionModeleRapport.GroupeMemesColonnes(V2, Versions));
        Assert.Equal(new[] { V3, V4 }, ResolutionModeleRapport.GroupeMemesColonnes(V3, Versions));
    }

    [Fact]
    public void SansFormulaire_ModeleParDefautEnVigueur_SinonLePremier()
    {
        var v1 = ParDefaut(1, 10);
        var v2 = ParDefaut(2, 30);

        Assert.Equal(1, Version(ResolutionModeleRapport.SansFormulaire(20, new[] { v1, v2 })).Numero);
        Assert.Equal(2, Version(ResolutionModeleRapport.SansFormulaire(40, new[] { v1, v2 })).Numero);
        Assert.Equal((1, RegleModele.PremierModele), Version(ResolutionModeleRapport.SansFormulaire(5, new[] { v1, v2 })));
        Assert.Equal(ModeleManquant.SansFormulaire, ResolutionModeleRapport.SansFormulaire(5, Array.Empty<AffectationModele>()).Manque);
    }

    [Fact]
    public void ModeleActuel_CeluiDUnDocumentCreeMaintenant()
    {
        var actuel = Document(ResolutionModeleRapport.Maintenant, V2, Structure(1, V1, 10), Revision(2, V1, 20));

        Assert.Equal((2, RegleModele.HeriteDeLaVersionPrecedente), Version(actuel));
    }

    private static (int Numero, RegleModele Regle) Version(ResolutionModele resolution)
    {
        Assert.NotNull(resolution.Modele);
        return (resolution.Modele!.Affectation.NumeroVersion, resolution.Modele.Regle);
    }
}
