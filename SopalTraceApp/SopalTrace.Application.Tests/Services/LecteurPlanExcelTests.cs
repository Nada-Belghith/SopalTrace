using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using SopalTrace.Infrastructure.Services.ExcelImport;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>
    /// Lecture des plans Excel (fabrication, produit fini, assemblage) : structure seule, sans base.
    /// Les classeurs sont construits ici pour reproduire chaque variante des fichiers réels.
    /// </summary>
    public class LecteurPlanExcelTests
    {
        private static readonly string[] EnTete =
            { "Caractéristique contrôlée", "Limite de spécification", "Type de contrôle", "Moyen de contrôle", "Code instrument de contrôle", "Observations" };

        private const string PngUnPixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

        private static PlanLu Lire(Action<IXLWorksheet> remplir)
        {
            using var flux = new MemoryStream();
            using (var classeur = new XLWorkbook())
            {
                remplir(classeur.AddWorksheet("Plan"));
                classeur.SaveAs(flux);
            }
            flux.Position = 0;
            return LecteurPlanExcel.Lire(flux);
        }

        private static void Titre(IXLWorksheet f, int ligne, string texte, int derniereColonne = 6, int lignes = 1)
        {
            f.Cell(ligne, 1).Value = texte;
            f.Range(ligne, 1, ligne + lignes - 1, derniereColonne).Merge();
        }

        private static void Entete(IXLWorksheet f, int ligne, params string[] libelles)
        {
            var textes = libelles.Length > 0 ? libelles : EnTete;
            for (var i = 0; i < textes.Length; i++) f.Cell(ligne, i + 1).Value = textes[i];
        }

        private static void Ligne(IXLWorksheet f, int ligne, params string[] valeurs)
        {
            for (var i = 0; i < valeurs.Length; i++)
                if (!string.IsNullOrEmpty(valeurs[i])) f.Cell(ligne, i + 1).Value = valeurs[i];
        }

        [Fact]
        public void Lit_les_sections_leurs_lignes_et_la_note_finale()
        {
            var plan = Lire(f =>
            {
                Titre(f, 1, "Caractéristiques à contrôler aux réglages (une série de 5 pièces)");
                Entete(f, 2);
                Ligne(f, 3, "Fonctionnement", "", "Attribut", "Manuel", "", "Pas de point dur");
                Ligne(f, 4, "Etanchéité externe à 30 bars", "Fuite admissible 15cm3/h", "Mesure", "Banc d’essai", "BAN47");
                Titre(f, 5, "Caractéristiques à contrôler (100% des pièces/h)");
                Entete(f, 6);
                Ligne(f, 7, "Marquage", "", "Attribut", "Visuel");
                Titre(f, 8, "NB : Pour chaque lot, vérifier l'aptitude à l'emploi des matériaux.");
            });

            Assert.Empty(plan.Erreurs);
            Assert.Equal(new[] { "Caractéristiques à contrôler aux réglages (une série de 5 pièces)", "Caractéristiques à contrôler (100% des pièces/h)" },
                plan.Sections.Select(s => s.Titre));
            Assert.Equal(2, plan.Sections[0].Lignes.Count);
            var etancheite = plan.Sections[0].Lignes[1];
            Assert.Equal(("Fuite admissible 15cm3/h", "Mesure", "Banc d’essai", "BAN47"),
                (etancheite.Limite, etancheite.TypeControle, etancheite.Moyen, etancheite.Instrument));
            Assert.Single(plan.Sections[1].Lignes);
            Assert.Single(plan.Remarques);
            Assert.StartsWith("NB :", plan.Remarques[0]);
        }

        [Fact]
        public void Caracteristique_fusionnee_sur_plusieurs_lignes_donne_une_seule_ligne()
        {
            var plan = Lire(f =>
            {
                Titre(f, 1, "Contrôle Produit Fini par échantillonnage (2 échantillons)");
                Entete(f, 2);
                Ligne(f, 3, "Essai d’endurance 10000 cycles", "", "Mesure", "Banc d’essai", "BEE LAB : (BEE32)", "Doit fonctionner sans difficulté");
                f.Range("A3:A5").Merge(); f.Range("C3:C5").Merge(); f.Range("D3:D5").Merge(); f.Range("E3:E5").Merge();
                f.Range("F3:F6").Merge();
                Ligne(f, 6, "Essai d’endurance 3000 cycles", "", "Mesure", "Banc d’essai", "BEE LAB : (BEE32)");
            });

            var lignes = plan.Sections.Single().Lignes;
            Assert.Equal(2, lignes.Count);
            Assert.Equal((3, 5), (lignes[0].LigneExcel, lignes[0].DerniereLigneExcel));
            // L'observation fusionnée sur les deux caractéristiques s'applique aux deux.
            Assert.Equal("Doit fonctionner sans difficulté", lignes[1].Observations);
        }

        [Fact]
        public void Ligne_sans_caracteristique_complete_la_precedente()
        {
            var plan = Lire(f =>
            {
                Titre(f, 1, "Contrôle Produit Fini par échantillonnage (selon ISO 2859-1)");
                Entete(f, 2);
                Ligne(f, 3, "Absence de bavure", "Suivant défauthèque", "Attribut", "Visuel");
                f.Range("A3:A4").Merge(); f.Range("C3:C4").Merge(); f.Range("D3:D4").Merge();
                Ligne(f, 4, "", "(FA0974)");
                Ligne(f, 5, "Aspect général", "", "Attribut", "Suivant défauthèque", "FA0774", "*Absence de bavures");
                Ligne(f, 6, "", "", "", "", "", "*Présence des trous");
            });

            var lignes = plan.Sections.Single().Lignes;
            Assert.Equal(2, lignes.Count);
            Assert.Equal("Suivant défauthèque (FA0974)", lignes[0].Limite);
            Assert.Equal("*Absence de bavures\n*Présence des trous", lignes[1].Observations);
            Assert.Empty(plan.Avertissements);
        }

        [Fact]
        public void En_tete_decale_et_colonnes_fusionnees_sont_suivis()
        {
            var plan = Lire(f =>
            {
                Titre(f, 1, "Caractéristiques à contrôler aux réglages (une série de 5 pièces)", derniereColonne: 8);
                f.Cell("A2").Value = "Caractéristique\ncontrôlée";
                f.Cell("B2").Value = "Limite de spécificatzion"; f.Range("B2:C2").Merge();
                f.Cell("D2").Value = "Type de\ncontrôle";
                f.Cell("E2").Value = "Moyen de contrôle";
                f.Cell("F2").Value = "Code\ninstrument de contrôle";
                f.Cell("G2").Value = "Observations"; f.Range("G2:H2").Merge();
                f.Cell("A3").Value = "Etanchéité externe 0,1 bar";
                f.Cell("B3").Value = "Fuite admissible 15cm3/h"; f.Range("B3:C4").Merge();
                f.Cell("D3").Value = "Mesure"; f.Range("D3:D4").Merge();
                f.Cell("F3").Value = "BAN47"; f.Range("F3:F4").Merge();
                f.Cell("G3").Value = "Externe à 30 bars et à 0,1 bar"; f.Range("G3:H4").Merge();
                f.Cell("A4").Value = "Etanchéité externe à 30 bars";
            });

            var lignes = plan.Sections.Single().Lignes;
            Assert.Equal(2, lignes.Count);
            Assert.All(lignes, l =>
            {
                Assert.Equal("Fuite admissible 15cm3/h", l.Limite);
                Assert.Equal("Mesure", l.TypeControle);
                Assert.Equal("BAN47", l.Instrument);
                Assert.Equal("Externe à 30 bars et à 0,1 bar", l.Observations);
            });
        }

        [Fact]
        public void Titre_sur_deux_lignes_et_note_non_fusionnee()
        {
            var plan = Lire(f =>
            {
                Titre(f, 1, "Contrôle Produit Fini par échantillonnage (2 échantillons)", lignes: 2);
                Entete(f, 3);
                Ligne(f, 4, "Essai de résistance de queue de robinet", "", "Mesure", "Clé dynamométrique", "CLD");
                f.Cell("A5").Value = " Pour chaque Lot, il faut vérifier l'aptitude à l'emploi des matériaux par la vérification des certificats.";
            });

            Assert.Single(plan.Sections);
            Assert.Single(plan.Sections[0].Lignes);
            Assert.Single(plan.Remarques);
        }

        [Fact]
        public void Croquis_rattache_a_la_caracteristique_qui_couvre_sa_ligne()
        {
            var plan = Lire(f =>
            {
                Titre(f, 1, "Caractéristiques à contrôler (100% des pièces/h)");
                Entete(f, 2);
                Ligne(f, 3, "Etanchéité externe à 30 bars", "Fuite admissible 15cm3/h", "Mesure", "", "BEE46");
                Ligne(f, 4, "Aspect général de la pièce", "", "Attribut", "Suivant défauthèque", "FA0774", "*Absence de bavures");
                Ligne(f, 5, "", "", "", "", "", "*Présence des trous");
                f.AddPicture(new MemoryStream(Convert.FromBase64String(PngUnPixel))).MoveTo(f.Cell("D3"));
                f.AddPicture(new MemoryStream(Convert.FromBase64String(PngUnPixel))).MoveTo(f.Cell("F5"));
                f.AddPicture(new MemoryStream(Convert.FromBase64String(PngUnPixel))).MoveTo(f.Cell("D1"));
            });

            var lignes = plan.Sections.Single().Lignes;
            Assert.StartsWith("data:image/png;base64,", lignes[0].Image);
            Assert.NotNull(lignes[1].Image);
            Assert.Contains(plan.Avertissements, a => a.Contains("hors d'une caractéristique") && a.Contains("1"));
        }

        [Fact]
        public void Tableau_sans_titre_cree_une_section_et_le_signale()
        {
            var plan = Lire(f =>
            {
                Entete(f, 1);
                Ligne(f, 2, "Marquage", "", "Attribut", "Visuel");
            });

            Assert.Empty(plan.Erreurs);
            Assert.Equal(string.Empty, plan.Sections.Single().Titre);
            Assert.Contains(plan.Avertissements, a => a.Contains("sans titre"));
        }

        [Fact]
        public void Fichier_sans_en_tete_reconnu_est_refuse_avec_un_message_clair()
        {
            var plan = Lire(f =>
            {
                f.Cell("A1").Value = "Liste des courses";
                f.Cell("A2").Value = "Pain"; f.Cell("B2").Value = "2";
            });

            Assert.Contains(plan.Erreurs, e => e.Contains("Aucune ligne d'en-tête"));
            Assert.Empty(plan.Sections);
        }

        [Fact]
        public void Colonne_hors_standard_devient_colonne_personnalisee()
        {
            var plan = Lire(f =>
            {
                Titre(f, 1, "Caractéristiques à contrôler aux réglages (une série de 5 pièces)", derniereColonne: 7);
                Entete(f, 2, "Caractéristique", "Limite", "Type de contrôle", "Moyen de contrôle", "Code instrument", "Observations", "Fréquence opérateur");
                Ligne(f, 3, "Marquage", "", "Attribut", "Visuel", "", "", "1/h");
            });

            var colonne = Assert.Single(plan.ColonnesPerso);
            Assert.Equal("Fréquence opérateur", colonne.Value.Libelle);
            Assert.Equal("1/h", plan.Sections[0].Lignes[0].ColonnesPerso[colonne.Key]);
        }

        // ---------- Fichiers réels (tests/fixtures/excel) ----------

        private static string? Fixture(string nom)
        {
            for (var dossier = new DirectoryInfo(AppContext.BaseDirectory); dossier != null; dossier = dossier.Parent)
            {
                var chemin = Path.Combine(dossier.FullName, "tests", "fixtures", "excel", nom);
                if (File.Exists(chemin)) return chemin;
            }
            return null;
        }

        /// <summary>Nombre de caractéristiques attendu par section, dans l'ordre du fichier.</summary>
        [Theory]
        [InlineData("Fab_en_cours_Manu.xlsx", new[] { 8, 4, 9, 4 }, 0)]
        [InlineData("PF_manu.xlsx", new[] { 20, 7, 5 }, 0)]
        [InlineData("PF_soupape.xlsx", new[] { 22, 6, 4 }, 0)]
        [InlineData("pf_en_cours_auto.xlsx", new[] { 5, 5, 6 }, 4)]
        [InlineData("pf_en_cours_manu.xlsx", new[] { 8, 4, 9, 4 }, 7)]
        public void Fichiers_reels_lus_completement(string fichier, int[] lignesParSection, int croquis)
        {
            var chemin = Fixture(fichier);
            if (chemin == null) return; // Fichiers d'exemple non présents sur ce poste.

            using var flux = File.OpenRead(chemin);
            var plan = LecteurPlanExcel.Lire(flux);

            Assert.Empty(plan.Erreurs);
            Assert.Equal(lignesParSection, plan.Sections.Select(s => s.Lignes.Count).ToArray());
            Assert.All(plan.Sections, s => Assert.False(string.IsNullOrWhiteSpace(s.Titre)));
            Assert.All(plan.Sections.SelectMany(s => s.Lignes), l => Assert.False(string.IsNullOrWhiteSpace(l.Caracteristique)));
            Assert.Equal(croquis, plan.Sections.SelectMany(s => s.Lignes).Count(l => l.Image != null));
            Assert.Single(plan.Remarques);
        }

        [Fact]
        public void Fichier_reel_soupape_les_references_FA_completent_la_limite()
        {
            var chemin = Fixture("PF_soupape.xlsx");
            if (chemin == null) return;

            using var flux = File.OpenRead(chemin);
            var lignes = LecteurPlanExcel.Lire(flux).Sections[0].Lignes;

            Assert.Equal("Suivant défauthèque (FA0974)", lignes.Single(l => l.Caracteristique == "Absence de bavure").Limite);
        }
    }
}
