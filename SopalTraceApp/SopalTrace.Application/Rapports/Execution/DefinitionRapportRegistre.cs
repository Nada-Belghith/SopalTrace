using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Domain.Constants;
using static SopalTrace.Application.Rapports.Execution.FormatsRapport;

namespace SopalTrace.Application.Rapports.Execution;

/// <summary>
/// Registre de traçabilité assemblage d'un poste de l'OF (FE0129) :
/// <list type="bullet">
/// <item>feuille 1 : une colonne par changement de lots (date, n° d'incrément, article, OF), une ligne par composant
/// (corps, volant, composants servis) avec le lot de chaque colonne ; au-delà de 10 colonnes, la feuille est recopiée
/// en pages suivantes ;</item>
/// <item>feuille 2 : étiquettes des composants scannées par le magasin pour l'OF (article, lot, quantité, date du
/// scan, n° de rapport).</item>
/// </list>
/// Les lignes annulées du registre n'apparaissent pas. Un seul PDF par poste : il est porté par le premier document
/// de registre du poste (les suivants, confirmations des autres équipes, désignent le même registre).
/// </summary>
public sealed class DefinitionRapportRegistre : IDefinitionRapportExecution
{
    public const int ColonnesParPage = 10;
    public const int ComposantsMaximum = 14;
    private const string Corps = "Corps";
    private const string Volant = "Volant";

    private readonly IDonneesExecutionRepository _donnees;

    public DefinitionRapportRegistre(IDonneesExecutionRepository donnees) => _donnees = donnees;

    public string TypeExecution => TypesDocumentExecution.RegistreTracabilite;

    public IReadOnlyList<string> ChampsObligatoires { get; } = new[] { "Entree1.Date", "Composant1.Nom" };

    public IReadOnlyList<ChampRapport> Champs { get; } = ConstruireCatalogue();

    private static List<ChampRapport> ConstruireCatalogue()
    {
        var champs = new List<ChampRapport>
        {
            new("Famille", "Famille d'articles (famille de produit fini de l'article)", "RBGFA_SOUPAPE"),
            new("DesignationFamille", "Désignation de la famille", "Robinet bouteille de gaz avec soupape"),
            new("Poste", "Poste d'assemblage", "PAS71"),
            new("NumeroOf", "Numéro de l'OF", "OF2607ASS02"),
            new("CodeArticle", "Article de l'OF", "25B2A01-1-1"),
            new("Mois", "Mois (début de l'OF)", "octobre"),
            new("Annee", "Année (début de l'OF)", "2026"),
            new("Page", "Numéro de la page du registre", "1"),
            new("Pages", "Nombre de pages du registre", "1"),
            new("Ligne.Composant", "Étiquettes (feuille 2) : désignation du composant", "Joint auto-serreur robinet de gaz"),
            new("Ligne.Article", "Étiquettes : article", "3834000-1"),
            new("Ligne.Lot", "Étiquettes : lot", "24060024"),
            new("Ligne.Quantite", "Étiquettes : quantité", "15 000"),
            new("Ligne.Date", "Étiquettes : date du scan au magasin", "24/07/2024"),
            new("Ligne.Rapport", "Étiquettes : n° du rapport de contrôle", "59880"),
            new("Ligne.Etat", "Étiquettes : état (non enregistré : vide)", ""),
        };
        for (var e = 1; e <= ColonnesParPage; e++)
        {
            champs.Add(new($"Entree{e}.Date", $"Date du changement de lots de la colonne {e}", "14/10/2026"));
            champs.Add(new($"Entree{e}.Inc", $"N° d'incrément du registre de la colonne {e}", "1"));
            champs.Add(new($"Entree{e}.CodeArticle", $"Article de la colonne {e}", "25B2A01-1-1"));
            champs.Add(new($"Entree{e}.Of", $"OF de la colonne {e}", "OF2607ASS02"));
            champs.Add(new($"Entree{e}.Equipe", $"Équipe de la colonne {e}", "E1"));
        }
        for (var c = 1; c <= ComposantsMaximum; c++)
        {
            champs.Add(new($"Composant{c}.Nom", $"Nom du composant de la ligne {c} (corps, volant, composants servis)", "Corps"));
            for (var e = 1; e <= ColonnesParPage; e++)
                champs.Add(new($"Composant{c}.Entree{e}", $"Lot du composant {c} dans la colonne {e}", "OFCORPS01"));
        }
        return champs;
    }

    public async Task<DonneesExecution?> ChargerAsync(Guid execControleDocumentStatutId)
    {
        var statut = await _donnees.GetStatutAsync(execControleDocumentStatutId);
        if (statut == null || statut.TypeDocument != TypeExecution || string.IsNullOrEmpty(statut.PosteCode)) return null;
        // Un seul PDF par registre de poste : celui du premier document de registre.
        if (await _donnees.GetPremierStatutRegistreAsync(statut.ExecControleOfId, statut.PosteCode) != statut.Id) return null;

        var entrees = await _donnees.GetRegistreAsync(statut.ExecControleOfId, statut.PosteCode);
        var etiquettes = await _donnees.GetEtiquettesAsync(statut.NumeroOf);
        return new DonneesExecution(Construire(statut, entrees, etiquettes),
            TexteFichier.Nettoyer($"Registre_{statut.PosteCode}_{statut.NumeroOf}"));
    }

    /// <summary>Valeurs du registre (sans accès aux données).</summary>
    public static DonneesRapport Construire(StatutExecutionRapport statut, IReadOnlyList<EntreeRegistreRapport> entrees, IReadOnlyList<EtiquetteRapport> etiquettes)
    {
        // Lignes du registre : corps, volant puis composants dans l'ordre d'apparition.
        var composants = new List<string>();
        if (entrees.Any(e => !string.IsNullOrWhiteSpace(e.Corps))) composants.Add(Corps);
        if (entrees.Any(e => !string.IsNullOrWhiteSpace(e.Volant))) composants.Add(Volant);
        foreach (var nom in entrees.SelectMany(e => e.Composants).Select(c => c.Designation.Trim()))
            if (!composants.Contains(nom, StringComparer.OrdinalIgnoreCase)) composants.Add(nom);
        composants = composants.Take(ComposantsMaximum).ToList();

        var pages = entrees.Chunk(ColonnesParPage).ToList();
        if (pages.Count == 0) pages.Add(Array.Empty<EntreeRegistreRapport>());
        var debut = statut.DateDebutOf ?? statut.DateExecution;

        DonneesRapport Page(EntreeRegistreRapport[] colonnes, int numero)
        {
            var page = new DonneesRapport();
            var c = page.Champs;
            c["Famille"] = Texte(statut.FamilleCode);
            c["DesignationFamille"] = Texte(statut.DesignationFamille);
            c["Poste"] = Texte(statut.PosteCode);
            c["NumeroOf"] = Texte(statut.NumeroOf);
            c["CodeArticle"] = Texte(statut.CodeArticle);
            c["Mois"] = debut?.ToString("MMMM", Francais) ?? "";
            c["Annee"] = debut?.ToString("yyyy", Francais) ?? "";
            c["Page"] = Nombre(numero);
            c["Pages"] = Nombre(pages.Count);
            for (var e = 0; e < colonnes.Length; e++)
            {
                var entree = colonnes[e];
                var p = $"Entree{e + 1}";
                c[$"{p}.Date"] = Date(entree.DateHeure);
                c[$"{p}.Inc"] = Nombre(entree.Version);
                c[$"{p}.CodeArticle"] = Texte(statut.CodeArticle);
                c[$"{p}.Of"] = Texte(statut.NumeroOf);
                c[$"{p}.Equipe"] = Texte(entree.Equipe);
            }
            for (var k = 0; k < composants.Count; k++)
            {
                c[$"Composant{k + 1}.Nom"] = composants[k];
                for (var e = 0; e < colonnes.Length; e++)
                    c[$"Composant{k + 1}.Entree{e + 1}"] = Lot(colonnes[e], composants[k]);
            }
            return page;
        }

        var donnees = Page(pages[0], 1);
        for (var i = 1; i < pages.Count; i++)
            donnees.PagesSuivantes.Add(Page(pages[i], i + 1));

        // Feuille 2 : étiquettes des composants du registre de ce poste (toutes celles de l'OF si le registre n'en cite aucun).
        var articles = entrees.SelectMany(e => e.Composants).Select(c => c.CodeArticle).Where(a => a != null).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var feuilleEtiquettes = new DonneesRapport();
        var section = new DonneesSectionRapport();
        foreach (var etiquette in etiquettes.Where(e => articles.Count == 0 || articles.Contains(e.CodeArticle)))
        {
            var ligne = new DonneesLigneRapport();
            var l = ligne.Champs;
            l["Composant"] = Texte(etiquette.Designation);
            l["Article"] = Texte(etiquette.CodeArticle);
            l["Lot"] = Texte(etiquette.Lot);
            l["Quantite"] = etiquette.Quantite?.ToString("#,##0.###", Francais) ?? "";
            l["Date"] = Date(etiquette.DateScan);
            l["Rapport"] = Texte(etiquette.NumeroRapport);
            l["Etat"] = "";
            section.Lignes.Add(ligne);
        }
        feuilleEtiquettes.Sections.Add(section);
        foreach (var champ in donnees.Champs.Where(c => !c.Key.StartsWith("Entree") && !c.Key.StartsWith("Composant")))
            feuilleEtiquettes.Champs[champ.Key] = champ.Value;
        donnees.FeuillesSuivantes.Add(feuilleEtiquettes);
        return donnees;
    }

    private static string Lot(EntreeRegistreRapport entree, string composant) => composant switch
    {
        Corps => Texte(entree.Corps),
        Volant => Texte(entree.Volant),
        _ => Texte(entree.Composants.FirstOrDefault(c => string.Equals(c.Designation.Trim(), composant, StringComparison.OrdinalIgnoreCase))?.Lot)
    };
}
