using ClosedXML.Excel;
using SopalTrace.Application.Rapports;
using SopalTrace.Infrastructure.Rapports;
using Xunit;

namespace SopalTrace.Application.Tests.Rapports;

/// <summary>Moteur des modèles Excel, sur le modèle livré du plan d'assemblage (FE0594) et sur des modèles construits.</summary>
public class MoteurModeleExcelTests
{
    private readonly MoteurModeleExcel _moteur = new();
    private static byte[] ModelePlanAssemblage => ModelesDeTest.Livres.Lire("PLAN_ASS") ?? throw new InvalidOperationException("Modèle PLAN_ASS absent de tests/fixtures/modeles.");

    [Fact]
    public void ModeleLivre_EstValide_EtSesChampsSontTrouves()
    {
        var analyse = _moteur.Analyser(ModelePlanAssemblage);

        Assert.True(analyse.Valide, string.Join(" ", analyse.Erreurs));
        var noms = analyse.Champs.Select(c => c.Nom).ToHashSet();
        Assert.Contains("Date", noms);
        Assert.Contains("Section.Libelle", noms);
        Assert.Contains("Ligne.Caracteristique", noms);
        Assert.Contains(analyse.Champs, c => c.Nom == "CodeFormulaire" && c.Emplacement == "Pied de page (droite)");
    }

    [Fact]
    public void Remplir_TexteLongDansUneCellule_RetourALaLigneEtLigneAgrandie()
    {
        var donnees = Donnees(("Section", 2));
        donnees.Sections[0].Lignes[0].Champs["Caracteristique"] = string.Join(" ", Enumerable.Repeat("Suivant plan du corps du produit et Défauthèque", 4));

        using var classeur = Ouvrir(_moteur.Remplir(ModelePlanAssemblage, donnees));
        var feuille = classeur.Worksheet(1);

        // Ligne au texte long (8) : retour à la ligne, plus haute que la ligne au texte court (9), qui reste telle quelle.
        Assert.True(feuille.Cell(8, 1).Style.Alignment.WrapText);
        Assert.True(feuille.Row(8).Height > feuille.Row(9).Height + 10, $"{feuille.Row(8).Height} / {feuille.Row(9).Height}");
    }

    [Fact]
    public void Remplir_ToujoursSurLaLargeurDUnePage()
    {
        using var classeur = Ouvrir(_moteur.Remplir(ModelePlanAssemblage, Donnees(("Section", 1))));

        Assert.Equal(1, classeur.Worksheet(1).PageSetup.PagesWide);
        Assert.Equal(0, classeur.Worksheet(1).PageSetup.PagesTall);
    }

    [Fact]
    public void Remplir_ColonneAjouteeHorsZoneDImpression_ZoneElargie()
    {
        // Modèle dont on a ajouté une colonne « Test » à droite sans élargir la zone d'impression.
        byte[] modele;
        int colonneAjoutee;
        using (var source = Ouvrir(ModelePlanAssemblage))
        {
            var feuille = source.Worksheet(1);
            colonneAjoutee = feuille.LastColumnUsed()!.ColumnNumber() + 1;
            feuille.Cell(7, colonneAjoutee).Value = "Test";
            feuille.PageSetup.PrintAreas.Clear();
            feuille.PageSetup.PrintAreas.Add(1, 1, 9, colonneAjoutee - 1);
            using var flux = new MemoryStream();
            source.SaveAs(flux);
            modele = flux.ToArray();
        }

        Assert.Contains(_moteur.Analyser(modele).Avertissements, a => a.Contains("zone d'impression s'arrête"));
        using var classeur = Ouvrir(_moteur.Remplir(modele, Donnees(("Section", 1))));
        Assert.Equal(colonneAjoutee, classeur.Worksheet(1).PageSetup.PrintAreas.Single().RangeAddress.LastAddress.ColumnNumber);
    }

    [Fact]
    public void Remplir_RepeteSectionsEtLignes_EtDecaleLaSuite()
    {
        var donnees = Donnees(("Aux réglages", 2), ("Sans ligne", 0), ("En cours", 3));

        using var classeur = Ouvrir(_moteur.Remplir(ModelePlanAssemblage, donnees));
        var feuille = classeur.Worksheet(1);

        // Bloc du modèle : titre (6), en-têtes (7), détail (8) ; NB (9) en dessous.
        Assert.Equal("Aux réglages", feuille.Cell(6, 1).GetString());
        Assert.True(feuille.Cell(6, 1).IsMerged());
        Assert.Equal("Caractéristique contrôlée", feuille.Cell(7, 1).GetString());
        Assert.Equal("S1 L1", feuille.Cell(8, 1).GetString());
        Assert.Equal("Limite S1 L2", feuille.Cell(9, 2).GetString());
        Assert.Equal("Sans ligne", feuille.Cell(10, 1).GetString());
        Assert.Equal("", feuille.Cell(12, 1).GetString());          // ligne de détail vide
        Assert.Equal("Thin", feuille.Cell(12, 1).Style.Border.LeftBorder.ToString()); // mais encadrée
        Assert.Equal("En cours", feuille.Cell(13, 1).GetString());
        Assert.Equal("S3 L3", feuille.Cell(17, 1).GetString());
        Assert.Equal("NB : vérifier les lots.", feuille.Cell(18, 1).GetString());
        Assert.True(feuille.Cell(18, 1).IsMerged());

        Assert.Equal("Date : 14/10/2026", feuille.Cell(3, 1).GetString());
        Assert.Equal("$A$1:$F$18", feuille.PageSetup.PrintAreas.Single().RangeAddress.ToStringFixed());
        Assert.Contains("FE0594.0", feuille.PageSetup.Footer.Right.GetText(XLHFOccurrence.OddPages));
        Assert.DoesNotContain(feuille.CellsUsed(XLCellsUsedOptions.Contents), c => c.GetString().Contains("{{"));
        Assert.Single(classeur.Worksheets);
        Assert.Single(feuille.Pictures);
    }

    [Fact]
    public void Remplir_ConserveLaTailleDuLogo()
    {
        static string Etendue(byte[] classeur)
        {
            using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(classeur));
            var dessin = archive.Entries.Single(e => e.FullName.StartsWith("xl/drawings/drawing") && e.FullName.EndsWith(".xml"));
            using var lecteur = new StreamReader(dessin.Open());
            var xml = lecteur.ReadToEnd();
            return System.Text.RegularExpressions.Regex.Match(xml, @"<(?:xdr:)?ext cx=""(\d+)"" cy=""(\d+)""").Value.Replace("xdr:", "");
        }

        var rempli = _moteur.Remplir(ModelePlanAssemblage, Donnees(("Section", 3)));

        Assert.Equal(@"<ext cx=""876300"" cy=""333375""", Etendue(rempli));
        Assert.Equal(Etendue(ModelePlanAssemblage), Etendue(rempli));
    }

    [Fact]
    public void Remplir_SansSection_RetireLeBloc()
    {
        using var classeur = Ouvrir(_moteur.Remplir(ModelePlanAssemblage, Donnees()));
        var feuille = classeur.Worksheet(1);

        Assert.Equal("NB : vérifier les lots.", feuille.Cell(6, 1).GetString());
        Assert.Equal("$A$1:$F$6", feuille.PageSetup.PrintAreas.Single().RangeAddress.ToStringFixed());
    }

    [Fact]
    public void Remplir_ValeursNonReinterpretees_EtEsperluetteDoubleeDansLePiedDePage()
    {
        var donnees = Donnees(("Section", 1));
        donnees.Sections[0].Lignes[0].Champs["Caracteristique"] = "{{Date}}";
        donnees.Champs["CodeFormulaire"] = "FE&01";

        using var classeur = Ouvrir(_moteur.Remplir(ModelePlanAssemblage, donnees));
        var feuille = classeur.Worksheet(1);

        Assert.Equal("{{Date}}", feuille.Cell(8, 1).GetString());
        Assert.Contains("FE&&01.0", feuille.PageSetup.Footer.Right.GetText(XLHFOccurrence.OddPages));
    }

    [Fact]
    public void Remplir_ColonnePropreAuFormulaire()
    {
        var modele = Construire(f =>
        {
            f.Cell(1, 1).Value = "{{Section.Libelle}}";
            f.Cell(2, 1).Value = "{{Ligne.Caracteristique}}";
            f.Cell(2, 2).Value = "{{Ligne.Colonne.FREQUENCE}}";
        });
        var donnees = Donnees(("S", 1));
        donnees.Sections[0].Lignes[0].Colonnes["FREQUENCE"] = "1/h";

        using var classeur = Ouvrir(_moteur.Remplir(modele, donnees));
        Assert.Equal("1/h", classeur.Worksheet(1).Cell(2, 2).GetString());
    }

    [Theory]
    [InlineData("deux lignes de détail")]
    [InlineData("section sous le détail")]
    [InlineData("accolades")]
    [InlineData("nom invalide")]
    [InlineData("fusion débordante")]
    [InlineData("ligne dans le pied de page")]
    [InlineData("section sans détail")]
    [InlineData("cellule en erreur")]
    public void Analyser_RefuseLesModelesMalConstruits(string cas)
    {
        var modele = Construire(f =>
        {
            switch (cas)
            {
                case "deux lignes de détail":
                    f.Cell(2, 1).Value = "{{Ligne.Caracteristique}}";
                    f.Cell(3, 1).Value = "{{Ligne.Observations}}";
                    break;
                case "section sous le détail":
                    f.Cell(2, 1).Value = "{{Ligne.Caracteristique}}";
                    f.Cell(3, 1).Value = "{{Section.Libelle}}";
                    break;
                case "accolades":
                    f.Cell(1, 1).Value = "Date : {{Date}";
                    break;
                case "nom invalide":
                    f.Cell(1, 1).Value = "{{Date du jour}}";
                    break;
                case "fusion débordante":
                    f.Cell(2, 1).Value = "{{Section.Libelle}}";
                    f.Cell(3, 1).Value = "{{Ligne.Caracteristique}}";
                    f.Range("B1:B2").Merge();
                    break;
                case "ligne dans le pied de page":
                    f.PageSetup.Footer.Right.AddText("{{Ligne.Caracteristique}}", XLHFOccurrence.AllPages);
                    break;
                case "section sans détail":
                    f.Cell(1, 1).Value = "{{Section.Libelle}}";
                    break;
                case "cellule en erreur":
                    f.Cell(1, 1).Value = XLError.IncompatibleValue;
                    break;
            }
        });

        var analyse = _moteur.Analyser(modele);

        Assert.False(analyse.Valide);
    }

    [Fact]
    public void ImageDansLaCellule_Excel365_DevientUneImageFlottanteAjusteeASaCellule()
    {
        // Modèle réel : logo inséré « dans la cellule » en A1 et B1 (Excel 365), illisible hors d'Excel 365 (#VALUE!).
        var modele = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Rapports", "Fixtures", "modele_logo_dans_cellule.xlsx"));

        var analyse = _moteur.Analyser(modele);
        using var classeur = Ouvrir(_moteur.Remplir(modele, Donnees(("Section", 2))));
        var feuille = classeur.Worksheet(1);

        Assert.True(analyse.Valide, string.Join(" ", analyse.Erreurs));
        Assert.Equal(2, feuille.Pictures.Count);
        Assert.All(feuille.Pictures, p =>
        {
            Assert.Equal(1, p.TopLeftCell.Address.RowNumber);
            Assert.True(p.Height <= feuille.Row(1).Height * 96 / 72);
        });
        Assert.Equal(XLDataType.Blank, feuille.Cell("A1").DataType);
        Assert.Equal("S1 L2", feuille.Cell(9, 1).GetString());
    }

    [Fact]
    public void Analyser_RefuseUnFichierIllisible()
    {
        Assert.False(_moteur.Analyser(new byte[] { 1, 2, 3 }).Valide);
    }

    [Fact]
    public void PlusieursFeuilles_ChacuneVerifieeEtRemplie_PagesRecopiees()
    {
        using var modele = new XLWorkbook();
        var registre = modele.AddWorksheet("Registre");
        registre.Cell(1, 1).Value = "Page {{Page}}";
        var annexe = modele.AddWorksheet("Annexe");
        annexe.Cell(1, 1).Value = "{{Titre}}";
        annexe.Cell(2, 1).Value = "{{Ligne.Lot}}";
        using var flux = new MemoryStream();
        modele.SaveAs(flux);

        var donnees = new DonneesRapport();
        donnees.Champs["Page"] = "1";
        donnees.PagesSuivantes.Add(new DonneesRapport { Champs = { ["Page"] = "2" } });
        var feuille2 = new DonneesRapport { Champs = { ["Titre"] = "Étiquettes" } };
        feuille2.Sections.Add(new DonneesSectionRapport { Lignes = { new DonneesLigneRapport { Champs = { ["Lot"] = "L1" } }, new DonneesLigneRapport { Champs = { ["Lot"] = "L2" } } } });
        donnees.FeuillesSuivantes.Add(feuille2);

        using var classeur = Ouvrir(_moteur.Remplir(flux.ToArray(), donnees));

        Assert.Equal(new[] { "Registre", "Registre (2)", "Annexe" }, classeur.Worksheets.Select(f => f.Name));
        Assert.Equal("Page 1", classeur.Worksheet("Registre").Cell(1, 1).GetString());
        Assert.Equal("Page 2", classeur.Worksheet("Registre (2)").Cell(1, 1).GetString());
        Assert.Equal(("Étiquettes", "L1", "L2"), (classeur.Worksheet("Annexe").Cell(1, 1).GetString(),
            classeur.Worksheet("Annexe").Cell(2, 1).GetString(), classeur.Worksheet("Annexe").Cell(3, 1).GetString()));

        // Une erreur nomme sa feuille.
        annexe.Cell(3, 1).Value = "{{Ligne.Autre}}";
        using var flux2 = new MemoryStream();
        modele.SaveAs(flux2);
        Assert.Contains(_moteur.Analyser(flux2.ToArray()).Erreurs, e => e.StartsWith("Feuille « Annexe » : "));
    }

    // ── Outils ───────────────────────────────────────────────────────────

    /// <summary>Données : sections (titre, nombre de lignes) ; lignes « S{n} L{m} ».</summary>
    private static DonneesRapport Donnees(params (string Titre, int Lignes)[] sections)
    {
        var donnees = new DonneesRapport();
        donnees.Champs["Date"] = "14/10/2026";
        donnees.Champs["CodeArticle"] = "RBGFA-BAC-01";
        donnees.Champs["Designation"] = "Robinet";
        donnees.Champs["Remarques"] = "NB : vérifier les lots.";
        donnees.Champs["CodeFormulaire"] = "FE0594";
        donnees.Champs["VersionFormulaire"] = "0";
        for (var s = 0; s < sections.Length; s++)
        {
            var section = new DonneesSectionRapport();
            section.Champs["Libelle"] = sections[s].Titre;
            for (var l = 1; l <= sections[s].Lignes; l++)
            {
                var ligne = new DonneesLigneRapport();
                ligne.Champs["Caracteristique"] = $"S{s + 1} L{l}";
                ligne.Champs["LimiteSpecification"] = $"Limite S{s + 1} L{l}";
                section.Lignes.Add(ligne);
            }
            donnees.Sections.Add(section);
        }
        return donnees;
    }

    private static byte[] Construire(Action<IXLWorksheet> preparer)
    {
        using var classeur = new XLWorkbook();
        var feuille = classeur.AddWorksheet("Modèle");
        preparer(feuille);
        using var flux = new MemoryStream();
        classeur.SaveAs(flux);
        return flux.ToArray();
    }

    private static XLWorkbook Ouvrir(byte[] contenu) => new(new MemoryStream(contenu));
}
