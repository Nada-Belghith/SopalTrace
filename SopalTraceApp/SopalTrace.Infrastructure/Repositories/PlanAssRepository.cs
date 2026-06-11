using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Constants;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class PlanAssRepository : IPlanAssRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanAssRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetDesignationArticleSageAsync(string codeArticleSage)
    {
        return await _context.Articles
            .Where(a => a.CodeArticle == codeArticleSage)
            .Select(a => a.Designation)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetDerniereVersionAsync(string operationCode, string? familleCode, string? codeArticleSage)
    {
        // On ignore codeArticleSage car non présent en base pour l'assemblage
        return await _context.PlanAssemblageEntetes
            .Where(p => p.OperationCode == operationCode && p.FamilleProduitFiniCode == familleCode)
            .Select(p => (int?)p.Version)
            .MaxAsync() ?? -1;
    }

    public async Task<int> GetDerniereVersionParCodeAsync(string code)
        => await _context.PlanAssemblageEntetes
            .Where(p => p.Designation == code)
            .Select(p => (int?)p.Version)
            .MaxAsync() ?? 0;

    public async Task<bool> ExistePlanMaitreActifAsync(string operationCode, string? familleCode, string? natureComposantCode, string? posteCode)
    {
        return await _context.PlanAssemblageEntetes.AnyAsync(p =>
            p.OperationCode == operationCode &&
            p.FamilleProduitFiniCode == familleCode &&
            p.NatureArticleCode == natureComposantCode &&
            p.PosteCode == posteCode &&
            p.Statut == StatutsPlan.Actif);
    }

    public async Task<bool> ExisteExceptionActiveAsync(string operationCode, string? familleCode, string? natureComposantCode, string? posteCode, string articleCode)
    {
        // Pour l'instant, on traite les exceptions comme des plans maîtres car pas de colonne Article spécifique en base pour l'instant (Plan_Ass)
        return await ExistePlanMaitreActifAsync(operationCode, familleCode, natureComposantCode, posteCode);
    }

    public async Task<bool> IsOperationValidePourNatureAsync(string natureCode, string operationCode)
    {
        // ✅ Vérification dans la table de gamme opératoire (NatureArticle_Operation)
        // et NON dans les plans existants, pour permettre la création du premier plan d'assemblage.
        return await _context.NatureArticleOperations
            .AnyAsync(g => g.NatureArticleCode == natureCode && g.OperationCode == operationCode);
    }

    public async Task<bool> ExisteParCodeAsync(string code)
        => await _context.PlanAssemblageEntetes.AnyAsync(p => p.Designation == code);

    public async Task<bool> ExisteParCodeEtLibelleAsync(string code, string libelle)
        => await _context.PlanAssemblageEntetes.AnyAsync(p => p.Designation == libelle && p.Statut == StatutsPlan.Actif);

    public async Task<PlanAssemblageEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanAssemblageEntetes
            .Include(p => p.PlanAssemblageSections)
                .ThenInclude(s => s.PlanAssemblageLignes)
            .Include(p => p.PlanAssemblageSections)
                .ThenInclude(s => s.RegleEchantillonnage)
            .Include(p => p.PlanAssemblageSections)
                .ThenInclude(s => s.Periodicite)
            .Include(p => p.Formulaire)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<PlanAssemblageEntete?> GetPlanActifMaitreAsync(string operationCode, string? familleCode, string? natureComposantCode, string? posteCode)
    {
        return await _context.PlanAssemblageEntetes
            .FirstOrDefaultAsync(p => 
                p.OperationCode == operationCode && 
                p.FamilleProduitFiniCode == familleCode && 
                p.NatureArticleCode == natureComposantCode &&
                p.PosteCode == posteCode &&
                p.Statut == StatutsPlan.Actif);
    }


    public async Task<PlanAssemblageEntete?> GetPlanActifParFormulaireAsync(Guid formulaireId)
    {
        return await _context.PlanAssemblageEntetes
            .FirstOrDefaultAsync(p =>
                p.FormulaireId == formulaireId &&
                p.Statut == StatutsPlan.Actif);
    }

    public async Task<PlanAssemblageEntete?> GetPlanActifExceptionAsync(string operationCode, string? familleCode, string codeArticleSage)
    {
        return await GetPlanActifMaitreAsync(operationCode, familleCode, null, null);
    }

    public async Task<PlanAssemblageEntete?> GetPlanByIdAsync(Guid planId)
    {
        return await _context.PlanAssemblageEntetes.FindAsync(planId);
    }

    public async Task<List<PlanAssemblageEntete>> GetPlansActifsAsync(string operationCode, string? familleCode, string? codeArticleSage)
    {
        return await _context.PlanAssemblageEntetes
            .Where(p => p.OperationCode == operationCode && p.FamilleProduitFiniCode == familleCode && p.Statut == StatutsPlan.Actif)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlanAssemblageEntete>> GetModelesParFiltresAsync(string? natureComposantCode, string? operationCode, string? posteCode = null, string? familleProduitCode = null)
    {
        var query = _context.PlanAssemblageEntetes
            .Include(p => p.Formulaire)
            .Where(p => p.Statut == StatutsPlan.Actif);

        if (!string.IsNullOrEmpty(natureComposantCode))
            query = query.Where(p => p.NatureArticleCode == natureComposantCode);

        if (!string.IsNullOrEmpty(operationCode))
            query = query.Where(p => p.OperationCode == operationCode);

        if (!string.IsNullOrEmpty(posteCode))
            query = query.Where(p => p.PosteCode == posteCode);

        if (!string.IsNullOrEmpty(familleProduitCode))
            query = query.Where(p => p.FamilleProduitFiniCode == familleProduitCode);

        return await query.ToListAsync();
    }

    public async Task AddPlanAsync(PlanAssemblageEntete plan)
    {
        await _context.PlanAssemblageEntetes.AddAsync(plan);
    }

    public void DeletePlan(PlanAssemblageEntete plan)
    {
        _context.PlanAssemblageEntetes.Remove(plan);
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw ex.ToDomainExceptionOrSelf("Le plan d'assemblage a été modifié/créé en parallèle.");
        }
    }
}
