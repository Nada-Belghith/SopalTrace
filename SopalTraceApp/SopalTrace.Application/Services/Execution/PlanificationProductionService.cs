using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services.Execution;

/// <summary>
/// Contrôles périodiques planifiés par le serveur (règles : <see cref="CalendrierProduction"/>), que l'écran soit
/// ouvert ou non :
/// <list type="bullet">
/// <item>assemblage : chaque équipe a, sur chaque poste qu'elle tient, ses périodes de production (plusieurs
/// équipes peuvent produire en même temps sur des postes différents) ; une période s'ouvre quand le poste est à
/// l'étape « Production » de l'assistant (même calcul que l'avancement) et se ferme à la pause de l'équipe, au
/// réglage ou à la fin de ce poste ;</item>
/// <item>fabrication : chaque section de périodicité horaire a ses périodes ; elles suivent l'état de l'OF
/// (en cours : ouverte ; réglage, pause, clôture : fermée).</item>
/// </list>
/// </summary>
public class PlanificationProductionService : IPlanificationProductionService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> Verrous = new();

    /// <summary>Production reprise plus tard que ce délai après la fin du réglage : le calcul repart de maintenant.</summary>
    private static readonly TimeSpan RepriseMaximale = TimeSpan.FromMinutes(2);

    private readonly IPlanificationProductionRepository _repository;
    private readonly IExecPlanAssemblageRepository _plans;
    private readonly IAvancementAssemblageService _avancement;
    private readonly IOccurrenceService _occurrences;
    private readonly IDemarragePosteRepository _postes;

    public PlanificationProductionService(IPlanificationProductionRepository repository, IExecPlanAssemblageRepository plans,
        IAvancementAssemblageService avancement, IOccurrenceService occurrences, IDemarragePosteRepository postes)
    {
        _repository = repository;
        _plans = plans;
        _avancement = avancement;
        _occurrences = occurrences;
        _postes = postes;
    }

    /// <summary>Plan d'une équipe sur un poste : sections planifiées, sections de réglage et saisies de réglage.</summary>
    private sealed record PostePlan(string Code, ExecControleOfSession Session, List<Guid> SectionsProduction,
        List<Guid> SectionsReglage, List<SaisieReglage> SaisiesReglage, double? Intervalle);

    /// <summary>Données d'une synchronisation : périodes de l'OF et plans déjà lus.</summary>
    private sealed class Etat
    {
        public required ExecControleOf ExecOf { get; init; }
        public required List<ExecControleOfSession> Sessions { get; init; }
        /// <summary>Postes tenus par les sessions (assemblage).</summary>
        public required List<DemarragePoste> Postes { get; init; }
        public required List<ExecProductionPeriod> Periodes { get; init; }
        /// <summary>Fabrication : tranches soldées (aucun nouveau contrôle n'y est ajouté).</summary>
        public HashSet<string> TranchesSoldees { get; init; } = new();
        public DateTime Maintenant { get; } = DateTime.Now;
        public Dictionary<(Guid, string), PostePlan> Plans { get; } = new();

        public List<ExecProductionPeriod> PeriodesPoste(Guid sessionId, string posteCode) => Periodes
            .Where(p => p.SessionId == sessionId && p.PosteCode == posteCode)
            .OrderBy(p => p.StartedAt).ThenBy(p => p.EndedAt ?? DateTime.MaxValue)
            .ToList();

        /// <summary>Périodes de la même suite de contrôles (équipe + poste, ou section) : numérotation et retards.</summary>
        public List<ExecProductionPeriod> PeriodesDeLaSuite(ExecProductionPeriod periode) => Periodes
            .Where(p => p.SessionId == periode.SessionId && p.PosteCode == periode.PosteCode && p.SectionId == periode.SectionId)
            .OrderBy(p => p.StartedAt).ThenBy(p => p.EndedAt ?? DateTime.MaxValue)
            .ToList();
    }

    public Task<List<Guid>> GetOfsAPlanifierAsync() => _repository.GetOfsAPlanifierAsync();

    public async Task SynchroniserAsync(Guid execControleOfId)
    {
        await SousVerrouAsync(execControleOfId, async () =>
        {
            var etat = await ChargerAsync(execControleOfId);
            if (etat == null) return;
            await SynchroniserAsync(etat);
        });
    }

    public async Task DemarrerReglageAsync(Guid execControleOfId, string posteCode)
    {
        await SousVerrouAsync(execControleOfId, async () =>
        {
            var etat = await ChargerAssemblageAsync(execControleOfId);
            var session = SessionEnProduction(etat, posteCode);
            var poste = await PlanAsync(etat, session, posteCode);
            if (poste.SectionsReglage.Count == 0)
                throw new RegleMetierException($"Le plan d'assemblage du poste {posteCode} n'a pas de contrôle aux réglages.");

            var periodes = etat.PeriodesPoste(session.Id, posteCode);
            var ouverte = periodes.FirstOrDefault(p => p.EndedAt == null);
            if (ouverte != null)
                Clore(etat, ouverte, etat.Maintenant, MotifsFinPeriode.Reglage, poste.SectionsProduction);
            else if (!CalendrierProduction.ReglageEnCours(periodes, poste.SaisiesReglage, poste.SectionsReglage))
                // Réglage avant toute production : une période vide en marque le début.
                Ajouter(etat, new ExecProductionPeriod
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfId = etat.ExecOf.Id,
                    SessionId = session.Id,
                    PosteCode = posteCode,
                    StartedAt = etat.Maintenant,
                    EndedAt = etat.Maintenant,
                    EndReason = MotifsFinPeriode.Reglage
                });
            await _repository.SaveChangesAsync();
        });
    }

    public async Task AnnulerReglageAsync(Guid execControleOfId, string posteCode)
    {
        await SousVerrouAsync(execControleOfId, async () =>
        {
            var etat = await ChargerAssemblageAsync(execControleOfId);
            var session = SessionEnProduction(etat, posteCode);
            var poste = await PlanAsync(etat, session, posteCode);
            var periodes = etat.PeriodesPoste(session.Id, posteCode);
            if (!CalendrierProduction.ReglageEnCours(periodes, poste.SaisiesReglage, poste.SectionsReglage)) return;

            var reglage = periodes.Last();
            if (poste.SaisiesReglage.Any(s => s.DateSaisie >= reglage.EndedAt))
                throw new RegleMetierException("Un contrôle de réglage a déjà été saisi : terminez le réglage par un contrôle conforme sur tous les critères.");

            reglage.EndReason = MotifsFinPeriode.ReglageAnnule;
            await SynchroniserAsync(etat);
        });
    }

    // ───────────────────────────── Synchronisation ─────────────────────────────

    private async Task SynchroniserAsync(Etat etat)
    {
        if (etat.ExecOf.TypeOf == TypesOf.Fabrication)
            await SynchroniserFabricationAsync(etat);
        else
            await SynchroniserAssemblageAsync(etat);
        await _repository.SaveChangesAsync();

        // Retards (le temps de pause ou de réglage ne compte pas), puis escalade vers le superviseur / le manager.
        foreach (var occurrence in await _repository.GetOccurrencesNonReponduesAsync(etat.ExecOf.Id))
        {
            var periode = etat.Periodes.FirstOrDefault(p => p.Id == occurrence.ProductionPeriodId);
            if (!occurrence.EstEnRetard && periode != null &&
                CalendrierProduction.EstEnRetard(occurrence.HeureNotifPrevue, etat.PeriodesDeLaSuite(periode), etat.Maintenant))
                occurrence.EstEnRetard = true;
        }
        await _repository.SaveChangesAsync();

        if (etat.ExecOf.Statut == StatutsExecution.EnCours)
            await _occurrences.VerifierEscaladeAsync(etat.ExecOf.Id);
    }

    private async Task SynchroniserAssemblageAsync(Etat etat)
    {
        // 1. Postes qui ne produisent plus : la période se ferme à l'heure de la fin du poste, de la pause de
        //    l'équipe ou de la fin de sa session.
        var cloture = etat.ExecOf.Statut == StatutsExecution.Cloture;
        foreach (var periode in etat.Periodes.Where(p => p.EndedAt == null && p.SessionId != null).ToList())
        {
            var session = etat.Sessions.First(s => s.Id == periode.SessionId);
            var poste = etat.Postes.FirstOrDefault(d => d.SessionId == session.Id && d.PosteCode == periode.PosteCode);
            if (session.Statut == StatutsSession.EnCours && !cloture && poste is { EnCours: true }) continue;
            var (fin, motif) = poste?.EndedAt is DateTime finPoste ? (finPoste, MotifsFinPeriode.FinPoste)
                : session.Statut == StatutsSession.EnPause ? (session.SuspendedAt ?? etat.Maintenant, MotifsFinPeriode.Pause)
                : (session.DateFin ?? etat.Maintenant, MotifsFinPeriode.FinPoste);
            Clore(etat, periode, fin, motif, (await PlanAsync(etat, session, periode.PosteCode!)).SectionsProduction);
        }
        if (cloture) return;

        // 2. Équipes en production : chaque poste qu'elles tiennent suit l'étape calculée par l'avancement.
        foreach (var session in etat.Sessions.Where(s => s.Statut == StatutsSession.EnCours))
        {
            var tenus = etat.Postes.Where(d => d.SessionId == session.Id && d.EnCours).Select(d => d.PosteCode).ToHashSet();
            if (tenus.Count == 0) continue;
            var phases = await _avancement.GetPhasesPostesAsync(etat.ExecOf.Id, session.Equipe);
            foreach (var (posteCode, phase) in phases.Where(p => tenus.Contains(p.Key)))
                SynchroniserPoste(etat, await PlanAsync(etat, session, posteCode), phase == PhasesAssemblage.Production);
        }
    }

    /// <summary>
    /// Fabrication : une suite de contrôles par section de périodicité horaire. En cours : période ouverte ;
    /// réglage, pause ou clôture : période fermée à l'heure du changement d'état de l'OF.
    /// </summary>
    private async Task SynchroniserFabricationAsync(Etat etat)
    {
        var plan = etat.ExecOf.PlanSourceId is Guid planId ? await _plans.GetDocumentByIdAsync(planId) : null;
        var intervalles = (plan?.DocumentSections ?? new List<DocumentSection>())
            .Where(CalendrierProduction.EstPlanifiable)
            .Select(s => (s.Id, Intervalle: CalendrierProduction.IntervalleSection(s.Periodicite)))
            .Where(x => x.Intervalle.HasValue)
            .ToDictionary(x => x.Id, x => x.Intervalle!.Value);

        var of = etat.ExecOf;
        var enCours = of.Statut == StatutsExecution.EnCours;
        foreach (var periode in etat.Periodes.Where(p => p.EndedAt == null && p.SectionId != null).ToList())
        {
            var sections = new List<Guid> { periode.SectionId!.Value };
            if (enCours && intervalles.ContainsKey(periode.SectionId.Value))
            {
                Planifier(etat, periode, etat.Maintenant, sections);
                continue;
            }
            var (fin, motif) = of.Statut switch
            {
                StatutsExecution.Reglage => (of.SuspendedAt ?? etat.Maintenant, MotifsFinPeriode.Reglage),
                StatutsExecution.EnPause => (of.SuspendedAt ?? etat.Maintenant, MotifsFinPeriode.Pause),
                StatutsExecution.Cloture => (of.DateFin ?? etat.Maintenant, MotifsFinPeriode.Arret),
                _ => (etat.Maintenant, MotifsFinPeriode.Arret)
            };
            Clore(etat, periode, fin, motif, sections);
        }

        if (!enCours) return;
        foreach (var (sectionId, intervalle) in intervalles)
        {
            if (etat.Periodes.Any(p => p.SectionId == sectionId && p.EndedAt == null)) continue;
            Ajouter(etat, new ExecProductionPeriod
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = of.Id,
                SectionId = sectionId,
                StartedAt = etat.Maintenant,
                IntervalMinutes = intervalle
            });
        }
    }

    /// <summary>
    /// Période du poste selon l'étape : ouverte en production (hors réglage en cours), fermée sinon. Une saisie
    /// de réglage ferme la période ouverte ; un réglage conforme sur tous les critères en ouvre une nouvelle.
    /// </summary>
    private void SynchroniserPoste(Etat etat, PostePlan poste, bool enProduction)
    {
        var planifiable = enProduction && poste.Intervalle.HasValue && poste.SectionsProduction.Count > 0;

        // Plusieurs saisies de réglage peuvent avoir eu lieu depuis la dernière synchronisation.
        for (var tour = 0; tour < 20; tour++)
        {
            var periodes = etat.PeriodesPoste(poste.Session.Id, poste.Code);
            var ouverte = periodes.FirstOrDefault(p => p.EndedAt == null);
            if (ouverte != null)
            {
                var reglage = poste.SaisiesReglage
                    .Where(s => s.DateSaisie > ouverte.StartedAt && s.DateSaisie <= etat.Maintenant)
                    .Select(s => (DateTime?)s.DateSaisie)
                    .Min();
                if (reglage.HasValue)
                {
                    Clore(etat, ouverte, reglage.Value, MotifsFinPeriode.Reglage, poste.SectionsProduction);
                    continue;
                }
                if (planifiable) Planifier(etat, ouverte, etat.Maintenant, poste.SectionsProduction);
                else Clore(etat, ouverte, etat.Maintenant, MotifsFinPeriode.Arret, poste.SectionsProduction);
                return;
            }

            if (!planifiable) return;
            var debut = etat.Maintenant;
            if (periodes.LastOrDefault() is { EndReason: MotifsFinPeriode.Reglage, EndedAt: DateTime debutReglage })
            {
                var finReglage = CalendrierProduction.FinReglage(debutReglage, poste.SaisiesReglage, poste.SectionsReglage);
                if (finReglage == null) return;
                if (etat.Maintenant - finReglage.Value <= RepriseMaximale) debut = finReglage.Value;
            }
            Ajouter(etat, new ExecProductionPeriod
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = etat.ExecOf.Id,
                SessionId = poste.Session.Id,
                PosteCode = poste.Code,
                StartedAt = debut,
                IntervalMinutes = poste.Intervalle
            });
        }
    }

    /// <summary>Ferme la période après y avoir créé les contrôles dus jusqu'à sa fin.</summary>
    private void Clore(Etat etat, ExecProductionPeriod periode, DateTime fin, string motif, IReadOnlyCollection<Guid> sections)
    {
        if (fin < periode.StartedAt) fin = periode.StartedAt;
        Planifier(etat, periode, fin, sections);
        periode.EndedAt = fin;
        periode.EndReason = motif;
    }

    /// <summary>
    /// Crée les contrôles dus jusqu'à <paramref name="limite"/> : une occurrence par section, numérotée dans sa
    /// suite (équipe + poste, ou section). Fabrication : rien n'est ajouté dans une tranche déjà soldée.
    /// </summary>
    private void Planifier(Etat etat, ExecProductionPeriod periode, DateTime limite, IReadOnlyCollection<Guid> sections)
    {
        var heures = CalendrierProduction.ControlesDus(periode, limite);
        if (heures.Count == 0) return;

        var numero = etat.PeriodesDeLaSuite(periode)
            .TakeWhile(p => p != periode)
            .Sum(p => p.PlannedControlCount) + periode.PlannedControlCount;
        var occurrences = new List<ExecPrelevementIntermediaire>();
        foreach (var heure in heures)
        {
            numero++;
            if (etat.TranchesSoldees.Contains(CalendrierProduction.Tranche(heure))) continue;
            occurrences.AddRange(sections.Select(sectionId => new ExecPrelevementIntermediaire
            {
                Id = Guid.NewGuid(),
                ExecControleOfid = etat.ExecOf.Id,
                SectionId = sectionId,
                TrancheHoraire = CalendrierProduction.Tranche(heure),
                NumeroOccurrence = numero,
                HeureNotifPrevue = heure,
                SessionId = periode.SessionId,
                PosteCode = periode.PosteCode,
                ProductionPeriodId = periode.Id,
                EstRepondu = false,
                EstEnRetard = false
            }));
        }
        _repository.AjouterOccurrences(occurrences);
        periode.PlannedControlCount += heures.Count;
    }

    private void Ajouter(Etat etat, ExecProductionPeriod periode)
    {
        _repository.AjouterPeriode(periode);
        etat.Periodes.Add(periode);
    }

    // ───────────────────────────── Lecture ─────────────────────────────

    private async Task<Etat?> ChargerAsync(Guid execControleOfId)
    {
        var execOf = await _plans.GetExecOfAsync(execControleOfId);
        if (execOf == null) return null;
        var assemblage = execOf.TypeOf == TypesOf.Assemblage;
        return new Etat
        {
            ExecOf = execOf,
            Sessions = await _repository.GetSessionsAsync(execControleOfId),
            Postes = assemblage ? await _postes.GetDemarragesAsync(execControleOfId) : new List<DemarragePoste>(),
            Periodes = await _repository.GetPeriodesAsync(execControleOfId),
            TranchesSoldees = assemblage ? new HashSet<string>() : await _repository.GetTranchesSoldeesAsync(execControleOfId)
        };
    }

    private async Task<Etat> ChargerAssemblageAsync(Guid execControleOfId)
    {
        var etat = await ChargerAsync(execControleOfId);
        return etat?.ExecOf.TypeOf == TypesOf.Assemblage ? etat : throw new RegleMetierException("Cet OF n'est pas un OF d'assemblage.");
    }

    private static ExecControleOfSession SessionEnProduction(Etat etat, string posteCode)
    {
        if (etat.ExecOf.Statut == StatutsExecution.Cloture)
            throw new EtatExecutionInvalideException("Cet OF est clôturé.");
        if (!etat.ExecOf.ExecControleOfPostes.Any(p => p.PosteCode == posteCode))
            throw new RegleMetierException($"Le poste {posteCode} n'est pas affecté à cet OF.");
        // Équipe qui tient le poste, en production.
        var titulaire = etat.Postes.FirstOrDefault(d => d.PosteCode == posteCode && d.EnCours);
        return etat.Sessions.FirstOrDefault(s => s.Id == titulaire?.SessionId && s.Statut == StatutsSession.EnCours)
            ?? throw new EtatExecutionInvalideException($"Aucune équipe ne produit sur le poste {posteCode} : reprenez la production avant un réglage.");
    }

    /// <summary>Plan de l'équipe sur le poste : celui qu'elle exécute, sinon celui qui s'applique à l'article.</summary>
    private async Task<PostePlan> PlanAsync(Etat etat, ExecControleOfSession session, string posteCode)
    {
        if (etat.Plans.TryGetValue((session.Id, posteCode), out var connu)) return connu;

        var statut = await _plans.GetStatutPlanAssAsync(etat.ExecOf.Id, posteCode, session.Id);
        var planId = statut?.DocId ?? await _plans.TrouverPlanAssemblageIdAsync(etat.ExecOf.NumeroOfNavigation.CodeArticle, posteCode);
        var sections = planId.HasValue
            ? (await _plans.GetDocumentByIdAsync(planId.Value))?.DocumentSections.ToList() ?? new List<DocumentSection>()
            : new List<DocumentSection>();

        var production = sections
            .Where(s => ExecPlanAssemblageService.TypeDe(s) == SectionTypes.Echantillonnage2859 && CalendrierProduction.EstPlanifiable(s))
            .Select(s => s.Id)
            .ToList();
        var reglage = sections.Where(s => ControlesLot.EstSectionReglage(ExecPlanAssemblageService.TypeDe(s))).ToList();
        var saisies = reglage.Count == 0
            ? new List<SaisieReglage>()
            : CalendrierProduction.SaisiesReglage(await _plans.GetReponsesPlanAsync(etat.ExecOf.Id, posteCode), session.Id,
                reglage.SelectMany(s => s.DocumentLignes.Select(l => (Ligne: l.Id, Section: s.Id))).ToDictionary(x => x.Ligne, x => x.Section));

        // Intervalle du poste : 60 / effectif par poste de sa fiche d'échantillonnage.
        var intervalle = CalendrierProduction.IntervalleMinutes(await _plans.GetEffectifParPosteAsync(etat.ExecOf.Id, posteCode));
        var plan = new PostePlan(posteCode, session, production, reglage.Select(s => s.Id).ToList(), saisies, intervalle);
        etat.Plans[(session.Id, posteCode)] = plan;
        return plan;
    }

    private static async Task SousVerrouAsync(Guid execControleOfId, Func<Task> action)
    {
        var verrou = Verrous.GetOrAdd(execControleOfId, _ => new SemaphoreSlim(1, 1));
        await verrou.WaitAsync();
        try { await action(); }
        finally { verrou.Release(); }
    }
}
