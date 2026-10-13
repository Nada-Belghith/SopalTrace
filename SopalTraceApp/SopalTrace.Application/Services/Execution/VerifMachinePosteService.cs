using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.Dtos.VerifMachine;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services.Execution;

/// <summary>
/// Vérifications machine d'un poste d'assemblage, par moment (démarrage, après la pause, fin de poste) :
/// <list type="bullet">
/// <item>machines du poste (par défaut et ajoutées par l'équipe, conservées par leur document) ;</item>
/// <item>un document par machine, poste, équipe et jour ; seuls ses points du moment demandé sont attendus ;</item>
/// <item>la validation enregistre les réponses et marque le moment comme fait, uniquement si tous ses points
/// sont répondus (une non-conformité exige une observation) ; « Corriger » rouvre le moment.</item>
/// </list>
/// </summary>
public class VerifMachinePosteService : IVerifMachinePosteService
{
    private static readonly HashSet<string> Periodicites = new()
    {
        PeriodicitesControle.Demarrage, PeriodicitesControle.ApresPause, PeriodicitesControle.FinPoste
    };

    private readonly IOperateurRepository _operateur;
    private readonly IAssemblageExecutionService _assemblage;
    private readonly IDemarragePosteRepository _demarrages;

    public VerifMachinePosteService(IOperateurRepository operateur, IAssemblageExecutionService assemblage, IDemarragePosteRepository demarrages)
    {
        _operateur = operateur;
        _assemblage = assemblage;
        _demarrages = demarrages;
    }

    public async Task<VerifMachinesPosteDto> GetMachinesAsync(Guid execControleOfId, string posteCode, string periodicite)
    {
        VerifierPeriodicite(periodicite);
        var execOf = await _operateur.GetExecOfByIdAsync(execControleOfId)
            ?? throw new RessourceIntrouvableException("Exécution introuvable.");
        var postes = await _operateur.GetPostesForExecutionAsync(execControleOfId);
        if (!postes.Contains(posteCode))
            throw new RegleMetierException($"Le poste {posteCode} n'est pas affecté à cet OF.");

        // Équipe du poste : celle qui le tient (ses vérifications ne valent que pour elle).
        var session = await _demarrages.GetSessionAsync(execControleOfId, posteCode);
        var statutsPoste = session == null
            ? new List<ExecControleDocumentStatut>()
            : (await _operateur.GetDocumentStatutsAsync(execControleOfId, session.Id)).Where(s => s.PosteCode == posteCode).ToList();
        var cloture = statutsPoste.Any(s => s.TypeDocument == TypesDocumentExecution.ClotureVerifMachinePoste);
        var documents = statutsPoste
            .Where(s => s.TypeDocument == TypesDocumentExecution.VerifMachine && s.MachineCode != null)
            .GroupBy(s => s.MachineCode!)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.DateExecution).First());

        var poste = await _operateur.GetPosteWithMachinesAsync(posteCode);
        var parDefaut = poste?.CodeMachines.Where(m => m.Actif).Select(m => m.CodeMachine).ToList() ?? new List<string>();
        var codes = parDefaut.Concat(documents.Keys).Distinct().OrderBy(c => c).ToList();
        var libelles = await _operateur.GetMachineLabelsAsync(codes);

        var lignes = new List<VerifMachinePosteLigneDto>();
        foreach (var code in codes)
        {
            documents.TryGetValue(code, out var statut);
            lignes.Add(await LigneAsync(code, libelles.GetValueOrDefault(code), !parDefaut.Contains(code), statut, periodicite, cloture));
        }

        var attendues = lignes.Where(l => l.Etat is EtatsVerifMachine.AFaire or EtatsVerifMachine.EnCours or EtatsVerifMachine.Validee).ToList();
        return new VerifMachinesPosteDto
        {
            ExecControleOfId = execControleOfId,
            NumeroOf = execOf.NumeroOf,
            PosteCode = posteCode,
            Periodicite = periodicite,
            Equipe = session?.Equipe,
            Modifiable = execOf.Statut != StatutsExecution.Cloture && session?.Statut == StatutsSession.EnCours
                         && await _demarrages.EstEnCoursAsync(session.Id, posteCode),
            Machines = lignes,
            Attendues = attendues.Count,
            Validees = attendues.Count(l => l.Etat == EtatsVerifMachine.Validee)
        };
    }

    private async Task<VerifMachinePosteLigneDto> LigneAsync(string machineCode, string? libelle, bool ajoutee,
        ExecControleDocumentStatut? statut, string periodicite, bool cloture)
    {
        var ligne = new VerifMachinePosteLigneDto
        {
            MachineCode = machineCode, Libelle = libelle, Ajoutee = ajoutee, StatutId = statut?.Id
        };

        // Version du plan figée par le document de l'équipe, sinon plan actif de la machine.
        ligne.PlanId = statut?.DocId ?? await _operateur.GetPlanVerifMachineActifIdAsync(machineCode);
        if (ligne.PlanId == null)
        {
            ligne.Etat = EtatsVerifMachine.PlanManquant;
            return ligne;
        }

        var points = await PointsDuMomentAsync(ligne.PlanId.Value, periodicite);
        ligne.PointsTotal = points.Count;
        if (points.Count == 0)
        {
            ligne.Etat = EtatsVerifMachine.NonPrevue;
            return ligne;
        }

        if (statut != null)
        {
            var reponses = DernieresReponses(await _operateur.GetExecVerifMachineReponsesAsync(statut.Id));
            ligne.PointsRepondus = points.Count(p => reponses.TryGetValue(p, out var r) && r.Conforme.HasValue);
        }

        if (cloture || (statut != null && EstValide(statut, periodicite)))
        {
            ligne.Etat = EtatsVerifMachine.Validee;
            ligne.ValideeLe = statut?.DateTermine ?? statut?.DateExecution;
            ligne.ValideePar = statut?.MatriculeOperateur;
        }
        else
        {
            ligne.Etat = ligne.PointsRepondus > 0 ? EtatsVerifMachine.EnCours : EtatsVerifMachine.AFaire;
        }
        return ligne;
    }

    public async Task<Guid> AjouterMachineAsync(Guid execControleOfId, string posteCode, string machineCode, string matricule)
    {
        // Le document créé garde la machine dans la liste du poste (rechargement, autres tablettes).
        return await _assemblage.InitVerifMachineAsync(execControleOfId,
            new InitVerifMachineRequest { PosteCode = posteCode, MachineCode = machineCode }, matricule);
    }

    public async Task ValiderAsync(Guid statutId, ValiderVerifMachineRequest request, string matricule)
    {
        VerifierPeriodicite(request.Periodicite);
        var statut = await StatutModifiableAsync(statutId);
        var planId = statut.DocId ?? throw new RegleMetierException("Aucun plan de vérification n'est associé à ce document.");

        var maintenant = DateTime.Now;
        var nouvelles = request.Reponses
            .Where(r => r.Conforme.HasValue || r.PressionEntree.HasValue || r.FuiteAffichee.HasValue || !string.IsNullOrWhiteSpace(r.Observation))
            .Select(r => new ExecVerifMachineReponse
            {
                ExecControleDocumentStatutId = statut.Id,
                DocumentVerifMachineEcheanceId = r.DocumentVerifMachineEcheanceId,
                DateExecution = maintenant,
                MatriculeOperateur = matricule,
                PressionEntree = r.PressionEntree,
                FuiteAffichee = r.FuiteAffichee,
                Conforme = r.Conforme,
                Observation = string.IsNullOrWhiteSpace(r.Observation) ? null : r.Observation.Trim()
            })
            .ToList();

        var points = await PointsDuMomentAsync(planId, request.Periodicite);
        if (points.Count == 0)
            throw new RegleMetierException("Cette machine n'a pas de vérification prévue à ce moment.");
        var pointsDuPlan = await TousLesPointsAsync(planId);
        if (nouvelles.Any(r => !pointsDuPlan.Contains(r.DocumentVerifMachineEcheanceId)))
            throw new RegleMetierException("Une réponse ne correspond à aucun point du plan de vérification de la machine.");

        // État après saisie : réponses existantes (historique conservé) complétées par les nouvelles.
        var dernieres = DernieresReponses((await _operateur.GetExecVerifMachineReponsesAsync(statut.Id)).Concat(nouvelles));
        var manquants = points.Count(p => !dernieres.TryGetValue(p, out var r) || !r.Conforme.HasValue);
        if (manquants > 0)
            throw new RegleMetierException($"Validation impossible : {manquants} point(s) de vérification sans réponse.");
        if (points.Any(p => dernieres[p].Conforme == false && string.IsNullOrWhiteSpace(dernieres[p].Observation)))
            throw new RegleMetierException("Une observation est obligatoire pour chaque point non conforme.");

        if (nouvelles.Count > 0) _operateur.AddExecVerifMachineReponses(nouvelles);

        var moments = await _operateur.GetPeriodicitesPlanMachineAsync(statut.MachineCode!);
        bool aApresPause = PeriodiciteMachineHelper.AApresPause(moments);
        bool aFinPoste = PeriodiciteMachineHelper.AFinPoste(moments);
        switch (request.Periodicite)
        {
            case PeriodicitesControle.ApresPause:
                statut.EstPauseTermine = true;
                if (!aFinPoste) statut.EstTermine = true;
                break;
            case PeriodicitesControle.FinPoste:
                statut.EstTermine = true;
                break;
            default:
                statut.EstDemarrageTermine = true;
                if (!aApresPause && !aFinPoste) statut.EstTermine = true;
                break;
        }
        statut.DateExecution ??= maintenant;
        statut.DateTermine = maintenant;
        statut.MatriculeOperateur = matricule;
        await _operateur.SaveChangesAsync();
    }

    public async Task CorrigerAsync(Guid statutId, string periodicite, string matricule)
    {
        VerifierPeriodicite(periodicite);
        var statut = await StatutModifiableAsync(statutId);
        switch (periodicite)
        {
            case PeriodicitesControle.ApresPause:
                statut.EstPauseTermine = false;
                statut.EstTermine = false;
                break;
            case PeriodicitesControle.FinPoste:
                statut.EstTermine = false;
                break;
            default:
                statut.EstDemarrageTermine = false;
                statut.EstTermine = false;
                break;
        }
        statut.DateTermine = null;
        statut.MatriculeOperateur = matricule;
        await _operateur.SaveChangesAsync();
    }

    public async Task<List<VerifMachineHistoriqueDto>> GetHistoriqueAsync(Guid execControleOfId, string posteCode,
        string? machineCode, string? equipe, DateTime? jour)
    {
        var session = await _demarrages.GetSessionAsync(execControleOfId, posteCode);
        var statuts = (await _operateur.GetStatutsVerifMachineAsync(execControleOfId))
            .Where(s => s.PosteCode == posteCode)
            .Where(s => string.IsNullOrEmpty(machineCode) || s.MachineCode == machineCode)
            .Where(s => string.IsNullOrEmpty(equipe) || string.Equals(s.Equipe, equipe, StringComparison.OrdinalIgnoreCase))
            .Where(s => jour == null || s.DateExecution?.Date == jour.Value.Date)
            .ToList();

        // Moments prévus par le plan de chaque machine : « fin de poste faite » n'a de sens que si elle est prévue
        // (sans fin de poste, le document est terminé dès le démarrage).
        var moments = new Dictionary<string, List<string>>();
        foreach (var code in statuts.Select(s => s.MachineCode).OfType<string>().Distinct())
            moments[code] = await _operateur.GetPeriodicitesPlanMachineAsync(code) ?? new List<string>();

        return statuts.Select(s =>
        {
            var m = s.MachineCode != null && moments.TryGetValue(s.MachineCode, out var liste) ? liste : new List<string>();
            var finPrevue = PeriodiciteMachineHelper.AFinPoste(m);
            var pausePrevue = PeriodiciteMachineHelper.AApresPause(m);
            return new VerifMachineHistoriqueDto
            {
                StatutId = s.Id,
                PlanId = s.DocId,
                MachineCode = s.MachineCode ?? string.Empty,
                Equipe = s.Equipe,
                Jour = s.DateExecution,
                DemarragePrevu = PeriodiciteMachineHelper.ADemarrage(m),
                ApresPausePrevue = pausePrevue,
                FinPostePrevue = finPrevue,
                Demarrage = s.EstDemarrageTermine || (s.EstTermine && !pausePrevue && !finPrevue),
                ApresPause = pausePrevue && s.EstPauseTermine,
                FinPoste = finPrevue && s.EstTermine,
                SessionActive = session != null && s.SessionId == session.Id,
                MatriculeOperateur = s.MatriculeOperateur
            };
        }).ToList();
    }

    // ───────────────────────────── Outils ─────────────────────────────

    private static void VerifierPeriodicite(string periodicite)
    {
        if (!Periodicites.Contains(periodicite))
            throw new RegleMetierException("La périodicité doit valoir DEMARRAGE, APRES_PAUSE ou FIN_POSTE.");
    }

    /// <summary>Moment déjà validé (le marqueur EstTermine couvre les moments qui suivent la dernière vérification prévue).</summary>
    private static bool EstValide(ExecControleDocumentStatut statut, string periodicite) => periodicite switch
    {
        PeriodicitesControle.ApresPause => statut.EstPauseTermine || statut.EstTermine,
        PeriodicitesControle.FinPoste => statut.EstTermine,
        _ => statut.EstDemarrageTermine || statut.EstTermine
    };

    private async Task<HashSet<Guid>> PointsDuMomentAsync(Guid planId, string periodicite) =>
        (await _operateur.GetEcheancesPlanVerifMachineAsync(planId))
            .Where(e => PeriodiciteMachineHelper.Correspond(e.Periodicite, periodicite))
            .Select(e => e.EcheanceId)
            .ToHashSet();

    private async Task<HashSet<Guid>> TousLesPointsAsync(Guid planId) =>
        (await _operateur.GetEcheancesPlanVerifMachineAsync(planId)).Select(e => e.EcheanceId).ToHashSet();

    /// <summary>Historique en ajout seul : la dernière réponse de chaque point fait foi.</summary>
    private static Dictionary<Guid, ExecVerifMachineReponse> DernieresReponses(IEnumerable<ExecVerifMachineReponse> reponses) =>
        reponses
            .GroupBy(r => r.DocumentVerifMachineEcheanceId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.DateExecution).First());

    private async Task<ExecControleDocumentStatut> StatutModifiableAsync(Guid statutId)
    {
        var statut = await _operateur.GetDocumentStatutByIdAsync(statutId)
            ?? throw new RessourceIntrouvableException("Vérification machine introuvable.");
        if (statut.TypeDocument != TypesDocumentExecution.VerifMachine)
            throw new RegleMetierException("Ce document n'est pas une vérification machine.");
        if (statut.ExecControleOf.Statut == StatutsExecution.Cloture)
            throw new EtatExecutionInvalideException("Cet OF est clôturé : la vérification machine ne peut plus être modifiée.");

        var session = statut.PosteCode == null ? null : await _demarrages.GetSessionAsync(statut.ExecControleOfId, statut.PosteCode);
        if (session == null || statut.SessionId != session.Id || !await _demarrages.EstEnCoursAsync(session.Id, statut.PosteCode!))
            throw new EtatExecutionInvalideException("Cette vérification appartient à une équipe qui ne tient plus ce poste : elle reste consultable.");
        if (session.Statut == StatutsSession.EnPause)
            throw new EtatExecutionInvalideException("L'OF est en pause : reprenez la production avant de valider.");
        return statut;
    }
}
