using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Domain.Constants;
using static SopalTrace.Application.Rapports.Execution.FormatsRapport;

namespace SopalTrace.Application.Rapports.Execution;

/// <summary>
/// Résultats du contrôle en cours de fabrication d'assemblage d'un poste et d'une équipe (FE0816) :
/// <list type="bullet">
/// <item>une section par contrôle de lot réalisé (première pièce, réglage, contrôle à 100 %, dernière pièce) et par
/// section du plan, avec la dernière réponse de chaque caractéristique (C / NC, non-conformité, action, matricule) ;</item>
/// <item>un tableau horaire fixe (Heure06 … Heure05) : résultat des contrôles de production de l'heure (NC si l'un est
/// non conforme) et bilan de la tranche (non-conformité, actions de correction, approbation) ;</item>
/// <item>un tableau par équipe (Quart1 : équipe E1, Quart2 : E2, Quart3 : E3) : contrôle de fin de poste (dernière pièce)
/// de l'équipe du document, sur la ligne de son équipe.</item>
/// </list>
/// Le modèle est celui du formulaire de résultats de la fiche (ex. FE-RC-ENCF-ASS-PAS72_SOUPAPE : FE0817).
/// </summary>
public sealed class DefinitionRapportResultatsAssemblage : IDefinitionRapportExecution
{
    private static readonly string[] OrdreMoments = { "PREMIERE_PIECE", "REGLAGE", "100PCT", "DERNIERE_PIECE" };
    /// <summary>Lignes du tableau fixe du lot de l'OF (fiche des robinets manuels : quatre caractéristiques).</summary>
    public const int LignesTableauLot = 8;

    private readonly IDonneesExecutionRepository _donnees;

    public DefinitionRapportResultatsAssemblage(IDonneesExecutionRepository donnees) => _donnees = donnees;

    public string TypeExecution => TypesDocumentExecution.PlanAssemblage;

    public IReadOnlyList<string> ChampsObligatoires { get; } = new[] { "Ligne.Caracteristique" };

    public IReadOnlyList<ChampRapport> Champs { get; } = ConstruireCatalogue();

    private static List<ChampRapport> ConstruireCatalogue()
    {
        var champs = new List<ChampRapport>
        {
            new("Date", "Date du document (prise de poste de l'équipe)", "14/10/2026"),
            new("CodeArticle", "Article de l'OF", "25B2A01-1-1"),
            new("DesignationArticle", "Désignation de l'article", "Robinet gaz avec soupape"),
            new("NumeroOf", "Numéro de l'OF", "OF2607ASS02"),
            new("Atelier", "Atelier", "ASS"),
            new("Poste", "Poste de travail", "PAS71"),
            new("DesignationPoste", "Libellé du poste", "Poste d'assemblage 71"),
            new("Equipe", "Équipe", "E1"),
            new("NomDocument", "Nom du plan d'assemblage appliqué", "Plan ASS RBGFA_SOUPAPE PAS71 V1"),
            new("CodeFormulaire", "Code du formulaire de résultats (pied de page : code.version)", "FE0816"),
            new("VersionFormulaire", "Version du formulaire de résultats", "1"),
            new("TitreProduction", "Titre de la section des contrôles de production du plan", "Contrôle par échantillonnage (ISO 2859-1)"),
            new("TitreFinPoste", "Titre du tableau des contrôles de fin de poste par équipe", "Contrôle de fin de poste (dernière pièce)"),
            new("Section.Numero", "Numéro du contrôle", "1"),
            new("Section.Libelle", "Contrôle et section du plan", "Première pièce · Caractéristiques à contrôler au début du poste"),
            new("Section.Moment", "Moment du contrôle", "Première pièce"),
            new("Section.LibelleSection", "Libellé de la section du plan", "Caractéristiques à contrôler au début du poste"),
            new("Ligne.Numero", "Numéro de la caractéristique", "1"),
            new("Ligne.Caracteristique", "Caractéristique contrôlée", "Étanchéité interne 0,1 bar"),
            new("Ligne.Conforme", "« X » si conforme", "X"),
            new("Ligne.NonConforme", "« X » si non conforme", ""),
            new("Ligne.Nc", "Non-conformité constatée", "Fuite"),
            new("Ligne.Actions", "Actions de correction", "Joint remplacé"),
            new("Ligne.Approbation", "Matricule de l'opérateur qui a saisi", "55555"),
            new("Ligne.Heure", "Heure de la saisie", "06:12"),
        };
        foreach (var heure in Enumerable.Range(0, 24))
        {
            var h = $"Heure{heure:00}";
            champs.Add(new($"{h}.Libelle", $"Tranche {heure} h – {(heure + 1) % 24} h", $"{heure}h-{(heure + 1) % 24}h"));
            champs.Add(new($"{h}.Conforme", $"« X » si les contrôles de production de {heure} h sont conformes", "X"));
            champs.Add(new($"{h}.NonConforme", $"« X » si un contrôle de production de {heure} h est non conforme", ""));
            champs.Add(new($"{h}.Nc", $"Non-conformité de la tranche de {heure} h", ""));
            champs.Add(new($"{h}.Actions", $"Actions de correction de la tranche de {heure} h", ""));
            champs.Add(new($"{h}.Approbation", $"Matricule d'approbation de la tranche de {heure} h", "55555"));
        }
        champs.Add(new("TitreLot", "Titre du tableau du lot de l'OF (section « lot OF » du plan)", "Caractéristiques à contrôler au niveau du lot (OF)"));
        for (var i = 1; i <= LignesTableauLot; i++)
        {
            champs.Add(new($"Lot{i}.Caracteristique", $"Caractéristique n° {i} du lot de l'OF (première et dernière pièce)", "Couple de manœuvre"));
            champs.Add(new($"Lot{i}.Conforme", $"« X » si la caractéristique n° {i} du lot est conforme", "X"));
            champs.Add(new($"Lot{i}.NonConforme", $"« X » si la caractéristique n° {i} du lot est non conforme", ""));
            champs.Add(new($"Lot{i}.Nc", $"Non-conformité de la caractéristique n° {i} du lot", ""));
            champs.Add(new($"Lot{i}.Actions", $"Actions de correction de la caractéristique n° {i} du lot", ""));
            champs.Add(new($"Lot{i}.Approbation", $"Matricule de la saisie de la caractéristique n° {i} du lot", "55555"));
        }
        for (var q = 1; q <= 3; q++)
        {
            champs.Add(new($"Quart{q}.Conforme", $"« X » si le contrôle de fin de poste de l'équipe E{q} est conforme", "X"));
            champs.Add(new($"Quart{q}.NonConforme", $"« X » si le contrôle de fin de poste de l'équipe E{q} est non conforme", ""));
            champs.Add(new($"Quart{q}.Nc", $"Non-conformités du contrôle de fin de poste de l'équipe E{q}", ""));
            champs.Add(new($"Quart{q}.Actions", $"Actions de correction de fin de poste de l'équipe E{q}", ""));
            champs.Add(new($"Quart{q}.Approbation", $"Matricule du contrôle de fin de poste de l'équipe E{q}", "55555"));
        }
        return champs;
    }

    public string? TypeDocumentApplique => TypesDocument.PlanAssemblage;

    /// <summary>Formulaire de résultats de la famille et du poste du plan (celui que prendra une fiche créée maintenant).</summary>
    public Task<Guid?> FormulaireFicheViergeAsync(Guid documentEnteteId) => _donnees.GetFormulaireResultatsPlanAsync(documentEnteteId);

    public async Task<DonneesExecution?> ChargerViergeAsync(Guid documentEnteteId, FormulaireDuDocument? formulaire)
    {
        var statut = await _donnees.GetStatutViergeAsync(documentEnteteId, TypeExecution, formulaire);
        if (statut == null) return null;
        var sections = await _donnees.GetSectionsPlanAsync(documentEnteteId);
        return new DonneesExecution(ConstruireVierge(statut, sections),
            TexteFichier.Nettoyer($"FicheVierge_ResultatsAssemblage_{statut.PosteCode}_{statut.NomDocument}"));
    }

    /// <summary>Sections de production ISO : leurs résultats vont dans le tableau horaire, pas dans une section de la fiche.</summary>
    private static readonly HashSet<string> SectionsDeProduction = new(StringComparer.OrdinalIgnoreCase)
    {
        SectionTypes.Echantillonnage2859, SectionTypes.Echantillonnage3789, SectionTypes.Echantillonnage3951
    };

    /// <summary>
    /// Fiche vierge : chaque section de contrôle du plan (réglage, première et dernière pièce, 100 %) avec toutes ses
    /// caractéristiques, cases à remplir vides ; tableau horaire et fin de poste vides.
    /// </summary>
    public static DonneesRapport ConstruireVierge(StatutExecutionRapport statut, IReadOnlyList<SectionPlanRapport> sections)
    {
        var donnees = Construire(statut, sections, Array.Empty<ReponseLigneRapport>(), Array.Empty<OccurrenceRapport>(), Array.Empty<TrancheRapport>());
        var numero = 0;
        foreach (var sectionPlan in sections.Where(s => s.TypeSection == null || !SectionsDeProduction.Contains(s.TypeSection)).OrderBy(s => s.Ordre))
        {
            var section = new DonneesSectionRapport();
            section.Champs["Numero"] = Nombre(++numero);
            section.Champs["Moment"] = "";
            section.Champs["LibelleSection"] = sectionPlan.Libelle;
            section.Champs["Libelle"] = sectionPlan.Libelle;
            section.RepriseDansTableauLot = sectionPlan.TypeSection == SectionTypes.LotOf;
            var numeroLigne = 0;
            foreach (var lignePlan in sectionPlan.Lignes.OrderBy(l => l.Ordre))
            {
                var ligne = new DonneesLigneRapport();
                ligne.Champs["Numero"] = Nombre(++numeroLigne);
                ligne.Champs["Caracteristique"] = Texte(lignePlan.Caracteristique);
                foreach (var vide in new[] { "Conforme", "NonConforme", "Nc", "Actions", "Approbation", "Heure" })
                    ligne.Champs[vide] = "";
                section.Lignes.Add(ligne);
            }
            donnees.Sections.Add(section);
        }
        return donnees;
    }

    public async Task<DonneesExecution?> ChargerAsync(Guid execControleDocumentStatutId)
    {
        var statut = await _donnees.GetStatutAsync(execControleDocumentStatutId);
        if (statut == null || statut.TypeDocument != TypeExecution || string.IsNullOrEmpty(statut.PosteCode)) return null;
        var documentId = await _donnees.GetDocumentAppliqueAsync(statut.Id);
        if (documentId == null) return null;

        var sections = await _donnees.GetSectionsPlanAsync(documentId.Value);
        var reponses = await _donnees.GetReponsesLignesAsync(statut.Id);
        var occurrences = await _donnees.GetOccurrencesAsync(statut.ExecControleOfId, statut.PosteCode, statut.SessionId);
        var tranches = await _donnees.GetTranchesAsync(statut.ExecControleOfId, statut.SessionId);
        return new DonneesExecution(Construire(statut, sections, reponses, occurrences, tranches),
            TexteFichier.Nettoyer($"ResultatsAssemblage_{statut.PosteCode}_{statut.NumeroOf}_{statut.Equipe}_{statut.DateExecution:yyyyMMdd}"));
    }

    /// <summary>Valeurs du document (sans accès aux données).</summary>
    public static DonneesRapport Construire(StatutExecutionRapport statut, IReadOnlyList<SectionPlanRapport> sections,
        IReadOnlyList<ReponseLigneRapport> reponses, IReadOnlyList<OccurrenceRapport> occurrences, IReadOnlyList<TrancheRapport> tranches)
    {
        var donnees = new DonneesRapport();
        var c = donnees.Champs;
        c["Date"] = Date(statut.DateExecution);
        c["CodeArticle"] = Texte(statut.CodeArticle);
        c["DesignationArticle"] = Texte(statut.DesignationArticle);
        c["NumeroOf"] = Texte(statut.NumeroOf);
        c["CodeFormulaire"] = Texte(statut.CodeFormulaire);
        c["VersionFormulaire"] = statut.VersionFormulaire?.ToString(CultureInfo.InvariantCulture) ?? "";
        c["Atelier"] = CodesOperation.Assemblage;
        c["Poste"] = Texte(statut.PosteCode);
        c["DesignationPoste"] = Texte(statut.DesignationPoste);
        c["Equipe"] = Texte(statut.Equipe);
        c["NomDocument"] = Texte(statut.NomDocument);
        c["TitreProduction"] = sections.FirstOrDefault(s => s.TypeSection == SectionTypes.Echantillonnage2859)?.Libelle ?? "Contrôles de production";
        c["TitreFinPoste"] = "Contrôle de fin de poste (dernière pièce)";

        // Fin de poste de l'équipe : sa ligne (E1, E2, E3) reçoit le résultat de ses contrôles « dernière pièce ».
        var finDePoste = reponses.Where(r => r.Contexte == "DERNIERE_PIECE").GroupBy(r => r.LigneId)
            .Select(g => g.OrderByDescending(r => r.DateSaisie).First()).ToList();
        for (var q = 1; q <= 3; q++)
        {
            var estEquipe = string.Equals(statut.Equipe, Equipes.PourPosition(q - 1), StringComparison.OrdinalIgnoreCase) && finDePoste.Count > 0;
            var nonConforme = estEquipe && finDePoste.Any(r => r.ResultatType == ResultatsLigne.NonConforme);
            c[$"Quart{q}.Conforme"] = Croix(estEquipe && !nonConforme);
            c[$"Quart{q}.NonConforme"] = Croix(nonConforme);
            c[$"Quart{q}.Nc"] = estEquipe ? string.Join(" ; ", finDePoste.Select(r => r.DetailsNc).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct()) : "";
            c[$"Quart{q}.Actions"] = estEquipe ? string.Join(" ; ", finDePoste.Select(r => r.ActionsCorrection).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct()) : "";
            c[$"Quart{q}.Approbation"] = estEquipe ? finDePoste.OrderByDescending(r => r.DateSaisie).First().MatriculeOperateur : "";
        }

        // Tableau fixe du lot de l'OF : caractéristiques des sections « lot OF » du plan, dernière réponse de chacune
        // (première ou dernière pièce) ; cases vides pour une fiche vierge.
        var lignesLot = sections.Where(s => s.TypeSection == SectionTypes.LotOf).OrderBy(s => s.Ordre)
            .SelectMany(s => s.Lignes.OrderBy(l => l.Ordre)).Take(LignesTableauLot).ToList();
        c["TitreLot"] = sections.FirstOrDefault(s => s.TypeSection == SectionTypes.LotOf)?.Libelle ?? "Caractéristiques à contrôler au niveau du lot (OF)";
        for (var i = 1; i <= LignesTableauLot; i++)
        {
            var lignePlan = i <= lignesLot.Count ? lignesLot[i - 1] : null;
            var reponse = lignePlan == null ? null : reponses.Where(r => r.LigneId == lignePlan.Id).OrderByDescending(r => r.DateSaisie).FirstOrDefault();
            c[$"Lot{i}.Caracteristique"] = Texte(lignePlan?.Caracteristique);
            c[$"Lot{i}.Conforme"] = Croix(reponse?.ResultatType == ResultatsLigne.Conforme);
            c[$"Lot{i}.NonConforme"] = Croix(reponse?.ResultatType == ResultatsLigne.NonConforme);
            c[$"Lot{i}.Nc"] = Texte(reponse?.DetailsNc);
            c[$"Lot{i}.Actions"] = Texte(reponse?.ActionsCorrection);
            c[$"Lot{i}.Approbation"] = Texte(reponse?.MatriculeOperateur);
        }

        // Contrôles de lot : une section par moment et par section du plan, dans l'ordre du poste.
        var sectionDeLigne = sections.SelectMany(s => s.Lignes.Select(l => (l.Id, Section: s))).ToDictionary(x => x.Id, x => x.Section);
        var groupes = reponses
            .Where(r => sectionDeLigne.ContainsKey(r.LigneId))
            .GroupBy(r => (Section: sectionDeLigne[r.LigneId], r.Contexte, r.NumeroReglage))
            .OrderBy(g => g.Key.Section.Ordre)
            .ThenBy(g => Array.IndexOf(OrdreMoments, g.Key.Contexte) is var i && i < 0 ? int.MaxValue : i)
            .ThenBy(g => g.Min(r => r.DateSaisie));
        var numero = 0;
        foreach (var groupe in groupes)
        {
            var section = new DonneesSectionRapport();
            var moment = Moment(groupe.Key.Contexte, groupe.Key.NumeroReglage);
            section.Champs["Numero"] = Nombre(++numero);
            section.Champs["Moment"] = moment;
            section.Champs["LibelleSection"] = groupe.Key.Section.Libelle;
            section.Champs["Libelle"] = $"{moment} · {groupe.Key.Section.Libelle}";
            section.RepriseDansTableauLot = groupe.Key.Section.TypeSection == SectionTypes.LotOf;
            var dernieres = groupe.GroupBy(r => r.LigneId).ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.DateSaisie).First());
            var numeroLigne = 0;
            foreach (var lignePlan in groupe.Key.Section.Lignes.OrderBy(l => l.Ordre))
            {
                if (!dernieres.TryGetValue(lignePlan.Id, out var reponse)) continue;
                var ligne = new DonneesLigneRapport();
                var l = ligne.Champs;
                var conforme = reponse.ResultatType == ResultatsLigne.Conforme;
                l["Numero"] = Nombre(++numeroLigne);
                l["Caracteristique"] = Texte(lignePlan.Caracteristique);
                l["Conforme"] = Croix(conforme);
                l["NonConforme"] = Croix(reponse.ResultatType == ResultatsLigne.NonConforme);
                l["Nc"] = Texte(reponse.DetailsNc);
                l["Actions"] = Texte(reponse.ActionsCorrection);
                l["Approbation"] = Texte(reponse.MatriculeOperateur);
                l["Heure"] = reponse.DateSaisie.ToString("HH:mm", CultureInfo.InvariantCulture);
                section.Lignes.Add(ligne);
            }
            donnees.Sections.Add(section);
        }

        // Tableau horaire : contrôles de production répondus de l'équipe sur le poste, bilan de la tranche.
        foreach (var heure in Enumerable.Range(0, 24))
        {
            var h = $"Heure{heure:00}";
            var tranche = $"H_{heure:00}_{(heure + 1) % 24:00}";
            var resultats = occurrences.Where(o => o.TrancheHoraire.Split('|')[0] == tranche).Select(o => o.Resultat).ToList();
            var bilan = tranches.FirstOrDefault(t => t.TrancheHoraire.Split('|')[0] == tranche);
            var nonConforme = resultats.Contains("NC") || bilan?.ResultatFinal == "NC";
            c[$"{h}.Libelle"] = $"{heure}h-{(heure + 1) % 24}h";
            c[$"{h}.NonConforme"] = Croix(nonConforme);
            c[$"{h}.Conforme"] = Croix(!nonConforme && (resultats.Contains("C") || bilan?.ResultatFinal == "C"));
            c[$"{h}.Nc"] = Texte(bilan?.DetailsNc);
            c[$"{h}.Actions"] = Texte(bilan?.ActionsCorrection);
            c[$"{h}.Approbation"] = resultats.Count > 0 || bilan?.ResultatFinal != null ? Texte(bilan?.MatriculeApprobateur) : "";
        }
        return donnees;
    }

    private static string Moment(string contexte, string? numeroReglage) => contexte switch
    {
        "PREMIERE_PIECE" => "Première pièce",
        "DERNIERE_PIECE" => "Dernière pièce",
        "REGLAGE" => string.IsNullOrEmpty(numeroReglage) ? "Réglage" : $"Réglage {numeroReglage}",
        "100PCT" => "Contrôle à 100 %",
        _ => contexte
    };
}
