using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution;

public class OperateurRepository : IOperateurRepository
{
    private readonly SopalTraceDbContext _context;

    public OperateurRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<MfgheadOrdreFabrication?> GetOfAsync(string numeroOf)
    {
        return await _context.MfgheadOrdreFabrications
            .Include(o => o.CodeArticleNavigation)
            .FirstOrDefaultAsync(o => o.NumeroOf == numeroOf);
    }

    public async Task<MagPreparationOf?> GetMagPreparationOfAsync(string numeroOf)
    {
        return await _context.MagPreparationOfs
            .FirstOrDefaultAsync(p => p.NumeroOf == numeroOf);
    }

    public async Task<PlanFabricationEntete?> GetPlanActifAsync(string codeArticle, string? operationCode = null)
    {
        var query = _context.PlanFabricationEntetes
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.PlanFabricationLignes)
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.TypeSection)
            .Where(p => p.Statut == "ACTIF" && 
                        (p.CodeArticleSageVersionne == codeArticle || 
                         p.CodeArticleSageVersionne.StartsWith(codeArticle + ".")));

        if (!string.IsNullOrEmpty(operationCode))
        {
            query = query.Where(p => p.OperationCode == operationCode);
        }

        return await query.FirstOrDefaultAsync();
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

    public void AddExecControleOf(ExecControleOf execOf)
    {
        _context.ExecControleOfs.Add(execOf);
    }



    public async Task<bool> HasReglageSectionsAsync(Guid planSourceId)
    {
        return await _context.PlanFabricationSections
            .Include(s => s.TypeSection)
            .AnyAsync(s => s.PlanEnteteId == planSourceId && s.TypeSection != null &&
                           (s.TypeSection.Code == "REGLAGE" || s.TypeSection.Code == "REGLAGE_PROD"));
    }



    public async Task<IEnumerable<PosteTravail>> GetPostesDisponiblesAsync()
    {
        return await _context.PosteTravails
            .Where(p => p.Actif)
            .ToListAsync();
    }

    public async Task<PosteTravail?> GetPosteWithMachinesAsync(string posteCode)
    {
        return await _context.PosteTravails
            .Include(p => p.CodeMachines)
            .FirstOrDefaultAsync(p => p.CodePoste == posteCode);
    }

    public async Task<IEnumerable<SopalTrace.Application.DTOs.Execution.Operateur.OperateurOfDto>> GetAllOfOperationsDisponiblesAsync()
    {
        // On récupère uniquement les OFs qui ont été préparés par le magasin (MagPreparationOf)
        var ofs = await _context.MagPreparationOfs
            .Include(m => m.NumeroOfNavigation)
                .ThenInclude(of => of.CodeArticleNavigation)
                    .ThenInclude(a => a.NatureArticleCodeNavigation)
                        .ThenInclude(na => na.NatureArticleOperations)
                            .ThenInclude(nao => nao.OperationCodeNavigation)
            .Where(m => m.Statut == "EN_COURS" || m.Statut == "PLANIFIE" || m.Statut == "EN_PAUSE")
            .ToListAsync();

        var result = new List<SopalTrace.Application.DTOs.Execution.Operateur.OperateurOfDto>();

        foreach (var magOf in ofs)
        {
            var of = magOf.NumeroOfNavigation;
            var article = of.CodeArticleNavigation;

            var activeExecs = await _context.ExecControleOfs
                .Where(e => e.NumeroOf == of.NumeroOf)
                .ToListAsync();

            var dto = new SopalTrace.Application.DTOs.Execution.Operateur.OperateurOfDto
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

                    if (execForOp != null)
                    {
                        aDesControlesReglage = await HasReglageSectionsAsync(execForOp.PlanSourceId);
                        var plan = await _context.PlanFabricationEntetes.FirstOrDefaultAsync(p => p.Id == execForOp.PlanSourceId);
                        if (plan != null)
                        {
                            legendeMoyens = plan.LegendeMoyens;
                            remarques = plan.Remarques;
                        }
                    }

                    dto.GammeOperatoire.Add(new SopalTrace.Application.DTOs.Execution.Operateur.OperateurOperationDto
                    {
                        OperationCode = op.OperationCode,
                        Libelle = op.OperationCodeNavigation?.Libelle ?? op.OperationCode,
                        MachinePrevueCode = machine?.CodeMachine ?? "",
                        ActiveExecControleOfId = execForOp?.Id,
                        ActiveExecStatut = execForOp?.Statut,
                        ActiveMachineCode = execForOp?.MachineCode,
                        EstEnReglage = execForOp?.EstEnReglage,
                        A_Des_Controles_Reglage = execForOp != null ? aDesControlesReglage : (bool?)null,
                        LegendeMoyens = legendeMoyens,
                        Remarques = remarques
                    });
                }
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<PlanFabricationLigne?> GetPlanLigneAsync(Guid ligneId)
    {
        return await _context.PlanFabricationLignes.FirstOrDefaultAsync(l => l.Id == ligneId);
    }

    public async Task<bool> AreAllOperationsClosedAsync(string numeroOf)
    {
        var of = await _context.MfgheadOrdreFabrications
            .Include(o => o.CodeArticleNavigation)
                .ThenInclude(a => a.NatureArticleCodeNavigation)
                    .ThenInclude(na => na.NatureArticleOperations)
            .FirstOrDefaultAsync(o => o.NumeroOf == numeroOf);

        if (of?.CodeArticleNavigation?.NatureArticleCodeNavigation?.NatureArticleOperations == null) 
            return false;

        var requiredOperations = of.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations
            .Select(o => o.OperationCode)
            .ToList();
        
        var execs = await _context.ExecControleOfs
            .Where(e => e.NumeroOf == numeroOf)
            .ToListAsync();

        foreach (var reqOp in requiredOperations)
        {
            var exec = execs.OrderByDescending(e => e.DateDebut).FirstOrDefault(e => e.OperationCode == reqOp);
            // Si une opération requise n'a jamais été commencée ou n'est pas clôturée, c'est faux.
            if (exec == null || exec.Statut != "CLOTURE")
                return false;
        }

        return true;
    }

    public async Task<bool> CanStartOperationAsync(string numeroOf, string operationCode)
    {
        var of = await _context.MfgheadOrdreFabrications
            .Include(o => o.CodeArticleNavigation)
                .ThenInclude(a => a.NatureArticleCodeNavigation)
                    .ThenInclude(na => na.NatureArticleOperations)
            .FirstOrDefaultAsync(o => o.NumeroOf == numeroOf);

        if (of?.CodeArticleNavigation?.NatureArticleCodeNavigation?.NatureArticleOperations == null) 
            return true;

        var operationsGamme = of.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations
            .OrderBy(o => o.OrdreGamme)
            .ToList();

        var index = operationsGamme.FindIndex(o => o.OperationCode == operationCode);
        if (index <= 0) return true; // C'est la première opération ou l'opération n'est pas dans la gamme

        var previousOp = operationsGamme[index - 1];

        // Vérifier si l'opération précédente a commencé
        var execPrecedente = await _context.ExecControleOfs
            .Where(e => e.NumeroOf == numeroOf && e.OperationCode == previousOp.OperationCode)
            .OrderByDescending(e => e.DateDebut)
            .FirstOrDefaultAsync();

        return execPrecedente != null;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
