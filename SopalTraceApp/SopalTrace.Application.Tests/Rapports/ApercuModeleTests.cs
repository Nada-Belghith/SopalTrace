using System;
using System.Collections.Generic;
using System.Linq;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Rapports;
using SopalTrace.Domain.Constants;
using Xunit;

namespace SopalTrace.Application.Tests.Rapports;

/// <summary>
/// Aperçu d'un modèle : le modèle vide avec l'identité du formulaire visé, jamais les données d'un document existant ni
/// des valeurs d'exemple.
/// </summary>
public class ApercuModeleTests
{
    private static readonly ColonnesFormulaire Formulaire = new(Guid.NewGuid(), "FE0594", 2, StatutsPlan.Actif, RolesFormulaire.EnCoursDeAssemblage,
        new Dictionary<string, string> { ["frequence"] = "Fréquence", ["moyen"] = "Moyen" });

    [Fact]
    public void AvecFormulaire_SeulesSonIdentiteEtDesLignesVides()
    {
        var donnees = DonneesApercu.Construire(new DefinitionRapportPlan(TypesDocument.PlanAssemblage), Formulaire, "Plan d'assemblage RBGFM PAS71");

        Assert.Equal(("FE0594", "2", "Plan d'assemblage RBGFM PAS71"),
            (donnees.Champs["CodeFormulaire"], donnees.Champs["VersionFormulaire"], donnees.Champs["DesignationFormulaire"]));
        Assert.All(donnees.Champs.Where(c => !c.Key.EndsWith("Formulaire", StringComparison.Ordinal)), c => Assert.Equal("", c.Value));

        var section = Assert.Single(donnees.Sections);
        Assert.All(section.Champs.Values, v => Assert.Equal("", v));
        Assert.Equal(DonneesApercu.LignesVides, section.Lignes.Count);
        Assert.All(section.Lignes, l =>
        {
            Assert.All(l.Champs.Values, v => Assert.Equal("", v));
            Assert.Equal(new[] { "frequence", "moyen" }, l.Colonnes.Keys.OrderBy(k => k, StringComparer.Ordinal));
            Assert.All(l.Colonnes.Values, v => Assert.Equal("", v));
        });
    }

    [Fact]
    public void SansFormulaire_ModeleEntierementVide()
    {
        var donnees = DonneesApercu.Construire(new DefinitionRapportPlan(TypesDocument.PlanAssemblage), null, null);

        Assert.All(donnees.Champs.Values, v => Assert.Equal("", v));
        Assert.All(donnees.Sections.SelectMany(s => s.Lignes), l => Assert.Empty(l.Colonnes));
    }
}
