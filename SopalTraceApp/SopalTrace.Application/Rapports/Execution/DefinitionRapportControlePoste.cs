using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Domain.Constants;
using static SopalTrace.Application.Rapports.Execution.FormatsRapport;

namespace SopalTrace.Application.Rapports.Execution;

/// <summary>
/// Résultat de contrôle au poste d'un jour de production (FE0794) : défauts du document (lignes) comptés heure par
/// heure pour chaque équipe, pièces réalisées et totaux. Les équipes et leurs heures sont celles du document
/// (définies par le superviseur) : {{Equipe1.Heure3}} est la 3e heure de la 1re équipe. Valeurs et totaux lus par
/// le même service que l'écran de l'opérateur (aucun calcul refait ici).
/// </summary>
public sealed class DefinitionRapportControlePoste : IDefinitionRapportExecution
{
    /// <summary>Équipes et heures par équipe proposées aux modèles (3 équipes de 8 h, ou 2 de 12 h).</summary>
    public const int EquipesMaximum = 3;
    public const int HeuresMaximum = 12;

    private readonly IExecRcPosteService _controlePoste;
    private readonly IDonneesExecutionRepository _donnees;

    public DefinitionRapportControlePoste(IExecRcPosteService controlePoste, IDonneesExecutionRepository donnees)
    {
        _controlePoste = controlePoste;
        _donnees = donnees;
    }

    public string TypeExecution => TypesDocumentExecution.ResultatControlePoste;

    public IReadOnlyList<string> ChampsObligatoires { get; } = new[] { "Ligne.Defaut" };

    public IReadOnlyList<ChampRapport> Champs { get; } = ConstruireCatalogue();

    private static List<ChampRapport> ConstruireCatalogue()
    {
        var champs = new List<ChampRapport>
        {
            new("Date", "Jour de production du document", "14/10/2026"),
            new("CodeArticle", "Article de l'OF", "25B2A01-1-1"),
            new("DesignationArticle", "Désignation de l'article", "Robinet gaz avec soupape"),
            new("NumeroOf", "Numéro de l'OF", "OF2607ASS02"),
            new("Poste", "Poste de travail", "PAS72"),
            new("DesignationPoste", "Libellé du poste", "Poste d'assemblage 72"),
            new("DesignationFamille", "Désignation de la famille de produit fini de l'article", "Robinet bouteille de gaz avec soupape"),
            new("NomDocument", "Nom du document de contrôle au poste appliqué", "Contrôle poste PAS72 soupape V1"),
            new("CodeFormulaire", "Code du formulaire du document", "FE0794"),
            new("VersionFormulaire", "Version du formulaire", "0"),
            new("Remarques", "Remarques du document", "En bleu : valeurs manuscrites"),
            new("TotalDefauts", "Total des défauts du jour", "12"),
            new("TotalPiecesTestees", "Total des pièces testées (réalisées + défauts)", "812"),
            new("TauxNc", "Taux de non-conformité", "1,48 %"),
            new("PiecesRebutees", "Pièces rebutées (toutes équipes)", "5"),
            new("PiecesConformes", "Pièces conformes (testées − rebutées)", "807"),
            new("Ligne.Numero", "Numéro du défaut", "1"),
            new("Ligne.Machine", "Machine ou banc d'essai", "BEE46"),
            new("Ligne.Defaut", "Désignation du défaut", "Fuite clapet"),
            new("Ligne.TotalDefauts", "Total du défaut sur la journée", "4"),
        };
        for (var e = 1; e <= EquipesMaximum; e++)
        {
            champs.Add(new($"Equipe{e}.Nom", $"Nom de l'équipe {e} du document", "Équipe matin"));
            champs.Add(new($"Equipe{e}.Code", $"Code de l'équipe {e}", "E1"));
            champs.Add(new($"Equipe{e}.Operateur", $"Opérateur de l'équipe {e} (qui a validé, sinon qui a saisi)", "Ali Ben Salah"));
            champs.Add(new($"Equipe{e}.Matricule", $"Matricule de l'opérateur de l'équipe {e}", "55555"));
            champs.Add(new($"Equipe{e}.Rebutees", $"Pièces rebutées par l'équipe {e}", "2"));
            for (var h = 1; h <= HeuresMaximum; h++)
            {
                champs.Add(new($"Equipe{e}.Heure{h}", $"Heure {h} de l'équipe {e}", "6h-7h"));
                champs.Add(new($"TotalNc.Equipe{e}.Heure{h}", $"Total des non-conformités de l'heure {h} de l'équipe {e}", "1"));
                champs.Add(new($"Realise.Equipe{e}.Heure{h}", $"Pièces réalisées pendant l'heure {h} de l'équipe {e}", "100"));
                champs.Add(new($"Ligne.Equipe{e}.Heure{h}", $"Défauts de la ligne pendant l'heure {h} de l'équipe {e}", "0"));
            }
        }
        return champs;
    }

    public string? TypeDocumentApplique => TypesDocument.ControlePoste;

    public Task<Guid?> FormulaireFicheViergeAsync(Guid documentEnteteId) => _donnees.GetFormulaireDocumentAsync(documentEnteteId);

    public async Task<DonneesExecution?> ChargerViergeAsync(Guid documentEnteteId, FormulaireDuDocument? formulaire)
    {
        var statut = await _donnees.GetStatutViergeAsync(documentEnteteId, TypeExecution, formulaire);
        var document = statut == null ? null : await _controlePoste.GetFicheViergeAsync(documentEnteteId);
        if (statut == null || document == null) return null;
        return new DonneesExecution(Construire(document, statut, new Dictionary<string, string>()),
            TexteFichier.Nettoyer($"FicheVierge_ControlePoste_{statut.PosteCode}_{statut.NomDocument}"));
    }

    public async Task<DonneesExecution?> ChargerAsync(Guid execControleDocumentStatutId)
    {
        var statut = await _donnees.GetStatutAsync(execControleDocumentStatutId);
        if (statut == null || statut.TypeDocument != TypeExecution) return null;
        var document = await _controlePoste.GetPlanByStatutIdAsync(statut.Id);
        if (document == null) return null;

        var matricules = Operateurs(document).Values.Where(m => m != null).Select(m => m!).Distinct().ToList();
        var noms = matricules.Count == 0 ? new Dictionary<string, string>() : await _donnees.GetNomsAsync(matricules);
        var donnees = Construire(document, statut, noms);
        return new DonneesExecution(donnees,
            TexteFichier.Nettoyer($"ControlePoste_{statut.PosteCode}_{statut.NumeroOf}_{statut.DateExecution:yyyyMMdd}"));
    }

    /// <summary>Valeurs du document (sans accès aux données).</summary>
    public static DonneesRapport Construire(ExecRcPosteDto document, StatutExecutionRapport statut, IReadOnlyDictionary<string, string> noms)
    {
        var donnees = new DonneesRapport();
        var c = donnees.Champs;
        c["Date"] = Date(document.JourProduction ?? statut.DateExecution);
        c["CodeArticle"] = Texte(statut.CodeArticle);
        c["DesignationArticle"] = Texte(statut.DesignationArticle);
        c["NumeroOf"] = Texte(statut.NumeroOf);
        c["Poste"] = Texte(document.PosteCode ?? statut.PosteCode);
        c["DesignationPoste"] = Texte(statut.DesignationPoste);
        c["DesignationFamille"] = Texte(statut.DesignationFamille);
        c["NomDocument"] = Texte(document.PlanNom ?? statut.NomDocument);
        c["CodeFormulaire"] = Texte(statut.CodeFormulaire);
        c["VersionFormulaire"] = statut.VersionFormulaire?.ToString(Francais) ?? "";
        c["Remarques"] = Texte(document.PlanRemarques);
        c["TotalDefauts"] = Nombre(document.Bilan.TotalDefauts);
        c["TotalPiecesTestees"] = Nombre(document.Bilan.TotalPiecesTestees);
        c["TauxNc"] = document.Bilan.TauxNc.ToString("0.##", Francais) + " %";
        c["PiecesRebutees"] = Nombre(document.Bilan.NbPiecesRebutees);
        c["PiecesConformes"] = Nombre(document.Bilan.NbPieceConforme);

        var operateurs = Operateurs(document);
        var equipes = document.Equipes.Take(EquipesMaximum).ToList();
        for (var e = 0; e < equipes.Count; e++)
        {
            var equipe = equipes[e];
            var prefixe = $"Equipe{e + 1}";
            var matricule = operateurs.GetValueOrDefault(equipe.Code);
            c[$"{prefixe}.Nom"] = Texte(equipe.Nom);
            c[$"{prefixe}.Code"] = Texte(equipe.Code);
            c[$"{prefixe}.Matricule"] = Texte(matricule);
            c[$"{prefixe}.Operateur"] = matricule != null && noms.TryGetValue(matricule, out var nom) ? nom : "";
            var rebut = document.RebutsParEquipe.FirstOrDefault(r => r.Equipe == equipe.Code);
            c[$"{prefixe}.Rebutees"] = rebut == null ? "" : Nombre(rebut.NbPiecesRebutees);
            foreach (var (tranche, h) in equipe.Tranches.Take(HeuresMaximum).Select((t, i) => (t, i + 1)))
            {
                c[$"{prefixe}.Heure{h}"] = Tranche(tranche);
                var comptages = document.Lignes.SelectMany(l => l.Heures).Where(x => x.TrancheHoraire == tranche).ToList();
                c[$"TotalNc.{prefixe}.Heure{h}"] = comptages.Count == 0 ? "" : Nombre(comptages.Sum(x => x.NbNcParHeure));
                var realise = document.Reponses.FirstOrDefault(r => r.TrancheHoraire == tranche);
                c[$"Realise.{prefixe}.Heure{h}"] = realise == null ? "" : Nombre(realise.TotalRealiseHeure);
            }
        }

        // Une seule « section » : le tableau des défauts du document.
        var section = new DonneesSectionRapport();
        var numero = 0;
        foreach (var ligne in document.Lignes.OrderBy(l => l.OrdreAffiche))
        {
            var donneesLigne = new DonneesLigneRapport();
            var l = donneesLigne.Champs;
            l["Numero"] = Nombre(++numero);
            l["Machine"] = ligne.MachineCode is null or "-" ? "" : ligne.MachineCode;
            l["Defaut"] = Texte(ligne.LibelleAffiche);
            l["TotalDefauts"] = Nombre(ligne.Heures.Sum(x => x.NbNcParHeure));
            for (var e = 0; e < equipes.Count; e++)
            {
                foreach (var (tranche, h) in equipes[e].Tranches.Take(HeuresMaximum).Select((t, i) => (t, i + 1)))
                {
                    var comptage = ligne.Heures.FirstOrDefault(x => x.TrancheHoraire == tranche);
                    l[$"Equipe{e + 1}.Heure{h}"] = comptage == null ? "" : Nombre(comptage.NbNcParHeure);
                }
            }
            section.Lignes.Add(donneesLigne);
        }
        donnees.Sections.Add(section);
        return donnees;
    }

    /// <summary>Opérateur de chaque équipe : celui qui a validé ses heures, sinon celui qui a le plus saisi.</summary>
    private static Dictionary<string, string?> Operateurs(ExecRcPosteDto document) =>
        document.Equipes.ToDictionary(e => e.Code, e =>
            document.Validations.FirstOrDefault(v => v.Equipe == e.Code && !string.IsNullOrEmpty(v.MatriculeOperateur))?.MatriculeOperateur
            ?? document.Lignes.SelectMany(l => l.Heures)
                .Where(x => e.Tranches.Contains(x.TrancheHoraire) && !string.IsNullOrEmpty(x.MatriculeOp))
                .GroupBy(x => x.MatriculeOp!)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault());
}
