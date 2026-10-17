using System;
using System.Linq;
using SopalTrace.Application.Rapports.Execution;
using SopalTrace.Domain.Constants;
using Xunit;

namespace SopalTrace.Application.Tests.Rapports;

/// <summary>
/// Fiche vierge d'un document qualité : les données du document (en-tête, sections, caractéristiques, paramètres) avec
/// le formulaire de la fiche ; tout ce que remplissent la production et les opérateurs reste vide.
/// </summary>
public class FichesViergesTests
{
    // En-tête d'une fiche jamais remplie : poste, famille et nom du document, sans OF ni article ni équipe.
    private static StatutExecutionRapport Vierge(string type, string codeFormulaire, int version) => new(
        Guid.Empty, Guid.Empty, type, "PAS71", "Poste 71", null, null, null, null, false, null, "", null, null,
        "RBGFA_SOUPAPE", "Robinet avec soupape", "SOUP", 0m, null, codeFormulaire, version, "Plan ASS RBGFA_SOUPAPE PAS71 V1");

    [Fact]
    public void Echantillonnage_ParametresDuPlan_TailleDuLotEtEffectifsVides()
    {
        var donnees = DefinitionRapportEchantillonnage.ConstruireVierge(
            new ParametresEchantillonnageRapport("II", "SIMPLE", "NORMAL", 0.65m, 1, 2), Vierge(TypesDocumentExecution.Echantillonnage, "FE0591", 0));

        Assert.Equal(("X", "X", "X"), (donnees.Champs["NiveauII"], donnees.Champs["PlanSimple"], donnees.Champs["ModeNormal"]));
        Assert.Equal(("0,65", "1", "2"), (donnees.Champs["Nqa"], donnees.Champs["CritereAcceptation"], donnees.Champs["CritereRejet"]));
        foreach (var champ in new[] { "TailleLot", "EffectifEchantillon", "NombrePostes", "EffectifParPoste", "LettreCode", "NumeroOf", "Operateur", "Equipe" })
            Assert.Equal("", donnees.Champs[champ]);
        Assert.Equal(("FE0591", "0"), (donnees.Champs["CodeFormulaire"], donnees.Champs["VersionFormulaire"]));
    }

    [Fact]
    public void VerificationMachineSer_ParRisquePuisPeriodicite_LesMethodesSousChaquePeriodicite()
    {
        // SER05 : un risque, deux méthodes (PRC, PRNC), trois périodicités chacune.
        var famille = new FamilleVerifMachine(Guid.NewGuid(), 1, "C_30_35", "Corps (30, 35)");
        EcheanceVerifMachine Echeance(int ordre, string periodicite, string moyen, string piece) => new(Guid.NewGuid(), ordre, periodicite, ordre, moyen,
            new[] { new PieceVerifMachine(moyen, famille.Id, piece) });
        PointVerifMachine Methode(int ordre, string methode, string moyen, string piece) => new(Guid.NewGuid(), ordre, "RISQUE",
            "Passage d'une pièce bouchée", methode, new[] { Echeance(1, "Au démarrage", moyen, piece), Echeance(2, "Après la pause", moyen, piece),
                Echeance(3, "A la fin du poste", moyen, piece) });
        var plan = new PlanVerifMachineRapport("SER05", "Marquage 05", "SER", false, new[] { famille },
            new[] { Methode(1, "Passage de la pièce conforme", "PRC", "RCF05"), Methode(2, "Pas de passage de la pièce non conforme", "PRNC", "RCN04") });

        var lignes = DefinitionRapportVerifMachine.Construire(plan, Array.Empty<ReponseVerifMachine>(),
            Vierge(TypesDocumentExecution.VerifMachine, "FE0798", 0), null, null).Sections.Single().Lignes;

        Assert.Equal(new[] { "Au démarrage|PRC", "Au démarrage|PRNC", "Après la pause|PRC", "Après la pause|PRNC", "A la fin du poste|PRC", "A la fin du poste|PRNC" },
            lignes.Select(l => $"{l.Champs["Periodicite"]}|{l.Champs["Moyen"]}"));
        Assert.Single(lignes.Select(l => l.Champs["Cle.Risque"]).Distinct());                       // une case pour le risque
        Assert.Equal(3, lignes.Select(l => l.Champs["Cle.Periodicite"]).Distinct().Count());        // une case par périodicité (2 lignes)
        Assert.Equal(6, lignes.Select(l => l.Champs["Cle.Methode"]).Distinct().Count());            // la méthode sur chaque ligne
        Assert.True(DefinitionRapportVerifMachine.ParPeriodicite(plan));
        Assert.False(DefinitionRapportVerifMachine.ParPeriodicite(plan with { ModeleVerification = "BEE" }));
    }

    [Fact]
    public void ResultatsAssemblage_ToutesLesSectionsDeControleDuPlan_CasesVides_SansLaSectionDeProduction()
    {
        var sections = new[]
        {
            new SectionPlanRapport(Guid.NewGuid(), 1, "Contrôle lot OF (1ère et dernière pièce)", SectionTypes.LotOf,
                new[] { new LignePlanRapport(Guid.NewGuid(), 1, "Étanchéité 0,1 bar"), new LignePlanRapport(Guid.NewGuid(), 2, "Serrage") }),
            new SectionPlanRapport(Guid.NewGuid(), 2, "Contrôle par échantillonnage (ISO 2859-1)", SectionTypes.Echantillonnage2859,
                new[] { new LignePlanRapport(Guid.NewGuid(), 1, "Aspect") }),
            new SectionPlanRapport(Guid.NewGuid(), 3, "Contrôle aux réglages", SectionTypes.Reglage,
                new[] { new LignePlanRapport(Guid.NewGuid(), 1, "Couple") })
        };

        var donnees = DefinitionRapportResultatsAssemblage.ConstruireVierge(Vierge(TypesDocumentExecution.PlanAssemblage, "FE0816", 1), sections);

        Assert.Equal(new[] { "Contrôle lot OF (1ère et dernière pièce)", "Contrôle aux réglages" },
            donnees.Sections.ConvertAll(s => s.Champs["Libelle"]));
        Assert.Equal(new[] { "Étanchéité 0,1 bar", "Serrage" }, donnees.Sections[0].Lignes.ConvertAll(l => l.Champs["Caracteristique"]));
        Assert.All(donnees.Sections[0].Lignes, l => Assert.Equal(("", "", ""), (l.Champs["Conforme"], l.Champs["NonConforme"], l.Champs["Approbation"])));
        Assert.Equal("Contrôle par échantillonnage (ISO 2859-1)", donnees.Champs["TitreProduction"]);
        Assert.Equal(("", "", "PAS71"), (donnees.Champs["Heure07.Conforme"], donnees.Champs["Quart1.Conforme"], donnees.Champs["Poste"]));
        Assert.Equal(("FE0816", "1", ""), (donnees.Champs["CodeFormulaire"], donnees.Champs["VersionFormulaire"], donnees.Champs["NumeroOf"]));
    }
}
