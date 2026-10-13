using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution;

public class OperateurRepository : IOperateurRepository
{
    /// <summary>
    /// OF proposé aux opérateurs : composants servis par le magasin (préparation terminée) et production pas encore finie
    /// (au moins une opération de la gamme sans exécution clôturée, ou une exécution encore ouverte).
    /// </summary>
    private static readonly Expression<Func<MagPreparationOf, bool>> OfDisponiblePourLaProduction = m =>
        m.Statut == StatutsPreparationMagasin.Termine &&
        !(m.NumeroOfNavigation.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations.Any() &&
          m.NumeroOfNavigation.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations.All(op =>
              m.NumeroOfNavigation.ExecControleOfs.Any(e => e.OperationCode == op.OperationCode && e.Statut == StatutsExecution.Cloture) &&
              !m.NumeroOfNavigation.ExecControleOfs.Any(e => e.OperationCode == op.OperationCode && e.Statut != StatutsExecution.Cloture)));

    /// <summary>Un OF clôturé reste au tableau de bord de l'opérateur ce nombre de jours (consultation de ses documents).</summary>
    public const int JoursOfsCloturesVisibles = 7;

    /// <summary>
    /// OF du tableau de bord : préparés par le magasin, disponibles pour la production ou clôturés depuis moins de
    /// <see cref="JoursOfsCloturesVisibles"/> jours (en consultation seule).
    /// </summary>
    private static Expression<Func<MagPreparationOf, bool>> OfAuTableauDeBord(DateTime clotureDepuis) => m =>
        m.Statut == StatutsPreparationMagasin.Termine &&
        (!(m.NumeroOfNavigation.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations.Any() &&
           m.NumeroOfNavigation.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations.All(op =>
               m.NumeroOfNavigation.ExecControleOfs.Any(e => e.OperationCode == op.OperationCode && e.Statut == StatutsExecution.Cloture) &&
               !m.NumeroOfNavigation.ExecControleOfs.Any(e => e.OperationCode == op.OperationCode && e.Statut != StatutsExecution.Cloture)))
         || m.NumeroOfNavigation.ExecControleOfs.Any(e => e.Statut == StatutsExecution.Cloture && e.DateFin >= clotureDepuis));

    private static DateTime DebutOfsCloturesVisibles => DateTime.Now.AddDays(-JoursOfsCloturesVisibles);

    private static readonly string[] StatutsPlanVerifMachineActif = { "ACTIF", "Actif" };

    private readonly SopalTraceDbContext _context;

    public OperateurRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    // ───────────────────────────── OF & exécution ─────────────────────────────

    public async Task<Operation?> GetOperationAsync(string operationCode)
        => await _context.Operations.AsNoTracking().FirstOrDefaultAsync(o => o.Code == operationCode);

    public async Task<MfgheadOrdreFabrication?> GetOfAsync(string numeroOf)
    {
        return await _context.MfgheadOrdreFabrications
            .Include(o => o.CodeArticleNavigation)
            .FirstOrDefaultAsync(o => o.NumeroOf == numeroOf);
    }

    public async Task<DocumentEntete?> GetPlanActifAsync(string codeArticle, string? operationCode = null)
    {
        var query = _context.DocumentEntetes
            .AsSplitQuery()
            .Include(p => p.DocumentSections)
                .ThenInclude(s => s.DocumentLignes)
            .Include(p => p.DocumentSections)
                .ThenInclude(s => s.TypeSection)
            .Where(p => p.TypeDocumentCode == TypesDocument.PlanFabrication &&
                        p.Statut == StatutsPlan.Actif &&
                        (p.CodeArticleSageVersionne == codeArticle ||
                         p.CodeArticleSageVersionne!.StartsWith(codeArticle + ".")));

        if (!string.IsNullOrEmpty(operationCode))
        {
            query = query.Where(p => p.OperationCode == operationCode);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<(decimal? Longueur, decimal? Diametre)> GetDernieresCotesTronconnageAsync(string codeArticle, string? operationCode)
    {
        var derniere = await _context.ExecControleOfs
            .Where(e => e.NumeroOfNavigation.CodeArticle == codeArticle
                        && (operationCode == null || e.OperationCode == operationCode)
                        && (e.LongueurNominale != null || e.DiametreNominal != null))
            .OrderByDescending(e => e.DateDebut)
            .Select(e => new { e.LongueurNominale, e.DiametreNominal })
            .FirstOrDefaultAsync();
        return (derniere?.LongueurNominale, derniere?.DiametreNominal);
    }

    public async Task<ExecControleOf?> GetExecOfByIdAsync(Guid execOfId)
    {
        return await _context.ExecControleOfs.FindAsync(execOfId);
    }

    public async Task<ExecControleOf?> GetExecOfWithIntermediairesAsync(Guid execOfId)
    {
        return await _context.ExecControleOfs
            .Include(x => x.ExecPrelevementIntermediaires)
            .FirstOrDefaultAsync(x => x.Id == execOfId);
    }

    public async Task<IEnumerable<string>> GetPostesForExecutionAsync(Guid execOfId)
    {
        return await _context.ExecControleOfPostes
            .Where(x => x.ExecControleOfId == execOfId)
            .Select(x => x.PosteCode)
            .ToListAsync();
    }

    public void AddExecControleOf(ExecControleOf execOf)
    {
        _context.ExecControleOfs.Add(execOf);
    }

    public async Task<bool> HasReglageSectionsAsync(Guid planSourceId)
    {
        return await _context.DocumentSections
            .AnyAsync(s => s.EnteteId == planSourceId && s.TypeSection != null &&
                           (s.TypeSection.TypeNormalise == SectionTypes.Reglage || s.TypeSection.TypeNormalise == SectionTypes.ReglageProd));
    }

    public async Task<bool> CanStartOperationAsync(string numeroOf, string operationCode)
    {
        var operationsGamme = await _context.MfgheadOrdreFabrications
            .Where(o => o.NumeroOf == numeroOf)
            .SelectMany(o => o.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations)
            .OrderBy(o => o.OrdreGamme)
            .Select(o => o.OperationCode)
            .ToListAsync();

        var index = operationsGamme.IndexOf(operationCode);
        if (index <= 0) return true; // Première opération de la gamme ou opération hors gamme

        var previousOp = operationsGamme[index - 1];
        return await _context.ExecControleOfs.AnyAsync(e => e.NumeroOf == numeroOf && e.OperationCode == previousOp);
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Index uniques (exécution active par OF, poste par OF...) : action déjà faite depuis un autre poste.
            throw ex.ToDomainExceptionOrSelf("Cette action vient d'être enregistrée depuis un autre poste. Rafraîchissez l'écran.");
        }
    }

    // ───────────────────────────── Catalogue ─────────────────────────────

    public async Task<IEnumerable<PosteTravail>> GetPostesDisponiblesAsync()
    {
        return await _context.PosteTravails
            .AsNoTracking()
            .Where(p => p.Actif)
            .ToListAsync();
    }

    public async Task<PosteTravail?> GetPosteWithMachinesAsync(string posteCode)
    {
        return await _context.PosteTravails
            .AsNoTracking()
            .Include(p => p.CodeMachines)
            .FirstOrDefaultAsync(p => p.CodePoste == posteCode);
    }

    public async Task<IReadOnlySet<string>> GetCodesPostesActifsAsync(IEnumerable<string> posteCodes)
    {
        var codes = posteCodes.Distinct().ToList();
        var existants = await _context.PosteTravails
            .Where(p => p.Actif && codes.Contains(p.CodePoste))
            .Select(p => p.CodePoste)
            .ToListAsync();
        return existants.ToHashSet();
    }

    public async Task<IEnumerable<OperateurOfDto>> GetAllOfOperationsDisponiblesAsync()
    {
        // On récupère uniquement les OFs qui ont été préparés par le magasin (MagPreparationOf)
        var ofs = await _context.MagPreparationOfs
            .Include(m => m.NumeroOfNavigation)
                .ThenInclude(of => of.CodeArticleNavigation)
                    .ThenInclude(a => a.NatureArticleCodeNavigation)
                        .ThenInclude(na => na.NatureArticleOperations)
                            .ThenInclude(nao => nao.OperationCodeNavigation)
            .Where(OfAuTableauDeBord(DebutOfsCloturesVisibles))
            .ToListAsync();

        var result = new List<OperateurOfDto>();

        foreach (var magOf in ofs)
        {
            var of = magOf.NumeroOfNavigation;
            var article = of.CodeArticleNavigation;

            var activeExecs = await _context.ExecControleOfs
                .Where(e => e.NumeroOf == of.NumeroOf)
                .ToListAsync();

            var dto = new OperateurOfDto
            {
                NumeroOf = of.NumeroOf,
                StatutOf = of.StatutOf,
                CodeArticle = of.CodeArticle,
                DesignationArticle = article?.Designation ?? "",
                QuantitePrevue = of.QuantitePrevue,
                QuantiteLancee = of.QuantiteLancee,
                DateDebut = of.DateDebut
            };

            if (article?.NatureArticleCodeNavigation?.NatureArticleOperations != null)
            {
                foreach (var op in article.NatureArticleCodeNavigation.NatureArticleOperations.OrderBy(o => o.OrdreGamme))
                {
                    var machine = await _context.Machines.FirstOrDefaultAsync(m => m.OperationCode == op.OperationCode);
                    var execForOp = activeExecs.OrderByDescending(e => e.DateDebut).FirstOrDefault(e => e.OperationCode == op.OperationCode);

                    bool aDesControlesReglage = false;
                    string? legendeMoyens = null;
                    string? remarques = null;

                    if (execForOp != null && execForOp.PlanSourceId.HasValue)
                    {
                        aDesControlesReglage = await HasReglageSectionsAsync(execForOp.PlanSourceId.Value);
                        var plan = await _context.DocumentEntetes.FirstOrDefaultAsync(p => p.Id == execForOp.PlanSourceId.Value);
                        if (plan != null)
                        {
                            legendeMoyens = plan.LegendeMoyens;
                            remarques = plan.Remarques;
                        }
                    }

                    dto.GammeOperatoire.Add(new OperateurOperationDto
                    {
                        OperationCode = op.OperationCode,
                        Libelle = op.OperationCodeNavigation?.Libelle ?? op.OperationCode,
                        MachinePrevueCode = machine?.CodeMachine ?? "",
                        AvecMachine = op.OperationCodeNavigation?.AvecMachine ?? false,
                        AvecCotesTronconnage = op.OperationCodeNavigation?.AvecCotesTronconnage ?? false,
                        ActiveExecControleOfId = execForOp?.Id,
                        ActiveExecStatut = execForOp?.Statut,
                        ActiveMachineCode = execForOp?.MachineCode,
                        EstEnReglage = execForOp == null ? null : execForOp.Statut == StatutsExecution.Reglage,
                        A_Des_Controles_Reglage = execForOp != null ? aDesControlesReglage : (bool?)null,
                        LegendeMoyens = legendeMoyens,
                        Remarques = remarques
                    });
                }
            }

            // Toutes les opérations de la gamme clôturées : l'OF est en consultation seule.
            if (dto.GammeOperatoire.Count > 0 && dto.GammeOperatoire.All(o => o.ActiveExecStatut == StatutsExecution.Cloture))
                dto.DateCloture = activeExecs.Where(e => e.Statut == StatutsExecution.Cloture).Max(e => e.DateFin);

            result.Add(dto);
        }

        // OF à traiter d'abord, puis les clôturés du plus récent au plus ancien.
        return result.OrderBy(o => o.DateCloture.HasValue).ThenByDescending(o => o.DateCloture).ToList();
    }

    public async Task<IEnumerable<Machine>> GetAllMachinesAsync()
    {
        return await _context.Machines.AsNoTracking().Where(m => m.Actif).ToListAsync();
    }

    public async Task<Dictionary<string, string>> GetMachineLabelsAsync(IEnumerable<string> machineCodes)
    {
        var codes = machineCodes.Distinct().ToList();
        if (codes.Count == 0) return new Dictionary<string, string>();

        return await _context.Machines
            .Where(m => codes.Contains(m.CodeMachine))
            .ToDictionaryAsync(m => m.CodeMachine, m => m.Libelle);
    }

    public async Task<Dictionary<string, List<string>>> GetMachinesPlanPeriodicitesAsync()
    {
        var lignes = await GetPeriodicitesPlansActifsAsync(machineCode: null);

        return lignes
            .GroupBy(l => l.MachineCode)
            .ToDictionary(g => g.Key, g => g.Select(l => l.Libelle).Distinct().ToList());
    }

    public async Task<List<string>> GetPeriodicitesPlanMachineAsync(string machineCode)
    {
        var lignes = await GetPeriodicitesPlansActifsAsync(machineCode);
        return lignes.Select(l => l.Libelle).Distinct().ToList();
    }

    /// <summary>Couples (machine, libellé de périodicité en minuscules) des plans de vérification actifs.</summary>
    private async Task<List<(string MachineCode, string Libelle)>> GetPeriodicitesPlansActifsAsync(string? machineCode)
    {
        var plans = _context.DocumentEntetes
            .Where(e => e.TypeDocumentCode == TypesDocument.VerifMachine &&
                        StatutsPlanVerifMachineActif.Contains(e.Statut) && e.MachineCode != null);

        if (machineCode != null)
        {
            plans = plans.Where(e => e.MachineCode == machineCode);
        }

        var lignes = await plans
            .SelectMany(e => e.DocumentLignes
                .SelectMany(l => l.DocumentVerifMachineEcheances)
                .Where(g => g.PeriodiciteMachine.ControlMoment != null)
                .Select(g => new { MachineCode = e.MachineCode!, Moment = g.PeriodiciteMachine.ControlMoment! }))
            .Distinct()
            .ToListAsync();

        return lignes.Select(l => (l.MachineCode, l.Moment)).ToList();
    }

    // ───────────────────────────── Assemblage (OF produits finis) ─────────────────────────────

    private IQueryable<MagPreparationOf> QueryOfsAssemblageDisponibles()
    {
        return _context.MagPreparationOfs
            .Where(OfDisponiblePourLaProduction)
            .Where(m => m.NumeroOfNavigation.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations
                            .Any(o => o.OperationCode == CodesOperation.Assemblage));
    }

    public async Task<List<OfAssemblageStatutDto>> GetOfsAssemblageStatutAsync()
    {
        var ofs = await _context.MagPreparationOfs
            .Where(OfAuTableauDeBord(DebutOfsCloturesVisibles))
            .Where(m => m.NumeroOfNavigation.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations
                            .Any(o => o.OperationCode == CodesOperation.Assemblage))
            .AsNoTracking()
            .Select(m => new
            {
                m.NumeroOfNavigation.NumeroOf,
                m.NumeroOfNavigation.CodeArticle,
                m.NumeroOfNavigation.CodeArticleNavigation.Designation,
                m.NumeroOfNavigation.QuantiteLancee,
                m.NumeroOfNavigation.DateDebut
            })
            .Distinct()
            .ToListAsync();

        var numeros = ofs.Select(o => o.NumeroOf).ToList();

        var execs = await _context.ExecControleOfs
            .AsNoTracking()
            .Where(e => e.TypeOf == TypesOf.Assemblage && numeros.Contains(e.NumeroOf))
            .Select(e => new
            {
                e.Id,
                e.NumeroOf,
                e.Statut,
                e.DateDebut,
                e.DateFin,
                Postes = e.ExecControleOfPostes.Select(p => p.PosteCode).ToList(),
                // Équipes ouvertes et postes qu'elles tiennent.
                Equipes = e.ExecControleOfSessions
                    .Where(s => s.Statut == StatutsSession.EnCours || s.Statut == StatutsSession.EnPause)
                    .OrderBy(s => s.DateDebut)
                    .Select(s => new EquipeOfAssemblageDto
                    {
                        Equipe = s.Equipe,
                        EnPause = s.Statut == StatutsSession.EnPause,
                        ClotureDemandee = s.ClosingRequest == DemandesCloture.FinOf,
                        Postes = s.ExecSessionPosts.Where(p => p.EndedAt == null).OrderBy(p => p.PosteCode).Select(p => p.PosteCode).ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

        // Exécution affichée : celle en cours, sinon la dernière clôturée (consultation).
        var execParOf = execs
            .GroupBy(e => e.NumeroOf)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Statut == StatutsExecution.Cloture).ThenByDescending(e => e.DateDebut).First());

        return ofs.Select(of =>
        {
            execParOf.TryGetValue(of.NumeroOf, out var exec);
            return new OfAssemblageStatutDto
            {
                NumeroOf = of.NumeroOf,
                CodeArticle = of.CodeArticle,
                DesignationArticle = of.Designation,
                QuantiteLancee = of.QuantiteLancee,
                DateDebut = exec?.DateDebut ?? of.DateDebut,
                Statut = exec?.Statut,
                ExecControleOfId = exec?.Id,
                Equipes = exec?.Equipes ?? new List<EquipeOfAssemblageDto>(),
                DateCloture = exec?.Statut == StatutsExecution.Cloture ? exec.DateFin : null,
                PostesExistants = exec?.Postes ?? new List<string>()
            };
        })
        // OF à traiter d'abord, puis les clôturés du plus récent au plus ancien.
        .OrderBy(o => o.DateCloture.HasValue).ThenByDescending(o => o.DateCloture)
        .ToList();
    }

    public async Task<bool> OfAssemblageDisponibleAsync(string numeroOf)
    {
        return await QueryOfsAssemblageDisponibles().AnyAsync(m => m.NumeroOf == numeroOf);
    }

    public async Task<bool> ExisteExecutionAssemblageActiveAsync(string numeroOf)
    {
        return await _context.ExecControleOfs
            .AnyAsync(e => e.NumeroOf == numeroOf && e.TypeOf == TypesOf.Assemblage && e.Statut != StatutsExecution.Cloture);
    }

    public async Task<Guid?> GetPlanVerifMachineActifIdAsync(string machineCode)
    {
        return await _context.DocumentEntetes
            .Where(p => p.TypeDocumentCode == TypesDocument.VerifMachine &&
                        p.MachineCode == machineCode && StatutsPlanVerifMachineActif.Contains(p.Statut))
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<(Guid EcheanceId, string Periodicite)>> GetEcheancesPlanVerifMachineAsync(Guid planId)
    {
        var echeances = await _context.DocumentVerifMachineEcheances
            .AsNoTracking()
            .Where(g => g.Ligne.EnteteId == planId)
            .Select(g => new { g.Id, g.PeriodiciteMachine.ControlMoment })
            .ToListAsync();
        return echeances.Select(g => (g.Id, g.ControlMoment ?? string.Empty)).ToList();
    }

    public async Task<List<ExecControleDocumentStatut>> GetStatutsVerifMachineAsync(Guid execControleOfId)
    {
        return await _context.ExecControleDocumentStatuts
            .AsNoTracking()
            .Where(s => s.ExecControleOfId == execControleOfId && s.TypeDocument == TypesDocumentExecution.VerifMachine)
            .OrderByDescending(s => s.DateExecution)
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, string>> GetFormulaireDesignationsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, string>();

        var result = await _context.RefFormulaires
            .Where(f => idList.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Designation ?? f.CodeReference);

        var docEntetes = await _context.DocumentEntetes
            .Where(d => idList.Contains(d.Id))
            .Select(d => new { d.Id, Designation = !string.IsNullOrEmpty(d.Designation) ? d.Designation : d.Nom })
            .ToListAsync();

        foreach (var doc in docEntetes.Where(d => !string.IsNullOrEmpty(d.Designation)))
        {
            result.TryAdd(doc.Id, doc.Designation!);
        }
        return result;
    }

    public void AddExecControleOfPoste(ExecControleOfPoste poste)
    {
        _context.ExecControleOfPostes.Add(poste);
    }

    // ───────────────────────────── Sessions d'équipe ─────────────────────────────

    public void AddSession(ExecControleOfSession session)
    {
        _context.ExecControleOfSessions.Add(session);
    }

    // ───────────────────────────── Statuts de documents ─────────────────────────────

    public async Task<IEnumerable<ExecControleDocumentStatut>> GetDocumentStatutsAsync(Guid execControleOfId, Guid sessionId)
    {
        return await _context.ExecControleDocumentStatuts
            .Where(d => d.ExecControleOfId == execControleOfId && d.SessionId == sessionId)
            .ToListAsync();
    }

    public async Task<List<ExecControleDocumentStatut>> GetStatutsOfAsync(Guid execControleOfId)
    {
        return await _context.ExecControleDocumentStatuts
            .Where(d => d.ExecControleOfId == execControleOfId)
            .ToListAsync();
    }

    public async Task<ExecControleDocumentStatut?> GetDocumentStatutByIdAsync(Guid statutId)
    {
        return await _context.ExecControleDocumentStatuts
            .Include(d => d.ExecControleOf)
                .ThenInclude(e => e.NumeroOfNavigation)
                    .ThenInclude(o => o.CodeArticleNavigation)
            .FirstOrDefaultAsync(d => d.Id == statutId);
    }

    public async Task<List<ExecControleDocumentStatut>> GetDocumentStatutsForMachineAsync(Guid execControleOfId, string machineCode)
    {
        var query = _context.ExecControleDocumentStatuts
            .Where(s => s.ExecControleOfId == execControleOfId && s.MachineCode == machineCode && s.TypeDocument == TypesDocumentExecution.VerifMachine);

        // Vérifications de l'équipe qui tient le poste de la machine, s'il est tenu ; sinon tout l'historique.
        var tenues = query.Where(d => _context.ExecSessionPosts.Any(p =>
            p.SessionId == d.SessionId && p.PosteCode == d.PosteCode && p.EndedAt == null));
        if (await tenues.AnyAsync())
        {
            query = tenues;
        }

        return await query
            .OrderByDescending(s => s.DateExecution)
            .ThenBy(s => s.Equipe)
            .ToListAsync();
    }

    public void AddExecControleDocumentStatut(ExecControleDocumentStatut statut)
    {
        _context.ExecControleDocumentStatuts.Add(statut);
    }

    public void UpdateExecControleDocumentStatut(ExecControleDocumentStatut statut)
    {
        _context.ExecControleDocumentStatuts.Update(statut);
    }

    // ───────────────────────────── Vérification machine ─────────────────────────────

    public void AddExecVerifMachineReponses(IEnumerable<ExecVerifMachineReponse> reponses)
    {
        _context.ExecVerifMachineReponses.AddRange(reponses);
    }

    public async Task<List<ExecVerifMachineReponse>> GetExecVerifMachineReponsesAsync(Guid statutId)
    {
        return await _context.ExecVerifMachineReponses
            .AsNoTracking()
            .Where(r => r.ExecControleDocumentStatutId == statutId)
            .ToListAsync();
    }
}
