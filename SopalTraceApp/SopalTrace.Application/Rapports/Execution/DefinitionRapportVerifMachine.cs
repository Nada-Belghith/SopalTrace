using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Domain.Constants;
using static SopalTrace.Application.Rapports.Execution.FormatsRapport;

namespace SopalTrace.Application.Rapports.Execution;

/// <summary>
/// Rapport de vérification machine (FE0795 BEE, FE0820 MAS19, FE0529 MAS22…) : une ligne par échéance du plan (point ×
/// périodicité × moyen de détection), pièces de référence par famille de corps, puis la dernière saisie de l'opérateur
/// (pression, ΔP, conforme / non conforme, observation). Risque, méthode et périodicité se fusionnent verticalement dans
/// le modèle ({{Ligne.Risque|fusion}}) : les clés de regroupement sont le point et la périodicité, jamais le libellé.
/// Ordre des lignes, comme à l'écran : point par point (chaque méthode et ses périodicités) ; ou, quand plusieurs points d'un
/// même risque partagent une périodicité (toujours pour une machine SER, jamais pour BEE), par risque puis par périodicité,
/// les méthodes sous chaque périodicité (formulaire SER05 : au démarrage PRC puis PRNC, après la pause PRC puis PRNC…).
/// Tableaux, comme à l'écran de l'opérateur : une section « tests de conformité » puis une section « risques » ; pour une
/// machine aux tableaux d'étanchéité séparés (MAS22), les risques contrôlés avec une fuite étalon (FEC/FENC) forment la
/// feuille suivante du modèle. Le modèle est celui du formulaire du plan de vérification (FE-VM-machine).
/// </summary>
public sealed class DefinitionRapportVerifMachine : IDefinitionRapportExecution
{
    /// <summary>Familles de corps proposées aux modèles (colonnes « Corps (code) »).</summary>
    public const int FamillesMaximum = 6;

    private readonly IDonneesExecutionRepository _donnees;

    public DefinitionRapportVerifMachine(IDonneesExecutionRepository donnees) => _donnees = donnees;

    public string TypeExecution => TypesDocumentExecution.VerifMachine;

    public IReadOnlyList<string> ChampsObligatoires { get; } = new[] { "Ligne.Risque" };

    public IReadOnlyList<ChampRapport> Champs { get; } = ConstruireCatalogue();

    private static List<ChampRapport> ConstruireCatalogue()
    {
        var champs = new List<ChampRapport>
        {
            new("Date", "Date de la vérification", "14/10/2026"),
            new("CodeArticle", "Article de l'OF", "25B2A01-1-1"),
            new("DesignationArticle", "Désignation de l'article", "Robinet gaz avec soupape"),
            new("NumeroOf", "Numéro de l'OF", "OF2607ASS02"),
            new("Equipe", "Équipe de la vérification", "E1"),
            new("Poste", "Poste de travail", "PAS71"),
            new("Machine", "Code de la machine vérifiée", "BEE46"),
            new("LibelleMachine", "Libellé de la machine", "Banc d'essai étanchéité 46"),
            new("Operateur", "Opérateur qui a validé (sinon qui a saisi)", "Ali Ben Salah"),
            new("Matricule", "Matricule de cet opérateur", "55555"),
            new("CodeFormulaire", "Code du formulaire du plan de vérification", "FE0795"),
            new("VersionFormulaire", "Version du formulaire", "0"),
            new("Section.Titre", "Titre de la première colonne du tableau : « Test de conformité », « Risque / Défaut » ou « Contrôle d'étanchéité »", "Risque / Défaut"),
            new("Section.Type", "Tableau : CONFORMITE, RISQUE ou ETANCHEITE", "RISQUE"),
            new("Ligne.Numero", "Numéro du point vérifié", "1"),
            new("Ligne.Type", "Risque ou point de conformité", "Risque"),
            new("Ligne.Risque", "Risque / défaut (ou point de conformité)", "Fuite interne"),
            new("Ligne.Methode", "Méthode de contrôle", "Test d'étanchéité"),
            new("Ligne.Periodicite", "Périodicité de l'échéance", "Au démarrage"),
            new("Ligne.Moyen", "Moyen de contrôle (détection)", "PRC+FEC"),
            new("Ligne.FuiteEtalon", "Pièces sans famille de corps (fuite étalon FEC / FENC…)", "FUI24"),
            new("Ligne.Pieces", "Toutes les pièces de référence de l'échéance", "BCF01, FUI24"),
            new("Ligne.Pression", "Pression d'entrée affichée (bar)", "6"),
            new("Ligne.DeltaP", "ΔP / fuite affichée (Pa)", "12"),
            new("Ligne.Conforme", "« X » si conforme", "X"),
            new("Ligne.NonConforme", "« X » si non conforme", ""),
            new("Ligne.Observation", "Observation en cas de non-conformité", "Joint changé"),
        };
        for (var f = 1; f <= FamillesMaximum; f++)
        {
            champs.Add(new($"Famille{f}", $"Code de la famille de corps {f} du plan", "25B0A01"));
            champs.Add(new($"Famille{f}.Designation", $"Désignation de la famille de corps {f}", "Corps robinet"));
            champs.Add(new($"Famille{f}.Titre", $"Titre de colonne « Corps (code) » de la famille {f}, vide si le plan n'a pas cette famille", "Corps (25B0A01)"));
            champs.Add(new($"Ligne.Piece{f}", $"Pièce(s) de référence de la famille {f} pour l'échéance", "BCF01"));
        }
        return champs;
    }

    public string? TypeDocumentApplique => TypesDocument.VerifMachine;

    public Task<Guid?> FormulaireFicheViergeAsync(Guid documentEnteteId) => _donnees.GetFormulaireDocumentAsync(documentEnteteId);

    public async Task<DonneesExecution?> ChargerViergeAsync(Guid documentEnteteId, FormulaireDuDocument? formulaire)
    {
        var statut = await _donnees.GetStatutViergeAsync(documentEnteteId, TypeExecution, formulaire);
        var plan = statut == null ? null : await _donnees.GetPlanVerifMachineAsync(documentEnteteId);
        if (statut == null || plan == null) return null;
        return new DonneesExecution(Construire(plan, Array.Empty<ReponseVerifMachine>(), statut, null, null),
            TexteFichier.Nettoyer($"FicheVierge_VerifMachine_{plan.MachineCode}"));
    }

    public async Task<DonneesExecution?> ChargerAsync(Guid execControleDocumentStatutId)
    {
        var statut = await _donnees.GetStatutAsync(execControleDocumentStatutId);
        if (statut == null || statut.TypeDocument != TypeExecution) return null;
        var documentId = await _donnees.GetDocumentAppliqueAsync(statut.Id);
        var plan = documentId == null ? null : await _donnees.GetPlanVerifMachineAsync(documentId.Value);
        if (plan == null) return null;

        var reponses = await _donnees.GetReponsesVerifMachineAsync(statut.Id);
        var matricule = statut.MatriculeOperateur
                        ?? reponses.GroupBy(r => r.MatriculeOperateur).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
        var noms = matricule == null ? new Dictionary<string, string>() : await _donnees.GetNomsAsync(new[] { matricule });
        var donnees = Construire(plan, reponses, statut, matricule, matricule == null ? null : noms.GetValueOrDefault(matricule));
        return new DonneesExecution(donnees,
            TexteFichier.Nettoyer($"VerifMachine_{plan.MachineCode}_{statut.NumeroOf}_{statut.DateExecution:yyyyMMdd}_{statut.Equipe}"));
    }

    /// <summary>Valeurs du rapport (sans accès aux données).</summary>
    public static DonneesRapport Construire(PlanVerifMachineRapport plan, IReadOnlyList<ReponseVerifMachine> reponses, StatutExecutionRapport statut,
        string? matricule, string? nomOperateur)
    {
        var donnees = new DonneesRapport();
        var c = donnees.Champs;
        c["Date"] = Date(statut.DateExecution ?? statut.DateTermine);
        c["CodeArticle"] = Texte(statut.CodeArticle);
        c["DesignationArticle"] = Texte(statut.DesignationArticle);
        c["NumeroOf"] = Texte(statut.NumeroOf);
        c["Equipe"] = Texte(statut.Equipe);
        c["Poste"] = Texte(statut.PosteCode);
        c["Machine"] = Texte(plan.MachineCode);
        c["LibelleMachine"] = Texte(plan.LibelleMachine);
        c["Operateur"] = Texte(nomOperateur);
        c["Matricule"] = Texte(matricule);
        c["CodeFormulaire"] = Texte(statut.CodeFormulaire);
        c["VersionFormulaire"] = statut.VersionFormulaire?.ToString(Francais) ?? "";
        var familles = plan.Familles.OrderBy(f => f.Ordre).Take(FamillesMaximum).ToList();
        for (var f = 0; f < familles.Count; f++)
        {
            c[$"Famille{f + 1}"] = familles[f].Code;
            c[$"Famille{f + 1}.Designation"] = familles[f].Designation;
            c[$"Famille{f + 1}.Titre"] = $"Corps ({familles[f].Code})";
        }

        var parEcheance = reponses.ToDictionary(r => r.EcheanceId);
        var parPeriodicite = ParPeriodicite(plan);
        var points = plan.Points.OrderBy(p => p.Ordre).Select((p, i) => (Point: p, Numero: i + 1)).ToList();
        bool Etancheite(PointVerifMachine p) => p.Echeances.Any(e => e.Pieces.Any(piece => RolesPieceReference.EstFuiteEtalon(piece.Role)));
        var conformite = points.Where(p => p.Point.TypeLigne == TypesLigneVerifMachine.Conformite).ToList();
        var risques = points.Where(p => p.Point.TypeLigne != TypesLigneVerifMachine.Conformite).ToList();
        var etancheite = plan.TablesEtalonSeparees ? risques.Where(p => Etancheite(p.Point)).ToList() : new();
        risques = risques.Except(etancheite).ToList();

        if (conformite.Count > 0) donnees.Sections.Add(Section("Test de conformité", TypeConformite, conformite, familles, parEcheance, parPeriodicite));
        if (risques.Count > 0 || donnees.Sections.Count == 0)
            donnees.Sections.Add(Section("Risque / Défaut", TypeRisque, risques, familles, parEcheance, parPeriodicite));
        if (etancheite.Count > 0)
        {
            // Feuille suivante du modèle : mêmes champs du document, tableau des contrôles d'étanchéité.
            var feuille = new DonneesRapport();
            foreach (var (nom, valeur) in c) feuille.Champs[nom] = valeur;
            feuille.Sections.Add(Section("Contrôle d'étanchéité", TypeEtancheite, etancheite, familles, parEcheance, parPeriodicite));
            donnees.FeuillesSuivantes.Add(feuille);
        }
        return donnees;
    }

    private const string TypeConformite = "CONFORMITE";
    private const string TypeRisque = "RISQUE";
    private const string TypeEtancheite = "ETANCHEITE";

    /// <summary>
    /// Regroupement par périodicité (l'« architecture A » de l'écran) : machine SER, ou plusieurs points d'un même risque qui
    /// partagent une périodicité ; jamais pour une machine BEE.
    /// </summary>
    public static bool ParPeriodicite(PlanVerifMachineRapport plan) => plan.ModeleVerification switch
    {
        "SER" => true,
        "BEE" => false,
        _ => plan.Points.GroupBy(p => p.Libelle).Where(g => g.Count() > 1)
            .Any(g => g.SelectMany(p => p.Echeances.Select(e => e.Periodicite).Distinct()).GroupBy(x => x).Any(x => x.Count() > 1))
    };

    /// <summary>Une ligne du tableau : l'échéance d'un point, et ses clés de regroupement vertical.</summary>
    private sealed record LigneTableau(PointVerifMachine Point, int Numero, EcheanceVerifMachine Echeance, string CleRisque, string CleMethode,
        string ClePeriodicite);

    /// <summary>Point par point : chaque méthode, puis ses périodicités.</summary>
    private static IEnumerable<LigneTableau> ParPoint(IEnumerable<(PointVerifMachine Point, int Numero)> points) =>
        points.SelectMany(p => p.Point.Echeances.Select(e => new LigneTableau(p.Point, p.Numero, e, p.Point.Id.ToString(), p.Point.Id.ToString(),
            $"{p.Point.Id}|{e.OrdrePeriodicite}|{e.Periodicite}")));

    /// <summary>
    /// Par risque, puis par périodicité : les points consécutifs d'un même risque partagent la case du risque et celle de
    /// chaque périodicité ; sous chaque périodicité, la méthode de chaque point (dans l'ordre du plan).
    /// </summary>
    private static IEnumerable<LigneTableau> ParRisqueEtPeriodicite(IEnumerable<(PointVerifMachine Point, int Numero)> points)
    {
        var groupes = new List<List<(PointVerifMachine Point, int Numero)>>();
        foreach (var p in points)
        {
            if (groupes.Count > 0 && groupes[^1][0].Point.Libelle == p.Point.Libelle) groupes[^1].Add(p);
            else groupes.Add(new() { p });
        }
        foreach (var groupe in groupes)
        {
            var cleRisque = groupe[0].Point.Id.ToString();
            var periodicites = groupe.SelectMany(p => p.Point.Echeances)
                .Select(e => (e.OrdrePeriodicite, e.Periodicite)).Distinct().OrderBy(x => x.OrdrePeriodicite).ToList();
            foreach (var (ordre, periodicite) in periodicites)
                foreach (var (point, _) in groupe)
                    foreach (var echeance in point.Echeances.Where(e => e.OrdrePeriodicite == ordre && e.Periodicite == periodicite))
                        yield return new LigneTableau(point, groupe[0].Numero, echeance, cleRisque, $"{point.Id}|{ordre}|{periodicite}",
                            $"{cleRisque}|{ordre}|{periodicite}");
        }
    }

    private static DonneesSectionRapport Section(string titre, string type, IEnumerable<(PointVerifMachine Point, int Numero)> points,
        IReadOnlyList<FamilleVerifMachine> familles, IReadOnlyDictionary<Guid, ReponseVerifMachine> parEcheance, bool parPeriodicite)
    {
        var section = new DonneesSectionRapport();
        section.Champs["Titre"] = titre;
        section.Champs["Type"] = type;
        foreach (var (point, numero, echeance, cleRisque, cleMethode, clePeriodicite) in parPeriodicite ? ParRisqueEtPeriodicite(points) : ParPoint(points))
        {
            var ligne = new DonneesLigneRapport();
            var l = ligne.Champs;
            l["Numero"] = Nombre(numero);
            l["Type"] = point.TypeLigne == TypesLigneVerifMachine.Conformite ? "Conformité" : "Risque";
            l["Risque"] = Texte(point.Libelle);
            l["Methode"] = Texte(point.Methode);
            l["Periodicite"] = Texte(echeance.Periodicite);
            l["Moyen"] = Texte(echeance.MoyenDetection);
            for (var f = 0; f < familles.Count; f++)
                l[$"Piece{f + 1}"] = Codes(echeance.Pieces.Where(p => p.FamilleId == familles[f].Id));
            l["FuiteEtalon"] = Codes(echeance.Pieces.Where(p => p.FamilleId == null));
            l["Pieces"] = Codes(echeance.Pieces);
            var reponse = parEcheance.GetValueOrDefault(echeance.Id);
            l["Pression"] = Decimal(reponse?.PressionEntree);
            l["DeltaP"] = Decimal(reponse?.FuiteAffichee);
            l["Conforme"] = Croix(reponse?.Conforme == true);
            l["NonConforme"] = Croix(reponse?.Conforme == false);
            l["Observation"] = Texte(reponse?.Observation);
            // Regroupements verticaux (fusion des cases identiques) selon l'ordre du tableau.
            l["Cle.Risque"] = cleRisque;
            l["Cle.Methode"] = cleMethode;
            l["Cle.Type"] = cleRisque;
            l["Cle.Numero"] = cleRisque;
            l["Cle.Periodicite"] = clePeriodicite;
            section.Lignes.Add(ligne);
        }
        return section;
    }

    private static string Codes(IEnumerable<PieceVerifMachine> pieces) =>
        string.Join(", ", pieces.Select(p => p.Code).Where(code => !string.IsNullOrWhiteSpace(code)).Distinct());

    private static string Decimal(decimal? valeur) => valeur?.ToString("0.###", Francais) ?? "";
}
