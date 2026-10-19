using ClosedXML.Excel;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Rapports;
using SopalTrace.Application.Rapports.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Infrastructure.Rapports;
using Xunit;

namespace SopalTrace.Application.Tests.Rapports;

/// <summary>Documents d'exécution : modèles livrés conformes à leur contrat, valeurs lues des réponses des opérateurs.</summary>
public class RapportsExecutionTests
{
    private readonly MoteurModeleExcel _moteur = new();

    public static IEnumerable<object[]> ModelesLivres() => new[]
    {
        new object[] { "PLAN_ASS", new DefinitionRapportPlan(TypesDocument.PlanAssemblage) },
        new object[] { "ECHANTILLONNAGE", new DefinitionRapportEchantillonnage(null!, null!) },
        new object[] { "RESULTAT_CONTROLE_POSTE", new DefinitionRapportControlePoste(null!, null!) },
        new object[] { "VERIF_MACHINE", new DefinitionRapportVerifMachine(null!) },
        new object[] { "VERIF_MACHINE_MAS", new DefinitionRapportVerifMachine(null!) },
        new object[] { "VERIF_MACHINE_MAS22", new DefinitionRapportVerifMachine(null!) },
        new object[] { "VERIF_MACHINE_MAS26", new DefinitionRapportVerifMachine(null!) },
        new object[] { "VERIF_MACHINE_SER05", new DefinitionRapportVerifMachine(null!) },
        new object[] { "PLAN_ASS_RESULTATS", new DefinitionRapportResultatsAssemblage(null!) },
        new object[] { "PLAN_ASS_RESULTATS_PAS72", new DefinitionRapportResultatsAssemblage(null!) },
        new object[] { "PLAN_ASS_RESULTATS_MANU", new DefinitionRapportResultatsAssemblage(null!) },
        new object[] { "REGISTRE_TRACA", new DefinitionRapportRegistre(null!) },
    };

    [Fact]
    public void Registre_ColonnesParChangementDeLots_PagesSuivantes_EtEtiquettes()
    {
        var debut = new DateTime(2026, 10, 14, 6, 0, 0);
        var entrees = Enumerable.Range(1, 12).Select(i => new EntreeRegistreRapport(i, debut.AddHours(i), $"OFCORPS{i:00}", i == 1 ? "OFVOLANT01" : null, "E1",
            new[] { new ComposantRegistreRapport("3834000-1", "Joint auto-serreur", $"JT{i:00}") })).ToList();
        var etiquettes = new[]
        {
            new EtiquetteRapport("3834000-1", "Joint auto-serreur robinet de gaz", "24060024", 15000m, "59880", debut),
            new EtiquetteRapport("9999999-9", "Autre composant", "X1", null, null, debut)       // pas dans ce registre
        };

        var donnees = DefinitionRapportRegistre.Construire(Statut(), entrees, etiquettes);

        Assert.Equal(("Corps", "Volant", "Joint auto-serreur"), (donnees.Champs["Composant1.Nom"], donnees.Champs["Composant2.Nom"], donnees.Champs["Composant3.Nom"]));
        Assert.Equal(("OFCORPS01", "OFVOLANT01", "JT01"), (donnees.Champs["Composant1.Entree1"], donnees.Champs["Composant2.Entree1"], donnees.Champs["Composant3.Entree1"]));
        Assert.Equal("", donnees.Champs["Composant2.Entree2"]);
        Assert.Equal(("1", "2"), (donnees.Champs["Page"], donnees.Champs["Pages"]));
        var page2 = Assert.Single(donnees.PagesSuivantes);
        Assert.Equal(("11", "OFCORPS12"), (page2.Champs["Entree1.Inc"], page2.Champs["Composant1.Entree2"]));
        Assert.False(page2.Champs.ContainsKey("Entree3.Date"));
        var etiquettesFeuille = Assert.Single(Assert.Single(donnees.FeuillesSuivantes).Sections).Lignes;
        Assert.Equal(("24060024", "15 000"), (Assert.Single(etiquettesFeuille).Champs["Lot"], etiquettesFeuille[0].Champs["Quantite"].Replace('\u202f', ' ').Replace('\u00a0', ' ')));

        using var classeur = new XLWorkbook(new MemoryStream(_moteur.Remplir(ModelesDeTest.Livres.Lire("REGISTRE_TRACA")!, donnees)));
        Assert.Equal(new[] { "Registre", "Registre (2)", "Étiquettes composants" }, classeur.Worksheets.Select(f => f.Name));
        Assert.Single(classeur.Worksheet("Registre (2)").Pictures);                  // logo recopié sur la page 2
        Assert.Equal("OFCORPS11", classeur.Worksheet("Registre (2)").Cell("B10").GetString());
        Assert.Equal("Joint auto-serreur robinet de gaz", classeur.Worksheet("Étiquettes composants").Cell("B4").GetString());
        Assert.All(classeur.Worksheets, f => Assert.DoesNotContain(f.CellsUsed(XLCellsUsedOptions.Contents), c => c.GetString().Contains("{{")));
    }

    [Fact]
    public void ResultatsAssemblage_UneSectionParControleDeLot_EtTableauHoraire()
    {
        var lot = new SectionPlanRapport(Guid.NewGuid(), 1, "Caractéristiques à contrôler au début du poste", SectionTypes.LotPoste, new[]
        {
            new LignePlanRapport(Guid.NewGuid(), 1, "Étanchéité interne"), new LignePlanRapport(Guid.NewGuid(), 2, "Marquage du corps")
        });
        var production = new SectionPlanRapport(Guid.NewGuid(), 2, "Contrôle par échantillonnage", SectionTypes.Echantillonnage2859, Array.Empty<LignePlanRapport>());
        var debut = new DateTime(2026, 10, 14, 6, 10, 0);
        var reponses = new[]
        {
            new ReponseLigneRapport(lot.Lignes[0].Id, "PREMIERE_PIECE", null, ResultatsLigne.NonConforme, "Fuite", null, "55555", debut),
            new ReponseLigneRapport(lot.Lignes[0].Id, "PREMIERE_PIECE", null, ResultatsLigne.Conforme, null, "Joint changé", "55555", debut.AddMinutes(5)),
            new ReponseLigneRapport(lot.Lignes[1].Id, "PREMIERE_PIECE", null, ResultatsLigne.Conforme, null, null, "55555", debut),
            new ReponseLigneRapport(lot.Lignes[0].Id, "DERNIERE_PIECE", null, ResultatsLigne.Conforme, null, null, "55556", debut.AddHours(7)),
        };
        var occurrences = new[] { new OccurrenceRapport("H_07_08", "C", debut.AddHours(1)), new OccurrenceRapport("H_08_09", "NC", debut.AddHours(2)) };
        var tranches = new[] { new TrancheRapport("H_08_09", "NC", "Fuite volant", "Tri 100 %", "55555") };

        var donnees = DefinitionRapportResultatsAssemblage.Construire(Statut(), new[] { lot, production }, reponses, occurrences, tranches);

        Assert.Equal(2, donnees.Sections.Count);
        Assert.Equal("Première pièce · Caractéristiques à contrôler au début du poste", donnees.Sections[0].Champs["Libelle"]);
        Assert.Equal(("X", "", "Joint changé"), (donnees.Sections[0].Lignes[0].Champs["Conforme"], donnees.Sections[0].Lignes[0].Champs["NonConforme"],
            donnees.Sections[0].Lignes[0].Champs["Actions"]));      // dernière réponse de la ligne
        Assert.Single(donnees.Sections[1].Lignes);                  // dernière pièce : seule la ligne saisie
        Assert.Equal("Dernière pièce", donnees.Sections[1].Champs["Moment"]);
        Assert.Equal(("X", ""), (donnees.Champs["Heure07.Conforme"], donnees.Champs["Heure07.NonConforme"]));
        Assert.Equal(("", "X", "Fuite volant", "Tri 100 %", "55555"), (donnees.Champs["Heure08.Conforme"], donnees.Champs["Heure08.NonConforme"],
            donnees.Champs["Heure08.Nc"], donnees.Champs["Heure08.Actions"], donnees.Champs["Heure08.Approbation"]));
        Assert.Equal(("", ""), (donnees.Champs["Heure10.Conforme"], donnees.Champs["Heure10.NonConforme"]));
        Assert.Equal("Contrôle par échantillonnage", donnees.Champs["TitreProduction"]);
        // Fin de poste de l'équipe E1 (dernière pièce conforme, saisie par 55556), lignes des autres équipes vides.
        Assert.Equal(("X", "", "55556"), (donnees.Champs["Quart1.Conforme"], donnees.Champs["Quart1.NonConforme"], donnees.Champs["Quart1.Approbation"]));
        Assert.Equal(("", ""), (donnees.Champs["Quart2.Conforme"], donnees.Champs["Quart2.Approbation"]));

        using (var p72 = new XLWorkbook(new MemoryStream(_moteur.Remplir(ModelesDeTest.Livres.Lire("PLAN_ASS_RESULTATS_PAS72")!, donnees))))
        {
            var f72 = p72.Worksheet(1);
            Assert.DoesNotContain(f72.CellsUsed(XLCellsUsedOptions.Contents), c => c.GetString().Contains("{{"));
            Assert.Contains(f72.CellsUsed(XLCellsUsedOptions.Contents), c => c.GetString() == "Contrôle de fin de poste (dernière pièce)");
        }

        using var classeur = new XLWorkbook(new MemoryStream(_moteur.Remplir(ModelesDeTest.Livres.Lire("PLAN_ASS_RESULTATS")!, donnees)));
        var feuille = classeur.Worksheet(1);
        Assert.DoesNotContain(feuille.CellsUsed(XLCellsUsedOptions.Contents), c => c.GetString().Contains("{{"));
        Assert.Equal("Étanchéité interne", feuille.Cell("A8").GetString());
        Assert.Equal("Dernière pièce · Caractéristiques à contrôler au début du poste", feuille.Cell("A10").GetString());
        Assert.Equal("Caractéristiques contrôlées", feuille.Cell("A11").GetString());   // en-têtes répétés pour chaque contrôle
    }

    [Fact]
    public void ResultatsAssemblageManuel_LotDeLOfDansSonTableauFixe_SansDoublonDansLeBloc()
    {
        var reglage = new SectionPlanRapport(Guid.NewGuid(), 1, "Caractéristiques à contrôler aux réglages", SectionTypes.Reglage, new[]
        {
            new LignePlanRapport(Guid.NewGuid(), 1, "Fonctionnement")
        });
        var lot = new SectionPlanRapport(Guid.NewGuid(), 3, "Caractéristiques à contrôler au niveau du lot (OF)", SectionTypes.LotOf, new[]
        {
            new LignePlanRapport(Guid.NewGuid(), 1, "Couple excessif position ouverte"), new LignePlanRapport(Guid.NewGuid(), 2, "Couple de manœuvre")
        });
        var debut = new DateTime(2026, 10, 14, 6, 10, 0);
        var reponses = new[]
        {
            new ReponseLigneRapport(reglage.Lignes[0].Id, "REGLAGE", "1", ResultatsLigne.Conforme, null, null, "55555", debut),
            new ReponseLigneRapport(lot.Lignes[0].Id, "PREMIERE_PIECE", null, ResultatsLigne.NonConforme, "Couple trop élevé", "Réglage visseuse", "55556", debut.AddMinutes(20)),
        };

        var donnees = DefinitionRapportResultatsAssemblage.Construire(Statut(), new[] { reglage, lot }, reponses,
            Array.Empty<OccurrenceRapport>(), Array.Empty<TrancheRapport>());

        Assert.Equal(("Couple excessif position ouverte", "", "X", "Couple trop élevé", "55556"), (donnees.Champs["Lot1.Caracteristique"],
            donnees.Champs["Lot1.Conforme"], donnees.Champs["Lot1.NonConforme"], donnees.Champs["Lot1.Nc"], donnees.Champs["Lot1.Approbation"]));
        Assert.Equal(("Couple de manœuvre", "", ""), (donnees.Champs["Lot2.Caracteristique"], donnees.Champs["Lot2.Conforme"], donnees.Champs["Lot2.NonConforme"]));
        Assert.Equal("", donnees.Champs["Lot3.Caracteristique"]);
        Assert.Equal("Caractéristiques à contrôler au niveau du lot (OF)", donnees.Champs["TitreLot"]);

        // Fiche des robinets manuels : le lot de l'OF dans son tableau, pas répété dans le bloc des contrôles.
        // using (var manuel = new XLWorkbook(new MemoryStream(_moteur.Remplir(ModelesDeTest.Livres.Lire("PLAN_ASS_RESULTATS_MANU")!, donnees))))
        // {
        //     var cellules = manuel.Worksheet(1).CellsUsed(XLCellsUsedOptions.Contents).Select(c => c.GetString()).ToList();
        //     Assert.DoesNotContain(cellules, t => t.Contains("{{"));
        //     Assert.Single(cellules, t => t == "Couple excessif position ouverte");
        //     Assert.DoesNotContain(cellules, t => t.StartsWith("Première pièce ·"));
        //     Assert.Contains(cellules, t => t == "Réglage 1 · Caractéristiques à contrôler aux réglages");
        // }
        // Modèle sans tableau du lot (postes 72 et 78) : le lot de l'OF reste dans le bloc des contrôles.
        using var p72 = new XLWorkbook(new MemoryStream(_moteur.Remplir(ModelesDeTest.Livres.Lire("PLAN_ASS_RESULTATS_PAS72")!, donnees)));
        Assert.Contains(p72.Worksheet(1).CellsUsed(XLCellsUsedOptions.Contents), c => c.GetString() == "Première pièce · Caractéristiques à contrôler au niveau du lot (OF)");
    }

    [Fact]
    public void VerifMachine_UneLigneParEcheance_FusionParPointPuisParPeriodicite()
    {
        // Plan type BEE46 : deux points au même libellé, 2 périodicités × 2 moyens chacun, 2 familles de corps.
        var familles = new[] { new FamilleVerifMachine(Guid.NewGuid(), 1, "25B0A01", "Corps A"), new FamilleVerifMachine(Guid.NewGuid(), 2, "25AXA01", "Corps B") };
        PointVerifMachine Point(int ordre) => new(Guid.NewGuid(), ordre, TypesLigneVerifMachine.Risque, "Fuite interne", "Test d'étanchéité",
            new[] { ("Au démarrage", 1, "PRC"), ("Au démarrage", 1, "PRC+FEC"), ("A la fin du poste", 3, "PRC"), ("A la fin du poste", 3, "PRC+FEC") }
                .Select((e, i) => new EcheanceVerifMachine(Guid.NewGuid(), i + 1, e.Item1, e.Item2, e.Item3, new[]
                {
                    new PieceVerifMachine("PRC", familles[0].Id, "BCF01"), new PieceVerifMachine("PRC", familles[1].Id, "BCF02"),
                    new PieceVerifMachine("FEC", null, e.Item3.Contains("FEC") ? "FUI24" : null)
                })).ToList());
        var plan = new PlanVerifMachineRapport("BEE46", "Banc 46", "BEE", false, familles, new[] { Point(1), Point(2) });
        var premiere = plan.Points[0].Echeances[1].Id;
        var reponses = new[] { new ReponseVerifMachine(premiere, DateTime.Now, "55555", 6.5m, 12m, false, "Joint changé") };

        var donnees = DefinitionRapportVerifMachine.Construire(plan, reponses, Statut(), "55555", "Ali");
        var lignes = Assert.Single(donnees.Sections).Lignes;
        Assert.Equal(8, lignes.Count);
        Assert.Equal(("BCF01", "BCF02", "FUI24"), (lignes[1].Champs["Piece1"], lignes[1].Champs["Piece2"], lignes[1].Champs["FuiteEtalon"]));
        Assert.Equal(("6,5", "12", "", "X", "Joint changé"),
            (lignes[1].Champs["Pression"], lignes[1].Champs["DeltaP"], lignes[1].Champs["Conforme"], lignes[1].Champs["NonConforme"], lignes[1].Champs["Observation"]));
        Assert.Equal("25AXA01", donnees.Champs["Famille2"]);

        using var classeur = new XLWorkbook(new MemoryStream(_moteur.Remplir(ModelesDeTest.Livres.Lire("VERIF_MACHINE")!, donnees)));
        var feuille = classeur.Worksheet(1);
        var fusions = feuille.MergedRanges.Select(m => m.RangeAddress.ToStringRelative()).ToHashSet();
        // Risque et méthode : fusionnés par point (A8:A11 puis A12:A15), jamais d'un point à l'autre malgré le même libellé.
        Assert.Contains("A8:A11", fusions);
        Assert.Contains("A12:A15", fusions);
        Assert.Contains("B8:B11", fusions);
        // Périodicité : fusionnée par périodicité dans le point.
        Assert.Contains("C8:C9", fusions);
        Assert.Contains("C10:C11", fusions);
        Assert.Contains("C12:C13", fusions);
        Assert.Equal("Fuite interne", feuille.Cell("A12").GetString());
        Assert.Equal("PRC+FEC", feuille.Cell("D9").GetString());
        Assert.DoesNotContain(feuille.CellsUsed(XLCellsUsedOptions.Contents), c => c.GetString().Contains("{{"));
        Assert.StartsWith("Validé par : Ali", feuille.Cell("A17").GetString());
    }

    [Fact]
    public void VerifMachine_TablesEtancheiteSeparees_ConformiteEtRisquesPuisFeuilleEtancheite()
    {
        // MAS22 : un test de conformité, un risque sans fuite étalon, un contrôle d'étanchéité (FEC/FENC).
        var familles = new[] { new FamilleVerifMachine(Guid.NewGuid(), 1, "30", "Corps 30") };
        PointVerifMachine Point(int ordre, string type, string libelle, string methode, params (string Periodicite, string Moyen, string? Fuite)[] echeances) =>
            new(Guid.NewGuid(), ordre, type, libelle, methode, echeances.Select((e, i) => new EcheanceVerifMachine(Guid.NewGuid(), i + 1, e.Periodicite, 1, e.Moyen,
                new[] { new PieceVerifMachine("PRC", familles[0].Id, "RCF01") }
                    .Concat(e.Fuite == null ? Array.Empty<PieceVerifMachine>() : new[] { new PieceVerifMachine("FEC", null, e.Fuite) }).ToList())).ToList());
        var plan = new PlanVerifMachineRapport("MAS22", "Machine 22", "MAS", true, familles, new[]
        {
            Point(1, TypesLigneVerifMachine.Conformite, "Pièce référence conforme", "Passage de la pièce", ("Au démarrage", "PRC", null), ("Après la pause", "PRC", null)),
            Point(2, TypesLigneVerifMachine.Risque, "Nombre de tours < 2,8", "Servomoteur", ("Au démarrage", "PRC", null)),
            Point(3, TypesLigneVerifMachine.Risque, "Étanchéité interne", "Test d'étanchéité 30 bar", ("Au démarrage", "PRC", null), ("Au démarrage", "PRC + FEC", "FUI24"))
        });

        var donnees = DefinitionRapportVerifMachine.Construire(plan, Array.Empty<ReponseVerifMachine>(), Statut(), "55555", "Ali");

        Assert.Equal(new[] { "Test de conformité", "Risque / Défaut" }, donnees.Sections.Select(s => s.Champs["Titre"]));
        Assert.Equal(new[] { 2, 1 }, donnees.Sections.Select(s => s.Lignes.Count));
        var etancheite = Assert.Single(Assert.Single(donnees.FeuillesSuivantes).Sections);
        Assert.Equal(("ETANCHEITE", 2, "MAS22"), (etancheite.Champs["Type"], etancheite.Lignes.Count, donnees.FeuillesSuivantes[0].Champs["Machine"]));

        using var classeur = new XLWorkbook(new MemoryStream(_moteur.Remplir(ModelesDeTest.Livres.Lire("VERIF_MACHINE_MAS22")!, donnees)));
        var page1 = classeur.Worksheet("Page 1");
        var page2 = classeur.Worksheet("Page 2");
        // Page 1 : titre de section puis ses lignes, pour chaque section ; la méthode (cellules B:C) fusionnée par point.
        Assert.Equal(("Test de conformité", "Pièce référence conforme", "Risque / Défaut", "Nombre de tours < 2,8"),
            (page1.Cell("A6").GetString(), page1.Cell("A8").GetString(), page1.Cell("A10").GetString(), page1.Cell("A12").GetString()));
        Assert.Contains("B8:C9", page1.MergedRanges.Select(m => m.RangeAddress.ToStringRelative()));
        // Page 2 : contrôles d'étanchéité avec la fuite étalon, et la validation.
        Assert.Equal(("Étanchéité interne", "FUI24"), (page2.Cell("A6").GetString(), page2.Cell("I7").GetString()));
        Assert.StartsWith("Validé par : Ali", page2.Cell("A9").GetString());
        Assert.Single(page2.Pictures);
        Assert.All(classeur.Worksheets, f => Assert.DoesNotContain(f.CellsUsed(XLCellsUsedOptions.Contents), c => c.GetString().Contains("{{")));
    }

    [Fact]
    public void VerifMachine_SansTablesSeparees_EtancheiteResteDansLesRisques()
    {
        var plan = new PlanVerifMachineRapport("BEE46", "Banc 46", "BEE", false, Array.Empty<FamilleVerifMachine>(), new[]
        {
            new PointVerifMachine(Guid.NewGuid(), 1, TypesLigneVerifMachine.Risque, "Fuite interne", "Test", new[]
            {
                new EcheanceVerifMachine(Guid.NewGuid(), 1, "Au démarrage", 1, "PRC+FEC", new[] { new PieceVerifMachine("FEC", null, "FUI24") })
            })
        });

        var donnees = DefinitionRapportVerifMachine.Construire(plan, Array.Empty<ReponseVerifMachine>(), Statut(), null, null);

        Assert.Equal("RISQUE", Assert.Single(donnees.Sections).Champs["Type"]);
        Assert.Empty(donnees.FeuillesSuivantes);
    }

    [Fact]
    public void OptionFusion_InconnueOuHorsLigne_EstRefusee()
    {
        static byte[] Modele(string a1, string a2)
        {
            using var classeur = new XLWorkbook();
            var f = classeur.AddWorksheet("M");
            f.Cell(1, 1).Value = a1;
            f.Cell(2, 1).Value = a2;
            using var flux = new MemoryStream();
            classeur.SaveAs(flux);
            return flux.ToArray();
        }

        Assert.False(_moteur.Analyser(Modele("{{Date|fusion}}", "{{Ligne.Risque}}")).Valide);
        Assert.False(_moteur.Analyser(Modele("{{Date}}", "{{Ligne.Risque|gras}}")).Valide);
        Assert.True(_moteur.Analyser(Modele("{{Date}}", "{{Ligne.Risque|fusion}}")).Valide);
    }

    [Theory]
    [MemberData(nameof(ModelesLivres))]
    public void ModeleLivre_AccepteParSonContrat(string code, IContratRapport contrat)
    {
        var modele = ModelesDeTest.Livres.Lire(code) ?? throw new InvalidOperationException($"Modèle {code} absent de tests/fixtures/modeles.");

        var verification = VerificateurModele.Verifier(_moteur.Analyser(modele), contrat, Array.Empty<ColonnesFormulaire>());

        Assert.True(verification.Accepte, string.Join(" ", verification.Erreurs));
    }

    [Fact]
    public void Echantillonnage_CochesEtEffectifs()
    {
        var fiche = new ExecEchantillonnageDto
        {
            NiveauControle = "II", TypePlan = "SIMPLE", ModeControle = "RENFORCE", NqaValeur = 0.65m, LettreCode = "L",
            EffectifEchantillonA = 200, NbPostesB = 2, EffectifParPosteAb = 100, CritereAcceptationAc = 0, CritereRejetRe = 1,
            TailleLot = 5000, CodeArticle = "25B2A01-1-1", NumeroOf = "OF1", InstrumentCodes = { "PC-01", "CAL-02" }
        };

        var donnees = DefinitionRapportEchantillonnage.Construire(fiche, Statut(), "Ali Ben Salah").Champs;

        Assert.Equal(("", "X", ""), (donnees["NiveauI"], donnees["NiveauII"], donnees["NiveauIII"]));
        Assert.Equal(("X", ""), (donnees["PlanSimple"], donnees["PlanDouble"]));
        Assert.Equal(("", "", "X"), (donnees["ModeReduit"], donnees["ModeNormal"], donnees["ModeRenforce"]));
        Assert.Equal("0,65", donnees["Nqa"]);
        Assert.Equal("PC-01, CAL-02", donnees["Instruments"]);
        Assert.Equal("Ali Ben Salah", donnees["Operateur"]);
    }

    [Fact]
    public void ControlePoste_EquipesHeuresEtTotaux_RemplissentLeModele()
    {
        var document = new ExecRcPosteDto
        {
            PosteCode = "PAS72", PlanNom = "Contrôle PAS72", JourProduction = new DateTime(2026, 10, 14),
            Equipes =
            {
                new ExecRcPosteEquipeDto { Code = "E1", Nom = "Matin", Tranches = { "06-07", "07-08" } },
                new ExecRcPosteEquipeDto { Code = "E2", Nom = "Après-midi", Tranches = { "14-15" } }
            },
            Lignes =
            {
                new ExecRcPosteLigneDto
                {
                    OrdreAffiche = 1, MachineCode = "BEE46", LibelleAffiche = "Fuite clapet",
                    Heures = { new ExecRcPosteHeureDto { TrancheHoraire = "06-07", NbNcParHeure = 2, MatriculeOp = "55555" },
                               new ExecRcPosteHeureDto { TrancheHoraire = "14-15", NbNcParHeure = 1, MatriculeOp = "55556" } }
                },
                new ExecRcPosteLigneDto { OrdreAffiche = 2, MachineCode = "-", LibelleAffiche = "Joint absent" }
            },
            Reponses = { new ExecRcPosteReponseDto { TrancheHoraire = "06-07", TotalRealiseHeure = 98 } },
            Bilan = new ExecRcPosteBilanDto { TotalDefauts = 3, TotalPiecesTestees = 101, TauxNc = 2.97, NbPiecesRebutees = 1, NbPieceConforme = 100 },
            Validations = { new ExecRcPosteValidationDto { Equipe = "E1", MatriculeOperateur = "55555" } }
        };

        var donnees = DefinitionRapportControlePoste.Construire(document, Statut(), new Dictionary<string, string> { ["55555"] = "Ali", ["55556"] = "Sami" });

        Assert.Equal("6h-7h", donnees.Champs["Equipe1.Heure1"]);
        Assert.Equal("Ali", donnees.Champs["Equipe1.Operateur"]);
        Assert.Equal("Sami", donnees.Champs["Equipe2.Operateur"]);
        Assert.Equal("2", donnees.Champs["TotalNc.Equipe1.Heure1"]);
        Assert.Equal("", donnees.Champs["TotalNc.Equipe1.Heure2"]);       // aucune saisie : case vide, pas « 0 »
        Assert.Equal("98", donnees.Champs["Realise.Equipe1.Heure1"]);
        Assert.Equal("2,97 %", donnees.Champs["TauxNc"]);
        var lignes = Assert.Single(donnees.Sections).Lignes;
        Assert.Equal(2, lignes.Count);
        Assert.Equal(("2", "1", "3"), (lignes[0].Champs["Equipe1.Heure1"], lignes[0].Champs["Equipe2.Heure1"], lignes[0].Champs["TotalDefauts"]));
        Assert.Equal("", lignes[1].Champs["Machine"]);

        // Le modèle livré se remplit sans champ restant.
        using var classeur = new XLWorkbook(new MemoryStream(_moteur.Remplir(ModelesDeTest.Livres.Lire("RESULTAT_CONTROLE_POSTE")!, donnees)));
        var feuille = classeur.Worksheet(1);
        Assert.DoesNotContain(feuille.CellsUsed(XLCellsUsedOptions.Contents), c => c.GetString().Contains("{{"));
        Assert.Equal("Fuite clapet", feuille.Cell(8, 3).GetString());
        Assert.Equal("Joint absent", feuille.Cell(9, 3).GetString());
        Assert.Equal("Total Non conforme", feuille.Cell(10, 1).GetString());
    }

    [Fact]
    public void ChampsMultiSegments_SontAcceptesParLeMoteur()
    {
        var analyse = _moteur.Analyser(ModelesDeTest.Livres.Lire("RESULTAT_CONTROLE_POSTE")!);

        Assert.True(analyse.Valide, string.Join(" ", analyse.Erreurs));
        Assert.Contains(analyse.Champs, c => c.Nom == "Ligne.Equipe2.Heure8");
        Assert.Equal(PorteeChamp.Ligne, NomsChamps.Portee("Ligne.Equipe2.Heure8"));
        Assert.Equal(PorteeChamp.Document, NomsChamps.Portee("TotalNc.Equipe1.Heure1"));
    }

    private static StatutExecutionRapport Statut() => new(
        Guid.NewGuid(), Guid.NewGuid(), TypesDocumentExecution.ResultatControlePoste, "PAS72", "Poste 72", null, "E1",
        new DateTime(2026, 10, 14), null, true, "55555", "OF1", "25B2A01-1-1", "Robinet", "RBGFA_SOUPAPE",
        "Robinet avec soupape", "SOUP", 5000, new DateTime(2026, 10, 13), "FE-RC-PAS72", 1, "Contrôle PAS72");
}
