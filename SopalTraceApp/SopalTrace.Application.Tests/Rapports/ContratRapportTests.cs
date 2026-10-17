using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Rapports;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using Xunit;

namespace SopalTrace.Application.Tests.Rapports;

/// <summary>Contrat des champs du plan d'assemblage et vérification des champs d'un modèle.</summary>
public class ContratRapportTests
{
    private readonly DefinitionRapportPlan _definition = new(TypesDocument.PlanAssemblage);

    [Fact]
    public void Construire_ProduitExactementLesChampsDuContrat()
    {
        var donnees = _definition.Construire(Document());
        var produits = donnees.Champs.Keys
            .Concat(donnees.Sections.SelectMany(s => s.Champs.Keys.Select(k => NomsChamps.PrefixeSection + k)))
            .Concat(donnees.Sections.SelectMany(s => s.Lignes).SelectMany(l => l.Champs.Keys.Select(k => NomsChamps.PrefixeLigne + k)))
            .ToHashSet();

        Assert.Equal(_definition.Champs.Select(c => c.Nom).ToHashSet(), produits);
    }

    [Fact]
    public void Construire_ValeursDuDocument()
    {
        var donnees = _definition.Construire(Document());

        Assert.Equal("14/10/2026", donnees.Champs["Date"]);
        Assert.Equal("AUTO : RBGFA-BAC-01", donnees.Champs["CodeArticle"]);
        Assert.Equal("Automatique", donnees.Champs["DesignationTypeRobinet"]);
        Assert.Equal("Robinet à fermeture automatique", donnees.Champs["Designation"]);
        Assert.Equal("FE0594", donnees.Champs["CodeFormulaire"]);
        Assert.Equal("0", donnees.Champs["VersionFormulaire"]);
        var section = Assert.Single(donnees.Sections);
        Assert.Equal("Aux réglages", section.Champs["Libelle"]);
        Assert.Equal("une série de 12 pièces", section.Champs["Periodicite"]);
        var ligne = Assert.Single(section.Lignes);
        Assert.Equal("Étanchéité", ligne.Champs["Caracteristique"]);
        Assert.Equal("Banc", ligne.Champs["MoyenControle"]);
        Assert.Equal("Oui", ligne.Champs["Critique"]);
        Assert.Equal("1/h", ligne.Colonnes["FREQUENCE"]);
    }

    [Fact]
    public void Empreinte_StableEtSensibleAuxDonnees()
    {
        var document = Document();
        var empreinte = _definition.Construire(document).Empreinte();

        Assert.Equal(empreinte, _definition.Construire(Document()).Empreinte());
        Assert.Equal(64, empreinte.Length);
        document.DocumentSections.Single().DocumentLignes.Single().LimiteSpecTexte = "1 fuite";
        Assert.NotEqual(empreinte, _definition.Construire(document).Empreinte());
    }

    [Fact]
    public void Verifier_ChampInconnu_EstRefuseAvecSuggestion()
    {
        var analyse = Analyse(("Ligne.Caracteristiqe", "Cellule A8"), ("Date", "Cellule A3"));

        var resultat = VerificateurModele.Verifier(analyse, _definition, Array.Empty<ColonnesFormulaire>());

        var erreur = Assert.Single(resultat.Erreurs);
        Assert.Contains("Cellule A8", erreur);
        Assert.Contains("{{Ligne.Caracteristique}}", erreur);
    }

    [Fact]
    public void Verifier_ColonnePropre_InconnueRefusee_AbsenteDUnFormulaireSignalee()
    {
        var formulaires = new[]
        {
            new ColonnesFormulaire(Guid.NewGuid(), "FE-ASS-A", 1, "ACTIF", "EN_COURS_DE_ASSEMBLAGE", new Dictionary<string, string> { ["FREQUENCE"] = "Fréquence" }),
            new ColonnesFormulaire(Guid.NewGuid(), "FE-ASS-B", 2, "ACTIF", "EN_COURS_DE_ASSEMBLAGE", new Dictionary<string, string>())
        };

        var connue = VerificateurModele.Verifier(Analyse(("Ligne.Colonne.FREQUENCE", "Cellule G8")), _definition, formulaires);
        var inconnue = VerificateurModele.Verifier(Analyse(("Ligne.Colonne.FREQENCE", "Cellule G8")), _definition, formulaires);

        Assert.True(connue.Accepte);
        Assert.Contains(connue.Avertissements, a => a.Contains("FE-ASS-B") && a.Contains("FREQUENCE"));
        Assert.False(inconnue.Accepte);
        Assert.Contains("FREQENCE", Assert.Single(inconnue.Erreurs));
    }

    [Fact]
    public void Verifier_ColonnePropreEcriteCommeChampDeLigne_DonneLaBonneEcriture()
    {
        // Colonne « Test » ajoutée par le superviseur : {{Ligne.Test}} est refusé avec l'écriture attendue.
        var formulaire = new ColonnesFormulaire(Guid.NewGuid(), "FE-ASS-A", 3, "ACTIF", "EN_COURS_DE_ASSEMBLAGE", new Dictionary<string, string> { ["test"] = "Test" });

        var resultat = VerificateurModele.Verifier(Analyse(("Ligne.Test", "Cellule G8")), _definition, new[] { formulaire });

        var erreur = Assert.Single(resultat.Erreurs);
        Assert.Contains("Cellule G8", erreur);
        Assert.Contains("{{Ligne.Colonne.test}}", erreur);
    }

    [Fact]
    public void Verifier_ColonneAbsenteDeLaVersionVisee_EstRefusee()
    {
        // Modèle envoyé pour la version 2 du formulaire, qui n'a pas (encore) la colonne utilisée.
        var version2 = new ColonnesFormulaire(Guid.NewGuid(), "FE-ASS-A", 2, "ACTIF", "EN_COURS_DE_ASSEMBLAGE", new Dictionary<string, string> { ["FREQUENCE"] = "Fréquence" });

        var resultat = VerificateurModele.Verifier(Analyse(("Ligne.Colonne.test", "Cellule G8")), _definition, new[] { version2 });

        Assert.Contains("FE-ASS-A version 2", Assert.Single(resultat.Erreurs));
    }

    /// <summary>Analyse d'un modèle qui contient le tableau obligatoire, plus les champs donnés.</summary>
    private static AnalyseModele Analyse(params (string Nom, string Emplacement)[] champs)
    {
        var analyse = new AnalyseModele();
        analyse.Champs.Add(new ChampTrouve("Section.Libelle", "Cellule A6"));
        analyse.Champs.Add(new ChampTrouve("Ligne.Caracteristique", "Cellule A8"));
        foreach (var (nom, emplacement) in champs) analyse.Champs.Add(new ChampTrouve(nom, emplacement));
        return analyse;
    }

    [Theory]
    [InlineData("SOUP", "PAS71", "SOUP : RBGFA_SOUPAPE · poste PAS71")]
    [InlineData("AUTO", null, "AUTO : RBGFA-BAC-01")]
    public void CodeArticle_PlanDeFamille_TypeEtFamille_PosteAjoutePourLaSoupape(string type, string? poste, string attendu)
    {
        var document = new DocumentEntete
        {
            FamilleProduitFiniCode = type == "SOUP" ? "RBGFA_SOUPAPE" : "RBGFA-BAC-01",
            FamilleProduitFiniCodeNavigation = new FamilleProduitFini { TypeRobinetCode = type },
            PosteCode = poste
        };

        Assert.Equal(attendu, DefinitionRapportPlan.CodeArticle(document));
    }

    [Fact]
    public void Verifier_AutreFormulaireOuExcelVide_EstRefuse()
    {
        var sansChamp = VerificateurModele.Verifier(new AnalyseModele(), _definition, Array.Empty<ColonnesFormulaire>());
        var sansTableau = new AnalyseModele();
        sansTableau.Champs.Add(new ChampTrouve("Date", "Cellule A3"));

        Assert.False(sansChamp.Accepte);
        Assert.Contains("{{Ligne.Caracteristique}}", Assert.Single(sansChamp.Erreurs));
        Assert.False(VerificateurModele.Verifier(sansTableau, _definition, Array.Empty<ColonnesFormulaire>()).Accepte);
    }

    private static DocumentEntete Document()
    {
        var document = new DocumentEntete
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            TypeDocumentCode = TypesDocument.PlanAssemblage,
            Nom = "Plan ASS V1",
            Version = 1,
            Statut = DocumentStatuts.Actif,
            CreeLe = new DateTime(2026, 10, 1),
            ModifieLe = new DateTime(2026, 10, 14, 9, 30, 0),
            FamilleProduitFiniCode = "RBGFA-BAC-01",
            FamilleProduitFiniCodeNavigation = new FamilleProduitFini
            {
                Code = "RBGFA-BAC-01", Designation = "Robinet à fermeture automatique", TypeRobinetCode = "AUTO",
                TypeRobinetCodeNavigation = new TypeRobinet { Code = "AUTO", Libelle = "Automatique" }
            },
            Formulaire = new RefFormulaire { CodeReference = "FE0594", Version = 0, Designation = "Plan ASS" },
            Remarques = "NB"
        };
        var section = new DocumentSection
        {
            LibelleSection = "Aux réglages",
            OrdreAffiche = 1,
            Periodicite = new Periodicite { NbrPieces = 12, Unite = "SERIE" }
        };
        section.DocumentLignes.Add(new DocumentLigne
        {
            OrdreAffiche = 1,
            LibelleAffiche = "Étanchéité",
            LimiteSpecTexte = "0 fuite",
            MoyenControle = new MoyenControle { Code = "BANC", Libelle = "Banc" },
            EstCritique = true,
            DocumentLigneExtraColonnes = { new DocumentLigneExtraColonne { CleColonne = "FREQUENCE", ValeurColonne = "1/h" } }
        });
        document.DocumentSections.Add(section);
        return document;
    }
}
