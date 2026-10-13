using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services.Execution;

/// <summary>
/// Avancement d'une session d'assemblage, seule source de vérité de l'assistant opérateur :
/// <list type="number">
/// <item>Échantillonnage (une fiche par OF) ;</item>
/// <item>Démarrage du poste : vérification machine (démarrage) et première pièce des sections de lot
/// (LOT_POSTE à chaque équipe, LOT_OF pour la première équipe de l'OF) ;</item>
/// <item>Production : contrôles ECHANTILLONNAGE_2859 planifiés par le serveur (<see cref="CalendrierProduction"/>) ;
/// après une pause déjeuner, vérification machine « après pause » avant de reprendre ;</item>
/// <item>Fin de poste / fin d'OF (à la demande de l'équipe) : vérification machine de fin de poste, dernière
/// pièce, résultat du contrôle au poste et registre de traçabilité, sur tous les postes, avant la clôture.</item>
/// </list>
/// </summary>
public class AvancementAssemblageService : IAvancementAssemblageService
{
    private readonly IExecPlanAssemblageRepository _plans;
    private readonly IOperateurRepository _operateur;
    private readonly IExecRcPosteRepository _controlePoste;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExecutionOfService _executionOf;

    public AvancementAssemblageService(IExecPlanAssemblageRepository plans, IOperateurRepository operateur,
        IExecRcPosteRepository controlePoste, IUnitOfWork unitOfWork, IExecutionOfService executionOf)
    {
        _plans = plans;
        _operateur = operateur;
        _controlePoste = controlePoste;
        _unitOfWork = unitOfWork;
        _executionOf = executionOf;
    }

    /// <summary>Données de l'OF lues une fois pour évaluer tous ses postes.</summary>
    private sealed class Contexte
    {
        public required ExecControleOf ExecOf { get; init; }
        /// <summary>Session de l'équipe consultée (elle peut être en pause pendant qu'une autre produit).</summary>
        public required ExecControleOfSession? Session { get; init; }
        /// <summary>Sessions ouvertes de l'OF : au plus une en production, les autres en pause.</summary>
        public required List<ExecControleOfSession> SessionsOuvertes { get; init; }
        public required ContexteEquipe Equipe { get; init; }
        public required List<ExecControleDocumentStatut> Statuts { get; init; }
        public required Dictionary<string, List<string>> PeriodicitesMachines { get; init; }
        public required List<string> Postes { get; init; }
        public required TacheAssemblageDto Echantillonnage { get; init; }
        /// <summary>Minutes entre deux contrôles de production (60 / effectif par poste) ; null sans effectif.</summary>
        public required double? IntervalleControle { get; init; }
        public DateTime Maintenant { get; } = DateTime.Now;

        public string CodeArticle => ExecOf.NumeroOfNavigation.CodeArticle;

        /// <summary>Statuts de la session active (vérification machine, plan d'assemblage).</summary>
        public IEnumerable<ExecControleDocumentStatut> StatutsSession(string posteCode) =>
            Session == null ? Enumerable.Empty<ExecControleDocumentStatut>()
                : Statuts.Where(s => s.SessionId == Session.Id && s.PosteCode == posteCode);
    }

    /// <summary>Tâches d'un poste, par moment du parcours.</summary>
    private sealed record EvaluationPoste(
        TacheAssemblageDto VerifMachineDemarrage,
        TacheAssemblageDto PremierePiece,
        TacheAssemblageDto? VerifMachineApresPause,
        TacheAssemblageDto Production,
        TacheAssemblageDto? ControlePoste,
        TacheAssemblageDto VerifMachineFinPoste,
        TacheAssemblageDto DernierePiece,
        TacheAssemblageDto Tracabilite,
        List<TacheAssemblageDto> ControlesALaDemande,
        bool ReglageEnCours);

    /// <summary>Étapes où l'équipe peut saisir (hors pause, sans équipe ou OF clôturé).</summary>
    private static readonly HashSet<string> PhasesAvecSaisie = new()
    {
        PhasesAssemblage.Demarrage, PhasesAssemblage.Production, PhasesAssemblage.Reprise, PhasesAssemblage.FinPoste, PhasesAssemblage.FinOf
    };

    // ───────────────────────────── Lecture ─────────────────────────────

    public async Task<AvancementAssemblageDto> GetAvancementAsync(Guid execControleOfId, string posteCode, string? equipe = null)
    {
        var ctx = await ChargerAsync(execControleOfId, equipe: equipe);
        if (!ctx.Postes.Contains(posteCode))
            throw new RegleMetierException($"Le poste {posteCode} n'est pas affecté à cet OF.");

        var evaluations = new Dictionary<string, EvaluationPoste>();
        foreach (var poste in ctx.Postes)
            evaluations[poste] = await EvaluerPosteAsync(ctx, poste);

        var phase = Phase(ctx, evaluations[posteCode]);
        var famille = await _plans.GetFamilleArticleAsync(ctx.CodeArticle);

        var dto = new AvancementAssemblageDto
        {
            ExecControleOfId = execControleOfId,
            NumeroOf = ctx.ExecOf.NumeroOf,
            CodeArticle = ctx.CodeArticle,
            DesignationArticle = ctx.ExecOf.NumeroOfNavigation.CodeArticleNavigation?.Designation,
            FamilleProduitFiniCode = famille?.FamilleCode,
            TypeRobinetCode = famille?.TypeRobinetCode,
            StatutOf = ctx.ExecOf.Statut,
            Phase = phase,
            Equipe = ctx.Session?.Equipe,
            PremiereEquipe = ctx.Equipe.PremiereEquipe,
            DemandeCloture = ctx.Session?.ClosingRequest,
            EnPauseDepuis = ctx.Session?.Statut == StatutsSession.EnPause ? ctx.Session.SuspendedAt : null,
            PauseDejeunerPrise = ctx.Session?.HadLunchBreak ?? false,
            EquipeEnProduction = ctx.SessionsOuvertes
                .FirstOrDefault(s => s.Statut == StatutsSession.EnCours && s.Id != ctx.Session?.Id)?.Equipe,
            EquipesEnPause = ctx.SessionsOuvertes
                .Where(s => s.Statut == StatutsSession.EnPause && s.Id != ctx.Session?.Id)
                .Select(s => s.Equipe)
                .ToList(),
            PosteCode = posteCode,
            ReglageEnCours = evaluations[posteCode].ReglageEnCours,
            Etapes = Etapes(phase, ctx.Echantillonnage),
            Taches = TachesDePhase(phase, ctx, evaluations[posteCode]),
            ControlesALaDemande = PhasesAvecSaisie.Contains(phase) ? evaluations[posteCode].ControlesALaDemande : new List<TacheAssemblageDto>(),
            Postes = ctx.Postes.Select(p => new PosteAvancementDto
            {
                PosteCode = p,
                TachesRestantes = TachesDePhase(Phase(ctx, evaluations[p]), ctx, evaluations[p]).Count(EstBloquanteRestante)
            }).ToList()
        };

        if (ctx.Session?.ClosingRequest != null)
        {
            dto.Manquants = Manquants(ctx, evaluations);
            dto.PeutTerminer = dto.Manquants.Count == 0;
        }
        return dto;
    }

    public async Task<Dictionary<string, string>> GetPhasesPostesAsync(Guid execControleOfId, string equipe)
    {
        var ctx = await ChargerAsync(execControleOfId, equipe: equipe);
        var phases = new Dictionary<string, string>();
        foreach (var poste in ctx.Postes)
            phases[poste] = Phase(ctx, await EvaluerPosteAsync(ctx, poste));
        return phases;
    }

    // ───────────────────────────── Fin de poste / fin d'OF ─────────────────────────────

    public async Task DemanderClotureAsync(Guid execControleOfId, string mode)
    {
        if (!DemandesCloture.Autorisees.Contains(mode))
            throw new RegleMetierException("La demande de fin doit valoir FIN_POSTE ou FIN_OF.");

        var session = await SessionModifiableAsync(execControleOfId);
        if (session.Statut == StatutsSession.EnPause)
            throw new EtatExecutionInvalideException("Reprenez la production avant de demander la fin.");

        session.ClosingRequest = mode;
        await _operateur.SaveChangesAsync();
    }

    public async Task AnnulerDemandeClotureAsync(Guid execControleOfId)
    {
        var session = await SessionModifiableAsync(execControleOfId);
        session.ClosingRequest = null;
        await _operateur.SaveChangesAsync();
    }

    public async Task TerminerAsync(Guid execControleOfId)
    {
        var session = await SessionModifiableAsync(execControleOfId);
        var mode = session.ClosingRequest
            ?? throw new RegleMetierException("Demandez d'abord la fin de poste ou la clôture de l'OF.");

        await VerifierFinPossibleAsync(execControleOfId, mode);

        var termine = mode == DemandesCloture.FinOf
            ? await _executionOf.CloturerOfAsync(execControleOfId)
            : await _executionOf.TerminerPosteAsync(execControleOfId);
        if (!termine)
            throw new EtatExecutionInvalideException("La session de l'équipe n'a pas pu être clôturée. Rafraîchissez l'écran.");
    }

    public async Task VerifierFinPossibleAsync(Guid execControleOfId, string mode)
    {
        var ctx = await ChargerAsync(execControleOfId, demandeCloture: mode);
        if (ctx.ExecOf.TypeOf != TypesOf.Assemblage || ctx.Session == null) return;

        var evaluations = new Dictionary<string, EvaluationPoste>();
        foreach (var poste in ctx.Postes)
            evaluations[poste] = await EvaluerPosteAsync(ctx, poste);

        var manquants = Manquants(ctx, evaluations);
        if (manquants.Count > 0)
            throw new RegleMetierException($"Fin impossible, il reste à faire : {string.Join(" ; ", manquants)}.");
    }

    private async Task<ExecControleOfSession> SessionModifiableAsync(Guid execControleOfId)
    {
        var execOf = await _plans.GetExecOfAsync(execControleOfId)
            ?? throw new RessourceIntrouvableException("Exécution OF introuvable.");
        if (execOf.Statut == StatutsExecution.Cloture)
            throw new EtatExecutionInvalideException("Cet OF est clôturé.");
        return await _operateur.GetActiveSessionAsync(execControleOfId)
            ?? throw new EtatExecutionInvalideException("Aucune équipe n'est active sur cet OF.");
    }

    // ───────────────────────────── Calcul ─────────────────────────────

    /// <summary>
    /// Données de l'OF pour une équipe (par défaut celle qui a la main) ; <c>demandeCloture</c> remplace la fin
    /// enregistrée sur la session pour vérifier une fin directe.
    /// </summary>
    private async Task<Contexte> ChargerAsync(Guid execControleOfId, string? demandeCloture = null, string? equipe = null)
    {
        var execOf = await _plans.GetExecOfAsync(execControleOfId)
            ?? throw new RessourceIntrouvableException("Exécution OF introuvable.");
        if (execOf.TypeOf != TypesOf.Assemblage && demandeCloture == null)
            throw new RegleMetierException("Cet OF n'est pas un OF d'assemblage.");

        var sessionsOuvertes = await _plans.GetSessionsOuvertesAsync(execControleOfId);
        // Équipe demandée : sa session ouverte (en production ou en pause) ; une équipe qui a terminé son poste
        // n'en a plus. Sans équipe : la session qui a la main.
        var session = string.IsNullOrWhiteSpace(equipe)
            ? await _plans.GetActiveSessionAsync(execControleOfId)
            : sessionsOuvertes.FirstOrDefault(s => string.Equals(s.Equipe, equipe, StringComparison.OrdinalIgnoreCase));
        var premiere = await _plans.GetPremiereSessionIdAsync(execControleOfId);
        var statuts = await _plans.GetStatutsOfAsync(execControleOfId);

        return new Contexte
        {
            ExecOf = execOf,
            Session = session,
            SessionsOuvertes = sessionsOuvertes,
            Equipe = session == null
                ? new ContexteEquipe(Guid.Empty, false, null)
                : new ContexteEquipe(session.Id, premiere == session.Id, demandeCloture ?? session.ClosingRequest),
            Statuts = statuts,
            PeriodicitesMachines = await _operateur.GetMachinesPlanPeriodicitesAsync(),
            Postes = execOf.ExecControleOfPostes.Select(p => p.PosteCode).Distinct().OrderBy(p => p).ToList(),
            Echantillonnage = await TacheEchantillonnageAsync(statuts),
            IntervalleControle = CalendrierProduction.IntervalleMinutes(await _plans.GetEffectifParPosteAsync(execControleOfId))
        };
    }

    private static string Phase(Contexte ctx, EvaluationPoste poste)
    {
        if (ctx.ExecOf.Statut == StatutsExecution.Cloture) return PhasesAssemblage.Cloture;
        if (ctx.Session == null) return PhasesAssemblage.SansEquipe;
        if (ctx.Session.Statut == StatutsSession.EnPause) return PhasesAssemblage.Pause;
        if (ctx.Session.ClosingRequest == DemandesCloture.FinOf) return PhasesAssemblage.FinOf;
        if (ctx.Session.ClosingRequest == DemandesCloture.FinPoste) return PhasesAssemblage.FinPoste;
        if (ctx.Echantillonnage.Etat != EtatsTache.Fait) return PhasesAssemblage.Echantillonnage;
        if (!EstFaite(poste.VerifMachineDemarrage) || !EstFaite(poste.PremierePiece)) return PhasesAssemblage.Demarrage;
        if (poste.VerifMachineApresPause != null && !EstFaite(poste.VerifMachineApresPause)) return PhasesAssemblage.Reprise;
        return PhasesAssemblage.Production;
    }

    private static List<TacheAssemblageDto> TachesDePhase(string phase, Contexte ctx, EvaluationPoste poste)
    {
        var taches = phase switch
        {
            PhasesAssemblage.Echantillonnage => new[] { ctx.Echantillonnage },
            PhasesAssemblage.Demarrage => new[] { poste.VerifMachineDemarrage, poste.PremierePiece },
            PhasesAssemblage.Reprise => new[] { poste.VerifMachineApresPause },
            // Le registre de traçabilité se remplit pendant la production (choix des lots) ; il devient
            // obligatoire en fin de poste.
            PhasesAssemblage.Production => new[] { poste.Production, Copie(poste.ControlePoste, bloquante: false), Copie(poste.Tracabilite, bloquante: false) },
            PhasesAssemblage.FinPoste or PhasesAssemblage.FinOf => new[]
            {
                poste.VerifMachineFinPoste, poste.DernierePiece, Copie(poste.ControlePoste, bloquante: true), poste.Tracabilite
            },
            _ => Array.Empty<TacheAssemblageDto?>()
        };
        // Une tâche sans rien à faire à ce moment n'est pas affichée.
        return taches.Where(t => t != null && t.Etat != EtatsTache.NonRequis).Select(t => t!).ToList();
    }

    private static List<string> Manquants(Contexte ctx, Dictionary<string, EvaluationPoste> evaluations)
    {
        var phase = ctx.Equipe.DemandeCloture == DemandesCloture.FinOf ? PhasesAssemblage.FinOf : PhasesAssemblage.FinPoste;
        var manquants = evaluations
            .SelectMany(e => TachesDePhase(phase, ctx, e.Value)
                .Where(EstBloquanteRestante)
                .Select(t => $"{e.Key} : {t.Libelle}"))
            .Distinct()
            .ToList();

        // Clôture de l'OF : chaque équipe encore ouverte (en pause) doit d'abord reprendre et faire sa fin de
        // poste, avec ses propres contrôles de fin.
        if (phase == PhasesAssemblage.FinOf)
        {
            manquants.AddRange(ctx.SessionsOuvertes
                .Where(s => s.Id != ctx.Session?.Id)
                .Select(s => $"Équipe {s.Equipe} (en pause) : reprendre et faire sa fin de poste"));
        }
        return manquants;
    }

    private static List<EtapeAssemblageDto> Etapes(string phase, TacheAssemblageDto echantillonnage)
    {
        var rangCourant = phase switch
        {
            PhasesAssemblage.Echantillonnage => 0,
            PhasesAssemblage.Demarrage => 1,
            PhasesAssemblage.Production or PhasesAssemblage.Reprise or PhasesAssemblage.Pause => 2,
            PhasesAssemblage.FinPoste or PhasesAssemblage.FinOf => 3,
            PhasesAssemblage.Cloture => 4,
            _ => echantillonnage.Etat == EtatsTache.Fait ? 1 : 0
        };
        var libelleFin = phase == PhasesAssemblage.FinOf || phase == PhasesAssemblage.Cloture ? "Fin d'OF" : "Fin de poste";
        var etapes = new[]
        {
            ("ECHANTILLONNAGE", "Échantillonnage"), ("DEMARRAGE", "Démarrage"), ("PRODUCTION", "Production"), ("FIN", libelleFin)
        };
        return etapes.Select((e, rang) => new EtapeAssemblageDto
        {
            Code = e.Item1,
            Libelle = e.Item2,
            Etat = rang < rangCourant ? "FAIT" : rang == rangCourant ? "EN_COURS" : "A_VENIR"
        }).ToList();
    }

    private async Task<EvaluationPoste> EvaluerPosteAsync(Contexte ctx, string posteCode)
    {
        var machines = await MachinesDuPosteAsync(ctx, posteCode);
        var (premiere, derniere, production, aLaDemande, reglageEnCours) = await TachesPlanAsync(ctx, posteCode);

        return new EvaluationPoste(
            VerifMachineDemarrage: TacheVerifMachine(ctx, posteCode, machines, PeriodicitesControle.Demarrage),
            PremierePiece: premiere,
            // Seule une pause déjeuner déclenche la vérification « après pause ».
            VerifMachineApresPause: ctx.Session?.HadLunchBreak == true
                ? TacheVerifMachine(ctx, posteCode, machines, PeriodicitesControle.ApresPause)
                : null,
            Production: production,
            ControlePoste: await TacheControlePosteAsync(ctx, posteCode),
            VerifMachineFinPoste: TacheVerifMachine(ctx, posteCode, machines, PeriodicitesControle.FinPoste),
            DernierePiece: derniere,
            Tracabilite: TacheTracabilite(ctx),
            ControlesALaDemande: aLaDemande,
            ReglageEnCours: reglageEnCours);
    }

    private async Task<TacheAssemblageDto> TacheEchantillonnageAsync(List<ExecControleDocumentStatut> statuts)
    {
        var tache = new TacheAssemblageDto
        {
            Code = TachesAssemblage.Echantillonnage,
            Libelle = "Fiche d'échantillonnage de l'OF",
            Bloquante = true
        };

        // Une fiche par OF : la première équipe la remplit, les suivantes la trouvent faite.
        if (statuts.Any(s => s.TypeDocument == TypesDocumentExecution.Echantillonnage))
        {
            tache.Etat = EtatsTache.Fait;
            tache.Detail = "Fiche remplie.";
        }
        else if (await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync() == null)
        {
            tache.Etat = EtatsTache.Manquant;
            tache.Detail = "Aucun plan d'échantillonnage actif : signalez-le au responsable qualité.";
        }
        else
        {
            tache.Etat = EtatsTache.AFaire;
            tache.Detail = "Obligatoire avant les contrôles de démarrage.";
        }
        return tache;
    }

    /// <summary>Machines par défaut du poste et machines déjà vérifiées par l'équipe sur ce poste.</summary>
    private async Task<List<string>> MachinesDuPosteAsync(Contexte ctx, string posteCode)
    {
        var poste = await _operateur.GetPosteWithMachinesAsync(posteCode);
        var parDefaut = poste?.CodeMachines.Where(m => m.Actif).Select(m => m.CodeMachine) ?? Enumerable.Empty<string>();
        var verifiees = ctx.StatutsSession(posteCode)
            .Where(s => s.TypeDocument == TypesDocumentExecution.VerifMachine && s.MachineCode != null)
            .Select(s => s.MachineCode!);
        return parDefaut.Concat(verifiees).Distinct().OrderBy(m => m).ToList();
    }

    private static TacheAssemblageDto TacheVerifMachine(Contexte ctx, string posteCode, List<string> machines, string periodicite)
    {
        var statuts = ctx.StatutsSession(posteCode).ToList();
        var cloturee = statuts.Any(s => s.TypeDocument == TypesDocumentExecution.ClotureVerifMachinePoste);

        bool Prevue(string machine)
        {
            var moments = ctx.PeriodicitesMachines.TryGetValue(machine, out var liste) ? liste : new List<string>();
            return periodicite switch
            {
                PeriodicitesControle.Demarrage => PeriodiciteMachineHelper.ADemarrage(moments),
                PeriodicitesControle.ApresPause => PeriodiciteMachineHelper.AApresPause(moments),
                _ => PeriodiciteMachineHelper.AFinPoste(moments)
            };
        }

        bool Faite(string machine) => statuts
            .Where(s => s.TypeDocument == TypesDocumentExecution.VerifMachine && s.MachineCode == machine)
            .Any(s => periodicite switch
            {
                PeriodicitesControle.Demarrage => s.EstDemarrageTermine || s.EstTermine,
                PeriodicitesControle.ApresPause => s.EstPauseTermine || s.EstTermine,
                _ => s.EstTermine
            });

        var prevues = machines.Where(Prevue).ToList();
        var restantes = cloturee ? new List<string>() : prevues.Where(m => !Faite(m)).ToList();

        return new TacheAssemblageDto
        {
            Code = TachesAssemblage.VerifMachine,
            Periodicite = periodicite,
            Libelle = periodicite switch
            {
                PeriodicitesControle.Demarrage => "Vérification machine au démarrage",
                PeriodicitesControle.ApresPause => "Vérification machine après la pause déjeuner",
                _ => "Vérification machine en fin de poste"
            },
            Bloquante = true,
            Etat = restantes.Count == 0 ? EtatsTache.Fait : EtatsTache.AFaire,
            Detail = prevues.Count == 0
                ? "Aucune machine du poste n'a de vérification prévue à ce moment."
                : restantes.Count == 0
                    ? $"{prevues.Count} machine(s) vérifiée(s)."
                    : $"À vérifier : {string.Join(", ", restantes)}."
        };
    }

    /// <summary>Première pièce, dernière pièce et contrôles de production du plan d'assemblage du poste.</summary>
    private async Task<(TacheAssemblageDto Premiere, TacheAssemblageDto Derniere, TacheAssemblageDto Production, List<TacheAssemblageDto> ALaDemande, bool ReglageEnCours)> TachesPlanAsync(Contexte ctx, string posteCode)
    {
        var statutPlan = ctx.StatutsSession(posteCode).FirstOrDefault(s => s.TypeDocument == TypesDocumentExecution.PlanAssemblage);
        var planId = statutPlan?.DocId ?? await _plans.TrouverPlanAssemblageIdAsync(ctx.CodeArticle, posteCode);
        var plan = planId.HasValue ? await _plans.GetDocumentByIdAsync(planId.Value) : null;

        if (plan == null)
        {
            var manquant = ExecPlanAssemblageService.MessagePlanIntrouvable(ctx.CodeArticle, posteCode);
            // Sans plan, rien à contrôler sur ce poste : le démarrage reste bloqué (signalement au superviseur),
            // mais la fin de poste n'attend pas un contrôle qui n'existe pas.
            TacheAssemblageDto PlanManquant(string code, string libelle, bool bloquante) => new()
            {
                Code = code, Libelle = libelle, Etat = EtatsTache.Manquant, Bloquante = bloquante, Detail = manquant
            };
            return (PlanManquant(TachesAssemblage.PlanPremierePiece, "Plan d'assemblage : contrôle de la première pièce", bloquante: true),
                    PlanManquant(TachesAssemblage.PlanDernierePiece, "Plan d'assemblage : contrôle de la dernière pièce", bloquante: false),
                    PlanManquant(TachesAssemblage.ControlesProduction, "Contrôles de production", bloquante: false),
                    new List<TacheAssemblageDto>(), false);
        }

        var reponses = await _plans.GetReponsesPlanAsync(ctx.ExecOf.Id, posteCode);
        var sections = ExecPlanAssemblageService.SectionsLot(plan, ctx.Equipe, reponses);

        TacheAssemblageDto TacheLot(string code, string libelle, Func<DTOs.Execution.ExecPlanAssemblageSectionDto, DTOs.Execution.ControleLotDto> controle,
            Func<string, string> moment)
        {
            var attendues = sections.Where(s => controle(s).Requis).ToList();
            var restantes = attendues.Where(s => !controle(s).Fait).ToList();
            return new TacheAssemblageDto
            {
                Code = code,
                Libelle = libelle,
                Bloquante = true,
                // Plan sans section LOT_POSTE / LOT_OF attendue à ce moment : rien à saisir, l'étape continue.
                Etat = attendues.Count == 0 ? EtatsTache.NonRequis : restantes.Count == 0 ? EtatsTache.Fait : EtatsTache.AFaire,
                Detail = attendues.Count == 0
                    ? "Aucun contrôle de lot attendu dans le plan à ce moment."
                    : restantes.Count == 0
                        ? "Fait."
                        : $"À faire : {string.Join(", ", restantes.Select(s => moment(s.TypeSection)).Distinct())}."
            };
        }

        var premiere = TacheLot(TachesAssemblage.PlanPremierePiece, "Plan d'assemblage : contrôle de la première pièce",
            s => s.PremierePiece, type => type == SectionTypes.LotOf ? "début d'OF" : "début de poste");
        var derniere = TacheLot(TachesAssemblage.PlanDernierePiece, "Plan d'assemblage : contrôle de la dernière pièce",
            s => s.DernierePiece, type => type == SectionTypes.LotOf ? "fin d'OF" : "fin de poste");

        var (production, reglageEnCours) = await TacheProductionAsync(ctx, posteCode, plan, reponses);
        return (premiere, derniere, production, ControlesALaDemande(plan, ctx, reponses, reglageEnCours), reglageEnCours);
    }

    /// <summary>
    /// Contrôles de production planifiés par le serveur pour l'équipe sur le poste : jamais « faits » tant que la
    /// production dure ; sans contrôle dû, la tâche annonce le prochain. Un réglage en cours les suspend.
    /// </summary>
    private async Task<(TacheAssemblageDto Tache, bool ReglageEnCours)> TacheProductionAsync(Contexte ctx, string posteCode,
        DocumentEntete plan, IReadOnlyCollection<ReponseLot> reponses)
    {
        var tache = new TacheAssemblageDto
        {
            Code = TachesAssemblage.ControlesProduction,
            Libelle = "Contrôles de production (ISO 2859-1)",
            Bloquante = false,
            Etat = EtatsTache.AFaire
        };
        var sectionsProduction = plan.DocumentSections
            .Where(s => ExecPlanAssemblageService.TypeDe(s) == SectionTypes.Echantillonnage2859)
            .ToList();
        if (!sectionsProduction.Any(CalendrierProduction.EstPlanifiable))
        {
            tache.Etat = EtatsTache.NonRequis;
            tache.Detail = sectionsProduction.Count == 0
                ? "Aucun contrôle de production dans le plan."
                : "Contrôles par série : aucun contrôle planifié dans le temps.";
            return (tache, false);
        }
        if (ctx.Session == null)
        {
            tache.Detail = "Aucune équipe sur cet OF.";
            return (tache, false);
        }

        var periodes = await _plans.GetPeriodesProductionAsync(ctx.Session.Id, posteCode);
        var sectionsReglage = plan.DocumentSections.Where(s => ControlesLot.EstSectionReglage(ExecPlanAssemblageService.TypeDe(s))).ToList();
        var saisiesReglage = CalendrierProduction.SaisiesReglage(reponses, ctx.Session.Id,
            sectionsReglage.SelectMany(s => s.DocumentLignes.Select(l => (Ligne: l.Id, Section: s.Id))).ToDictionary(x => x.Ligne, x => x.Section));
        var reglageEnCours = CalendrierProduction.ReglageEnCours(periodes, saisiesReglage, sectionsReglage.Select(s => s.Id).ToList());
        var aFaire = await _plans.CompterOccurrencesAFaireAsync(ctx.Session.Id, posteCode);
        var prochain = CalendrierProduction.ProchainControle(periodes.FirstOrDefault(p => p.EndedAt == null));

        tache.Compteur = aFaire;
        tache.Detail = string.Join(" ", new[]
        {
            aFaire > 0 ? $"{aFaire} contrôle(s) à faire maintenant." : null,
            reglageEnCours
                ? "Réglage en cours : contrôles suspendus jusqu'à un réglage conforme sur tous les critères."
                : ctx.IntervalleControle == null
                    ? "Pas d'effectif par poste dans la fiche d'échantillonnage : aucun contrôle planifié."
                    : prochain.HasValue
                        ? $"{(aFaire > 0 ? "Suivant" : "Aucun contrôle dû : prochain contrôle")} à {prochain.Value:HH:mm} (une pièce toutes les {ctx.IntervalleControle.Value:0.#} min)."
                        : aFaire > 0 ? null : "Les contrôles démarrent avec la production du poste."
        }.Where(t => t != null));
        return (tache, reglageEnCours);
    }

    /// <summary>Contrôles aux réglages et de toutes les pièces : l'opérateur choisit quand les faire (jamais bloquants).</summary>
    private static List<TacheAssemblageDto> ControlesALaDemande(DocumentEntete plan, Contexte ctx, IReadOnlyCollection<ReponseLot> reponses,
        bool reglageEnCours)
    {
        var sections = ExecPlanAssemblageService.SectionsALaDemande(plan, ctx.Equipe, reponses);
        return new[]
            {
                (Code: TachesAssemblage.Reglage, Libelle: "Contrôle aux réglages", Contexte: ContextesReponse.Reglage),
                (Code: TachesAssemblage.ToutesPieces, Libelle: "Contrôle de toutes les pièces", Contexte: ContextesReponse.CentPourCent)
            }
            .Select(g => (g, Sections: sections.Where(s => s.Contexte == g.Contexte).ToList()))
            .Where(x => x.Sections.Count > 0)
            .Select(x =>
            {
                var dernier = x.Sections.Select(s => s.DernierControle).Where(c => c.Fait).MaxBy(c => c.DateSaisie);
                var enCours = reglageEnCours && x.g.Code == TachesAssemblage.Reglage;
                return new TacheAssemblageDto
                {
                    Code = x.g.Code,
                    Libelle = x.g.Libelle,
                    Bloquante = false,
                    Etat = dernier != null && !enCours ? EtatsTache.Fait : EtatsTache.AFaire,
                    Compteur = x.Sections.Sum(s => s.NombreControlesEquipe),
                    Detail = enCours
                        ? "Réglage en cours : terminez-le par un contrôle conforme sur tous les critères."
                        : dernier == null
                            ? "À faire quand vous le jugez nécessaire."
                            : $"Dernier contrôle à {dernier.DateSaisie:HH:mm} : {(dernier.Resultat == ResultatsControle.NonConforme ? "non conforme" : "conforme")}."
                };
            })
            .ToList();
    }

    /// <summary>
    /// Contrôle au poste : document du jour partagé par les équipes ; fait pour l'équipe quand elle a validé
    /// ses heures (ou quand le poste a été clôturé après son arrivée).
    /// </summary>
    private async Task<TacheAssemblageDto?> TacheControlePosteAsync(Contexte ctx, string posteCode)
    {
        if (ctx.Session == null) return null;
        if (await _controlePoste.GetPlanActifByPosteAsync(posteCode, ctx.ExecOf.Id) == null)
        {
            // Sans document actif pour le poste, rien ne peut être rempli : à signaler, sans bloquer la fin de poste.
            return new TacheAssemblageDto
            {
                Code = TachesAssemblage.ControlePoste,
                Libelle = "Résultat du contrôle au poste",
                Etat = EtatsTache.Manquant,
                Detail = $"Aucun document de contrôle au poste actif pour {posteCode} : signalez-le au superviseur."
            };
        }

        var debut = ctx.Session.DateDebut;
        var fait = ctx.Statuts.Any(s => s.PosteCode == posteCode && s.TypeDocument == TypesDocumentExecution.ClotureResultatControlePoste &&
            (s.SessionId == ctx.Session.Id || (s.SessionId == null && s.DateExecution >= debut)));

        return new TacheAssemblageDto
        {
            Code = TachesAssemblage.ControlePoste,
            Libelle = "Résultat du contrôle au poste",
            Etat = fait ? EtatsTache.Fait : EtatsTache.AFaire,
            Detail = fait
                ? "Heures de l'équipe validées."
                : "Document du jour, commun aux équipes : remplissez vos heures puis validez-les avant la fin de poste."
        };
    }

    /// <summary>Registre de traçabilité : commun à l'OF, fait pour l'équipe quand elle a confirmé ses lots.</summary>
    private static TacheAssemblageDto TacheTracabilite(Contexte ctx)
    {
        var fait = ctx.Session != null && ctx.Statuts.Any(s =>
            s.TypeDocument == TypesDocumentExecution.RegistreTracabilite && s.SessionId == ctx.Session.Id && s.EstTermine);
        return new TacheAssemblageDto
        {
            Code = TachesAssemblage.Tracabilite,
            Libelle = "Registre de traçabilité",
            Bloquante = true,
            Etat = fait ? EtatsTache.Fait : EtatsTache.AFaire,
            Detail = fait ? "Lots de l'équipe confirmés." : "Lots utilisés (corps, volant, composants) : à confirmer avant la fin de poste."
        };
    }

    /// <summary>Copie d'une tâche avec son caractère bloquant pour l'étape (un document manquant ne bloque jamais).</summary>
    private static TacheAssemblageDto? Copie(TacheAssemblageDto? tache, bool bloquante) => tache == null ? null : new TacheAssemblageDto
    {
        Code = tache.Code, Libelle = tache.Libelle, Etat = tache.Etat, Detail = tache.Detail,
        Periodicite = tache.Periodicite, Compteur = tache.Compteur,
        Bloquante = bloquante && tache.Etat != EtatsTache.Manquant
    };

    private static bool EstFaite(TacheAssemblageDto tache) => tache.Etat is EtatsTache.Fait or EtatsTache.NonRequis;

    private static bool EstBloquanteRestante(TacheAssemblageDto tache) => tache.Bloquante && !EstFaite(tache);
}
