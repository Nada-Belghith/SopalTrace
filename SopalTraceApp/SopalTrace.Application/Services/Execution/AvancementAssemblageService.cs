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
/// Avancement d'un OF d'assemblage, poste par poste, seule source de vérité de l'assistant opérateur. Chaque poste
/// suit son propre parcours, indépendamment des autres postes de l'OF :
/// <list type="number">
/// <item>Démarrage du poste par l'équipe (enregistré en base), puis fiche d'échantillonnage du poste (une par poste de
/// l'OF), vérification machine (démarrage) et première pièce des sections de lot (LOT_POSTE à chaque démarrage,
/// LOT_OF au premier démarrage du poste sur l'OF) ;</item>
/// <item>Production : contrôles ECHANTILLONNAGE_2859 planifiés par le serveur (<see cref="CalendrierProduction"/>) ;
/// après une pause déjeuner, vérification machine « après pause » avant de reprendre ;</item>
/// <item>Fin de poste (demandée sur ce poste seulement) : vérification machine de fin de poste, dernière pièce,
/// contrôle au poste et registre de traçabilité ; le poste terminé, une autre équipe peut le prendre ;</item>
/// <item>Clôture de l'OF : les tâches de fin de chaque poste que l'équipe tient, et la dernière pièce « fin d'OF »
/// sur chaque poste démarré au moins une fois.</item>
/// </list>
/// Plusieurs équipes peuvent travailler en même temps sur l'OF, une seule à la fois par poste.
/// </summary>
public class AvancementAssemblageService : IAvancementAssemblageService
{
    private readonly IExecPlanAssemblageRepository _plans;
    private readonly IOperateurRepository _operateur;
    private readonly IExecRcPosteRepository _controlePoste;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExecutionOfService _executionOf;
    private readonly IDemarragePosteRepository _demarrages;

    public AvancementAssemblageService(IExecPlanAssemblageRepository plans, IOperateurRepository operateur,
        IExecRcPosteRepository controlePoste, IUnitOfWork unitOfWork, IExecutionOfService executionOf,
        IDemarragePosteRepository demarrages)
    {
        _demarrages = demarrages;
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
        /// <summary>Session de l'équipe considérée (en production ou en pause) ; null si aucune.</summary>
        public required ExecControleOfSession? Session { get; init; }
        /// <summary>Postes tenus par les sessions de l'OF (toutes équipes).</summary>
        public required List<DemarragePoste> Demarrages { get; init; }
        public required List<ExecControleDocumentStatut> Statuts { get; init; }
        public required Dictionary<string, List<string>> PeriodicitesMachines { get; init; }
        public required List<string> Postes { get; init; }
        /// <summary>Un plan d'échantillonnage est actif (sinon la fiche d'un poste ne peut pas être remplie).</summary>
        public required bool PlanEchantillonnageActif { get; init; }
        public DateTime Maintenant { get; } = DateTime.Now;

        public string CodeArticle => ExecOf.NumeroOfNavigation.CodeArticle;

        public ContexteEquipe EquipePour(string posteCode) => ContexteEquipe.Pour(Session, posteCode, Demarrages);

        /// <summary>Poste tenu (non terminé), par n'importe quelle équipe ; null s'il est libre.</summary>
        public DemarragePoste? Titulaire(string posteCode) => Demarrages.FirstOrDefault(d => d.PosteCode == posteCode && d.EnCours);

        public bool ClotureDemandee => Session?.ClosingRequest == DemandesCloture.FinOf;

        /// <summary>Statuts de la session active (vérification machine, plan d'assemblage).</summary>
        public IEnumerable<ExecControleDocumentStatut> StatutsSession(string posteCode) =>
            Session == null ? Enumerable.Empty<ExecControleDocumentStatut>()
                : Statuts.Where(s => s.SessionId == Session.Id && s.PosteCode == posteCode);
    }

    /// <summary>Tâches d'un poste, par moment du parcours.</summary>
    private sealed record EvaluationPoste(
        string PosteCode,
        ContexteEquipe Equipe,
        TacheAssemblageDto Echantillonnage,
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
        var ctx = await ChargerAsync(execControleOfId, posteCode, equipe);
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
            PosteDemarre = evaluations[posteCode].Equipe.PosteDemarre,
            PremierDemarragePoste = evaluations[posteCode].Equipe.PremierDemarrage,
            DemandeCloture = phase is PhasesAssemblage.FinPoste or PhasesAssemblage.FinOf ? evaluations[posteCode].Equipe.DemandeCloture : null,
            EquipeTitulaire = ctx.Titulaire(posteCode)?.Equipe,
            EnPauseDepuis = ctx.Session?.Statut == StatutsSession.EnPause ? ctx.Session.SuspendedAt : null,
            PauseDejeunerPrise = ctx.Session?.HadLunchBreak ?? false,
            PosteCode = posteCode,
            ReglageEnCours = evaluations[posteCode].ReglageEnCours,
            // En pause, la frise montre l'étape où le poste a été laissé (échantillonnage, démarrage, production, fin).
            Etapes = Etapes(phase == PhasesAssemblage.Pause ? Phase(ctx, evaluations[posteCode], horsPause: true) : phase,
                evaluations[posteCode].Echantillonnage),
            Taches = TachesDePhase(phase, ctx, evaluations[posteCode]),
            ControlesALaDemande = PhasesAvecSaisie.Contains(phase) && evaluations[posteCode].Equipe.PosteDemarre
                ? evaluations[posteCode].ControlesALaDemande : new List<TacheAssemblageDto>(),
            Postes = ctx.Postes.Select(p =>
            {
                var phasePoste = Phase(ctx, evaluations[p]);
                return new PosteAvancementDto
                {
                    PosteCode = p,
                    Demarre = evaluations[p].Equipe.PosteDemarre,
                    Phase = phasePoste,
                    EquipeTitulaire = ctx.Titulaire(p)?.Equipe,
                    TachesRestantes = TachesDePhase(phasePoste, ctx, evaluations[p]).Count(EstBloquanteRestante)
                };
            }).ToList()
        };

        // Fin de poste : tâches de fin de ce poste ; clôture de l'OF : celles de tous les postes.
        if (phase is PhasesAssemblage.FinPoste or PhasesAssemblage.FinOf)
        {
            dto.Manquants = Manquants(ctx, evaluations, phase == PhasesAssemblage.FinOf ? null : posteCode);
            dto.PeutTerminer = dto.Manquants.Count == 0;
        }
        return dto;
    }

    public async Task<Dictionary<string, string>> GetPhasesPostesAsync(Guid execControleOfId, string equipe)
    {
        var ctx = await ChargerAsync(execControleOfId, null, equipe);
        var phases = new Dictionary<string, string>();
        foreach (var poste in ctx.Postes)
            phases[poste] = Phase(ctx, await EvaluerPosteAsync(ctx, poste));
        return phases;
    }

    /// <summary>
    /// L'équipe prend un poste libre de l'OF : ses contrôles de démarrage (et le « début d'OF » si c'est le premier
    /// démarrage du poste sur l'OF) deviennent dus, puis ses contrôles de fin de poste.
    /// </summary>
    public async Task DemarrerPosteAsync(Guid execControleOfId, string posteCode, string? equipe, string matricule)
    {
        var execOf = await ExecOfOuvertAsync(execControleOfId);
        VerifierPosteAffecte(execOf, posteCode);
        var session = await SessionEquipeAsync(execControleOfId, equipe);
        if (session.Statut == StatutsSession.EnPause)
            throw new EtatExecutionInvalideException("Reprenez la production avant de démarrer un poste.");
        if (session.ClosingRequest == DemandesCloture.FinOf)
            throw new EtatExecutionInvalideException("La clôture de l'OF est demandée : annulez-la pour démarrer un poste.");

        var titulaire = (await _demarrages.GetDemarragesAsync(execControleOfId)).FirstOrDefault(d => d.PosteCode == posteCode && d.EnCours);
        if (titulaire?.SessionId == session.Id) return;
        if (titulaire != null)
            throw new EtatExecutionInvalideException(MessagePosteOccupe(posteCode, titulaire.Equipe));
        if (await _demarrages.GetPosteSessionAsync(session.Id, posteCode) != null)
            throw new EtatExecutionInvalideException(
                $"Votre équipe a déjà terminé le poste {posteCode} dans cette prise de poste : terminez vos autres postes, puis ouvrez une nouvelle prise de poste pour le reprendre.");

        _demarrages.Ajouter(new ExecSessionPost
        {
            Id = Guid.NewGuid(), SessionId = session.Id, ExecControleOfId = execControleOfId, PosteCode = posteCode,
            StartedAt = DateTime.Now, StartedBy = matricule
        });
        await _demarrages.SaveChangesAsync();
    }

    public static string MessagePosteOccupe(string posteCode, string? equipe) =>
        $"Le poste {posteCode} est tenu par l'équipe {equipe ?? "en cours"} : elle doit le terminer avant qu'une autre équipe le prenne.";

    // ───────────────────────────── Fin de poste / clôture de l'OF ─────────────────────────────

    public async Task DemanderClotureAsync(Guid execControleOfId, string mode, string posteCode, string? equipe = null)
    {
        if (!DemandesCloture.Autorisees.Contains(mode))
            throw new RegleMetierException("La demande de fin doit valoir FIN_POSTE ou FIN_OF.");

        var execOf = await ExecOfOuvertAsync(execControleOfId);
        VerifierPosteAffecte(execOf, posteCode);
        var session = await SessionDuPosteAsync(execControleOfId, posteCode, equipe);
        if (session.Statut == StatutsSession.EnPause)
            throw new EtatExecutionInvalideException("Reprenez la production avant de demander la fin.");

        if (mode == DemandesCloture.FinOf)
        {
            // Clôture : l'équipe doit être seule sur l'OF (les autres ont terminé leurs postes).
            var autres = (await _demarrages.GetSessionsOuvertesAsync(execControleOfId)).Where(s => s.Id != session.Id).ToList();
            if (autres.Count > 0)
            {
                var tenus = (await _demarrages.GetDemarragesAsync(execControleOfId)).Where(d => d.EnCours && d.SessionId != session.Id).ToList();
                throw new EtatExecutionInvalideException(
                    "Clôture impossible : " + string.Join(" ; ", autres.Select(s =>
                    {
                        var postes = tenus.Where(t => t.SessionId == s.Id).Select(t => t.PosteCode).ToList();
                        return $"l'équipe {s.Equipe} {(postes.Count > 0 ? $"tient encore {string.Join(", ", postes)}" : "a une session ouverte")}";
                    })) + ". Chaque équipe doit terminer ses postes avant la clôture de l'OF.");
            }
            session.ClosingRequest = DemandesCloture.FinOf;
            await _operateur.SaveChangesAsync();
            return;
        }

        if (session.ClosingRequest == DemandesCloture.FinOf)
            throw new EtatExecutionInvalideException("La clôture de l'OF est déjà demandée : elle couvre la fin de tous les postes.");
        var poste = await _demarrages.GetPosteSessionAsync(session.Id, posteCode);
        if (poste == null)
            throw new EtatExecutionInvalideException($"Votre équipe n'a pas démarré le poste {posteCode} : il n'a pas de fin de poste.");
        if (poste.EndedAt != null)
            throw new EtatExecutionInvalideException($"Le poste {posteCode} est déjà terminé par votre équipe.");
        poste.ClosingRequestedAt ??= DateTime.Now;
        await _demarrages.SaveChangesAsync();
    }

    public async Task AnnulerDemandeClotureAsync(Guid execControleOfId, string posteCode, string? equipe = null)
    {
        await ExecOfOuvertAsync(execControleOfId);
        var session = await SessionDuPosteAsync(execControleOfId, posteCode, equipe);
        if (session.ClosingRequest == DemandesCloture.FinOf)
        {
            session.ClosingRequest = null;
            // Équipe venue seulement clôturer : sans poste en cours, sa session n'a plus d'objet.
            if ((await _demarrages.GetPostesEnCoursAsync(session.Id)).Count == 0)
            {
                session.Statut = StatutsSession.Termine;
                session.DateFin = DateTime.Now;
                session.SuspendedAt = null;
            }
            await _operateur.SaveChangesAsync();
            return;
        }
        var poste = await _demarrages.GetPosteSessionAsync(session.Id, posteCode);
        if (poste == null || poste.EndedAt != null) return;
        poste.ClosingRequestedAt = null;
        await _demarrages.SaveChangesAsync();
    }

    public async Task TerminerAsync(Guid execControleOfId, string posteCode, string? equipe, string matricule)
    {
        await ExecOfOuvertAsync(execControleOfId);
        var session = await SessionDuPosteAsync(execControleOfId, posteCode, equipe);

        if (session.ClosingRequest == DemandesCloture.FinOf)
        {
            await VerifierFinPossibleAsync(execControleOfId, DemandesCloture.FinOf, posteCode, session.Equipe);
            if (!await _executionOf.CloturerOfAsync(execControleOfId))
                throw new EtatExecutionInvalideException("L'OF n'a pas pu être clôturé. Rafraîchissez l'écran.");
            return;
        }

        var poste = await _demarrages.GetPosteSessionAsync(session.Id, posteCode);
        if (poste?.EndedAt != null) return;
        if (poste?.ClosingRequestedAt == null)
            throw new RegleMetierException("Demandez d'abord la fin de poste ou la clôture de l'OF.");
        await VerifierFinPossibleAsync(execControleOfId, DemandesCloture.FinPoste, posteCode, session.Equipe);
        if (!await _executionOf.TerminerPostesAsync(execControleOfId, session.Id, new[] { posteCode }, matricule))
            throw new EtatExecutionInvalideException("Le poste n'a pas pu être terminé. Rafraîchissez l'écran.");
    }

    public async Task TerminerPostesDirectementAsync(Guid execControleOfId, string? equipe, string? posteCode, string matricule, bool verifier)
    {
        await ExecOfOuvertAsync(execControleOfId);
        var session = posteCode != null || !string.IsNullOrWhiteSpace(equipe)
            ? await SessionDuPosteAsync(execControleOfId, posteCode ?? "", equipe)
            : await SessionEquipeAsync(execControleOfId, null);
        var postes = posteCode != null
            ? new List<string> { posteCode }
            : (await _demarrages.GetPostesEnCoursAsync(session.Id)).Select(p => p.PosteCode).ToList();
        if (verifier)
            foreach (var poste in postes)
                await VerifierFinPossibleAsync(execControleOfId, DemandesCloture.FinPoste, poste, session.Equipe);
        if (!await _executionOf.TerminerPostesAsync(execControleOfId, session.Id, postes, matricule))
            throw new EtatExecutionInvalideException($"L'équipe {session.Equipe} ne tient aucun de ces postes : rien à terminer.");
    }

    public async Task VerifierFinPossibleAsync(Guid execControleOfId, string mode, string? posteCode = null, string? equipe = null)
    {
        var execOf = await _plans.GetExecOfAsync(execControleOfId)
            ?? throw new RessourceIntrouvableException("Exécution OF introuvable.");
        if (execOf.TypeOf != TypesOf.Assemblage) return;

        // Clôture directe sans équipe précisée : celle de la seule équipe ouverte ; jamais pendant qu'une autre travaille.
        if (mode == DemandesCloture.FinOf && posteCode == null && string.IsNullOrWhiteSpace(equipe))
        {
            var ouvertes = await _demarrages.GetSessionsOuvertesAsync(execControleOfId);
            if (ouvertes.Count > 1)
                throw new RegleMetierException(
                    $"Clôture impossible : les équipes {string.Join(", ", ouvertes.Select(s => s.Equipe))} travaillent encore sur l'OF.");
            equipe = ouvertes.FirstOrDefault()?.Equipe;
        }

        var ctx = await ChargerAsync(execControleOfId, posteCode, equipe);
        if (ctx.Session == null) return;

        // Fin vérifiée telle qu'elle serait demandée (fin directe sans demande préalable comprise).
        var evaluations = new Dictionary<string, EvaluationPoste>();
        foreach (var poste in ctx.Postes)
            evaluations[poste] = await EvaluerPosteAsync(ctx, poste, mode);

        var postes = mode == DemandesCloture.FinOf ? null
            : posteCode != null ? new[] { posteCode }
            : ctx.Postes.Where(p => evaluations[p].Equipe.PosteDemarre && !evaluations[p].Equipe.PosteTermine).ToArray();
        var manquants = postes == null
            ? Manquants(ctx, evaluations, null)
            : postes.SelectMany(p => Manquants(ctx, evaluations, p)).ToList();
        if (manquants.Count > 0)
            throw new RegleMetierException($"Fin impossible, il reste à faire : {string.Join(" ; ", manquants)}.");
    }

    private async Task<ExecControleOf> ExecOfOuvertAsync(Guid execControleOfId)
    {
        var execOf = await _plans.GetExecOfAsync(execControleOfId)
            ?? throw new RessourceIntrouvableException("Exécution OF introuvable.");
        if (execOf.Statut == StatutsExecution.Cloture)
            throw new EtatExecutionInvalideException("Cet OF est clôturé.");
        return execOf;
    }

    private static void VerifierPosteAffecte(ExecControleOf execOf, string posteCode)
    {
        if (!execOf.ExecControleOfPostes.Any(p => p.PosteCode == posteCode))
            throw new RegleMetierException($"Le poste {posteCode} n'est pas affecté à cet OF.");
    }

    /// <summary>Session de l'équipe de la tablette (par défaut la seule équipe ouverte sur l'OF).</summary>
    private async Task<ExecControleOfSession> SessionEquipeAsync(Guid execControleOfId, string? equipe)
    {
        if (!string.IsNullOrWhiteSpace(equipe))
            return await _demarrages.GetSessionAsync(execControleOfId, null, equipe)
                ?? throw new EtatExecutionInvalideException($"L'équipe {equipe.Trim()} n'a pas de session ouverte sur cet OF.");
        var ouvertes = await _demarrages.GetSessionsOuvertesAsync(execControleOfId);
        return ouvertes.Count switch
        {
            0 => throw new EtatExecutionInvalideException("Aucune équipe n'est active sur cet OF."),
            1 => ouvertes[0],
            _ => throw new RegleMetierException("Plusieurs équipes travaillent sur cet OF : précisez la vôtre.")
        };
    }

    /// <summary>Session de l'équipe donnée, sinon celle qui tient le poste (ou qui clôture l'OF).</summary>
    private async Task<ExecControleOfSession> SessionDuPosteAsync(Guid execControleOfId, string posteCode, string? equipe) =>
        await _demarrages.GetSessionAsync(execControleOfId, posteCode, equipe)
        ?? throw new EtatExecutionInvalideException(string.IsNullOrWhiteSpace(equipe)
            ? $"Aucune équipe ne tient le poste {posteCode}."
            : $"L'équipe {equipe.Trim()} n'a pas de session ouverte sur cet OF.");

    // ───────────────────────────── Calcul ─────────────────────────────

    /// <summary>
    /// Données de l'OF vues par une équipe : celle donnée (si elle a une session ouverte), sinon celle qui tient le
    /// poste, sinon celle qui clôture l'OF (aucune si le poste est libre et l'équipe inconnue).
    /// </summary>
    private async Task<Contexte> ChargerAsync(Guid execControleOfId, string? posteCode, string? equipe)
    {
        var execOf = await _plans.GetExecOfAsync(execControleOfId)
            ?? throw new RessourceIntrouvableException("Exécution OF introuvable.");
        if (execOf.TypeOf != TypesOf.Assemblage)
            throw new RegleMetierException("Cet OF n'est pas un OF d'assemblage.");

        var session = await _demarrages.GetSessionAsync(execControleOfId, posteCode, equipe);
        var statuts = await _plans.GetStatutsOfAsync(execControleOfId);

        return new Contexte
        {
            ExecOf = execOf,
            Session = session,
            Demarrages = await _demarrages.GetDemarragesAsync(execControleOfId),
            Statuts = statuts,
            PeriodicitesMachines = await _operateur.GetMachinesPlanPeriodicitesAsync(),
            Postes = execOf.ExecControleOfPostes.Select(p => p.PosteCode).Distinct().OrderBy(p => p).ToList(),
            PlanEchantillonnageActif = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync() != null
        };
    }

    /// <param name="ctx">Données de l'OF.</param>
    /// <param name="poste">Évaluation du poste.</param>
    /// <param name="horsPause">Étape où en est le poste, sans tenir compte de la pause de l'équipe (frise des étapes).</param>
    private static string Phase(Contexte ctx, EvaluationPoste poste, bool horsPause = false)
    {
        if (ctx.ExecOf.Statut == StatutsExecution.Cloture) return PhasesAssemblage.Cloture;
        if (ctx.Session == null) return PhasesAssemblage.SansEquipe;
        if (ctx.Session.Statut == StatutsSession.EnPause && !horsPause) return PhasesAssemblage.Pause;
        if (poste.Equipe.DemandeCloture == DemandesCloture.FinOf) return PhasesAssemblage.FinOf;
        if (poste.Equipe.PosteTermine) return PhasesAssemblage.PosteTermine;
        if (poste.Equipe.DemandeCloture == DemandesCloture.FinPoste) return PhasesAssemblage.FinPoste;
        if (!poste.Equipe.PosteDemarre)
            return ctx.Titulaire(poste.PosteCode) != null ? PhasesAssemblage.PosteOccupe : PhasesAssemblage.NonDemarre;
        if (poste.Echantillonnage.Etat != EtatsTache.Fait) return PhasesAssemblage.Echantillonnage;
        if (!EstFaite(poste.VerifMachineDemarrage) || !EstFaite(poste.PremierePiece)) return PhasesAssemblage.Demarrage;
        if (poste.VerifMachineApresPause != null && !EstFaite(poste.VerifMachineApresPause)) return PhasesAssemblage.Reprise;
        return PhasesAssemblage.Production;
    }

    private static List<TacheAssemblageDto> TachesDePhase(string phase, Contexte ctx, EvaluationPoste poste)
    {
        var taches = phase switch
        {
            PhasesAssemblage.Echantillonnage => new[] { poste.Echantillonnage },
            PhasesAssemblage.NonDemarre => new[] { TacheDemarrerPoste(poste.PosteCode) },
            PhasesAssemblage.Demarrage => new[] { poste.VerifMachineDemarrage, poste.PremierePiece },
            PhasesAssemblage.Reprise => new[] { poste.VerifMachineApresPause },
            // Le registre de traçabilité se remplit pendant la production (choix des lots) ; il devient
            // obligatoire en fin de poste.
            PhasesAssemblage.Production => new[] { poste.Production, Copie(poste.ControlePoste, bloquante: false), Copie(poste.Tracabilite, bloquante: false) },
            // Poste démarré par l'équipe : tous ses contrôles de fin ; sinon, seul le « fin d'OF » éventuel.
            PhasesAssemblage.FinPoste or PhasesAssemblage.FinOf => poste.Equipe.PosteDemarre
                ? new[] { poste.VerifMachineFinPoste, poste.DernierePiece, Copie(poste.ControlePoste, bloquante: true), poste.Tracabilite }
                : new[] { poste.DernierePiece },
            _ => Array.Empty<TacheAssemblageDto?>()
        };
        // Une tâche sans rien à faire à ce moment n'est pas affichée.
        return taches.Where(t => t != null && t.Etat != EtatsTache.NonRequis).Select(t => t!).ToList();
    }

    /// <summary>Tâches de fin restantes : du poste donné (fin de poste), de tous les postes sinon (clôture).</summary>
    private static List<string> Manquants(Contexte ctx, Dictionary<string, EvaluationPoste> evaluations, string? posteCode)
    {
        var phase = posteCode == null ? PhasesAssemblage.FinOf : PhasesAssemblage.FinPoste;
        return evaluations
            .Where(e => posteCode == null || e.Key == posteCode)
            .SelectMany(e => TachesDePhase(phase, ctx, e.Value)
                .Where(EstBloquanteRestante)
                .Select(t => $"{e.Key} : {t.Libelle}"))
            .Distinct()
            .ToList();
    }

    private static TacheAssemblageDto TacheDemarrerPoste(string posteCode) => new()
    {
        Code = TachesAssemblage.DemarrerPoste,
        Libelle = $"Démarrer le poste {posteCode}",
        Bloquante = true,
        Etat = EtatsTache.AFaire,
        Detail = "Le poste n'est pas encore démarré par votre équipe : ses contrôles de démarrage commencent au démarrage."
    };

    private static List<EtapeAssemblageDto> Etapes(string phase, TacheAssemblageDto echantillonnage)
    {
        var rangCourant = phase switch
        {
            PhasesAssemblage.NonDemarre or PhasesAssemblage.Echantillonnage => 0,
            PhasesAssemblage.Demarrage => 1,
            PhasesAssemblage.Production or PhasesAssemblage.Reprise or PhasesAssemblage.Pause => 2,
            PhasesAssemblage.FinPoste or PhasesAssemblage.FinOf => 3,
            PhasesAssemblage.Cloture or PhasesAssemblage.PosteTermine => 4,
            PhasesAssemblage.PosteOccupe => -1,
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

    /// <param name="ctx">Données de l'OF.</param>
    /// <param name="posteCode">Poste évalué.</param>
    /// <param name="finVerifiee">Fin à vérifier avant qu'elle soit demandée (fin directe) : FIN_POSTE ou FIN_OF.</param>
    private async Task<EvaluationPoste> EvaluerPosteAsync(Contexte ctx, string posteCode, string? finVerifiee = null)
    {
        var machines = await MachinesDuPosteAsync(ctx, posteCode);
        var equipe = ctx.EquipePour(posteCode);
        if (finVerifiee != null && equipe.SessionId != Guid.Empty && equipe.DemandeCloture != DemandesCloture.FinOf
            && (finVerifiee == DemandesCloture.FinOf || equipe.PosteDemarre))
            equipe = equipe with { DemandeCloture = finVerifiee };
        var (premiere, derniere, production, aLaDemande, reglageEnCours) = await TachesPlanAsync(ctx, posteCode, equipe);

        return new EvaluationPoste(
            PosteCode: posteCode,
            Equipe: equipe,
            Echantillonnage: TacheEchantillonnage(ctx, posteCode),
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
            Tracabilite: TacheTracabilite(ctx, posteCode),
            ControlesALaDemande: aLaDemande,
            ReglageEnCours: reglageEnCours);
    }

    /// <summary>Fiche d'échantillonnage du poste : une par poste de l'OF, retrouvée faite par les équipes suivantes.</summary>
    private static TacheAssemblageDto TacheEchantillonnage(Contexte ctx, string posteCode)
    {
        var tache = new TacheAssemblageDto
        {
            Code = TachesAssemblage.Echantillonnage,
            Libelle = $"Fiche d'échantillonnage du poste {posteCode}",
            Bloquante = true
        };

        if (ctx.Statuts.Any(s => s.TypeDocument == TypesDocumentExecution.Echantillonnage && s.PosteCode == posteCode))
        {
            tache.Etat = EtatsTache.Fait;
            tache.Detail = "Fiche remplie.";
        }
        else if (!ctx.PlanEchantillonnageActif)
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
    private async Task<(TacheAssemblageDto Premiere, TacheAssemblageDto Derniere, TacheAssemblageDto Production, List<TacheAssemblageDto> ALaDemande, bool ReglageEnCours)> TachesPlanAsync(Contexte ctx, string posteCode, ContexteEquipe equipe)
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
        var sections = ExecPlanAssemblageService.SectionsLot(plan, equipe, reponses);

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
        return (premiere, derniere, production, ControlesALaDemande(plan, equipe, reponses, reglageEnCours), reglageEnCours);
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
        var intervalle = CalendrierProduction.IntervalleMinutes(await _plans.GetEffectifParPosteAsync(ctx.ExecOf.Id, posteCode));
        var prochain = CalendrierProduction.ProchainControle(periodes.FirstOrDefault(p => p.EndedAt == null));

        tache.Compteur = aFaire;
        tache.Detail = string.Join(" ", new[]
        {
            aFaire > 0 ? $"{aFaire} contrôle(s) à faire maintenant." : null,
            reglageEnCours
                ? "Réglage en cours : contrôles suspendus jusqu'à un réglage conforme sur tous les critères."
                : intervalle == null
                    ? "Pas d'effectif par poste dans la fiche d'échantillonnage du poste : aucun contrôle planifié."
                    : prochain.HasValue
                        ? $"{(aFaire > 0 ? "Suivant" : "Aucun contrôle dû : prochain contrôle")} à {prochain.Value:HH:mm} (une pièce toutes les {intervalle.Value:0.#} min)."
                        : aFaire > 0 ? null : "Les contrôles démarrent avec la production du poste."
        }.Where(t => t != null));
        return (tache, reglageEnCours);
    }

    /// <summary>Contrôles aux réglages et de toutes les pièces : l'opérateur choisit quand les faire (jamais bloquants).</summary>
    private static List<TacheAssemblageDto> ControlesALaDemande(DocumentEntete plan, ContexteEquipe equipe, IReadOnlyCollection<ReponseLot> reponses,
        bool reglageEnCours)
    {
        var sections = ExecPlanAssemblageService.SectionsALaDemande(plan, equipe, reponses);
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

    /// <summary>Registre de traçabilité : commun à l'OF, fait pour l'équipe sur le poste quand elle y a confirmé ses lots.</summary>
    private static TacheAssemblageDto TacheTracabilite(Contexte ctx, string posteCode)
    {
        var fait = ctx.Session != null && ctx.Statuts.Any(s =>
            s.TypeDocument == TypesDocumentExecution.RegistreTracabilite && s.SessionId == ctx.Session.Id && s.PosteCode == posteCode && s.EstTermine);
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
