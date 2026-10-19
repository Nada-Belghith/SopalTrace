using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Constants;
using Xunit;

namespace SopalTrace.Application.Tests.Helpers;

/// <summary>
/// Champ d'application du formulaire d'un document : seuls les critères qui existent pour son type ; le code du
/// formulaire n'est jamais déduit.
/// </summary>
public class ChampFormulaireTests
{
    [Fact]
    public void PlanAssemblage_FamilleEtPoste_LaNatureNeCompteQueSansFamille()
    {
        Assert.Equal(new ChampFormulaire(RolesFormulaire.EnCoursDeAssemblage, "RBGFA_SOUPAPE", "PAS72", null, null),
            ChampsFormulaire.PourDocument(TypesDocument.PlanAssemblage, "RBGFA_SOUPAPE", " PAS72 ", null, "PF"));
        // Piston : pas de famille, sa nature désigne son formulaire.
        Assert.Equal(new ChampFormulaire(RolesFormulaire.EnCoursDeAssemblage, null, null, null, "PISTON"),
            ChampsFormulaire.PourDocument(TypesDocument.PlanAssemblage, "", null, null, "PISTON"));
    }

    [Fact]
    public void AutresTypes_SeulsLeursCriteres()
    {
        Assert.Equal(new ChampFormulaire(RolesFormulaire.VerifMachine, null, null, "MAS22", null),
            ChampsFormulaire.PourDocument(TypesDocument.VerifMachine, "RBGFA", "PAS71", "MAS22", "PF"));
        Assert.Equal(new ChampFormulaire(RolesFormulaire.ResultatControlePoste, "RBGFA_SOUPAPE", "PAS71", null, null),
            ChampsFormulaire.PourDocument(TypesDocument.ControlePoste, "RBGFA_SOUPAPE", "PAS71", null, null));
        Assert.Equal(new ChampFormulaire(RolesFormulaire.ProduitFini, "RBGFM", null, null, null),
            ChampsFormulaire.PourDocument(TypesDocument.PlanProduitFini, "RBGFM", "PAS71", null, null));
        Assert.Equal(new ChampFormulaire(RolesFormulaire.Echantillonnage, null, null, null, null),
            ChampsFormulaire.PourDocument(TypesDocument.PlanEchantillonnage, "X", "Y", "Z", "W"));
        Assert.Null(ChampsFormulaire.PourDocument(TypesDocument.PlanFabrication, "X", null, null, null));
    }

    [Fact]
    public void Description_LisiblePourLesMessages()
    {
        Assert.Equal("EN_COURS_DE_ASSEMBLAGE (famille RBGFA_SOUPAPE, poste PAS78)",
            new ChampFormulaire(RolesFormulaire.EnCoursDeAssemblage, "RBGFA_SOUPAPE", "PAS78", null, null).ToString());
        // Le type de robinet d'une famille est le sien : il n'est cité que sans famille.
        Assert.Equal("RESULTAT_CONTROLE_ASSEMBLAGE (famille RBGFA_SOUPAPE, poste PAS71)",
            new ChampFormulaire(RolesFormulaire.ResultatControleAssemblage, "RBGFA_SOUPAPE", "PAS71", null, null, "SOUP").ToString());
        Assert.Equal("RESULTAT_CONTROLE_ASSEMBLAGE (poste PAS71, type de robinet SOUP)",
            new ChampFormulaire(RolesFormulaire.ResultatControleAssemblage, null, "PAS71", null, null, "SOUP").ToString());
    }
}
