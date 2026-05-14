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
        var article = await _context.Itmmasters.FirstOrDefaultAsync(a => a.CodeArticle == codeArticleSage);
        return article?.Designation;
    }

    public async Task<int> GetDerniereVersionAsync(string operationCode, string? familleCode, string? codeArticleSage)
    {
        // On ignore codeArticleSage car non présent en base pour l'assemblage
        return await _context.PlanAssEntetes
            .Where(p => p.OperationCode == operationCode && p.FamilleProduitFiniCode == familleCode)
            .Select(p => (int?)p.Version)
            .MaxAsync() ?? -1;
    }

    public async Task<int> GetDerniereVersionParCodeAsync(string code)
        => await _context.PlanAssEntetes
            .Where(p => p.Designation == code)
            .Select(p => (int?)p.Version)
            .MaxAsync() ?? 0;

    public async Task<bool> ExistePlanMaitreActifAsync(string operationCode, string? familleCode, string? natureComposantCode, string? posteCode)
    {
        return await _context.PlanAssEntetes.AnyAsync(p =>
            p.OperationCode == operationCode &&
            p.FamilleProduitFiniCode == familleCode &&
            p.NatureComposantCode == natureComposantCode &&
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
        return await _context.NatureComposantOperations
            .AnyAsync(g => g.NatureComposantCode == natureCode && g.OperationCode == operationCode);
    }

    public async Task<bool> ExisteParCodeAsync(string code)
        => await _context.PlanAssEntetes.AnyAsync(p => p.Designation == code);

    public async Task<bool> ExisteParCodeEtLibelleAsync(string code, string libelle)
        => await _context.PlanAssEntetes.AnyAsync(p => p.Designation == libelle && p.Statut == StatutsPlan.Actif);

    public async Task<PlanAssEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanAssEntetes
            .Include(p => p.PlanAssSections)
                .ThenInclude(s => s.PlanAssLignes)
            .Include(p => p.PlanAssSections)
                .ThenInclude(s => s.RegleEchantillonnage)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<PlanAssEntete?> GetPlanActifMaitreAsync(string operationCode, string? familleCode, string? natureComposantCode, string? posteCode)
    {
        return await _context.PlanAssEntetes
            .FirstOrDefaultAsync(p => 
                p.OperationCode == operationCode && 
                p.FamilleProduitFiniCode == familleCode && 
                p.NatureComposantCode == natureComposantCode &&
                p.PosteCode == posteCode &&
                p.Statut == StatutsPlan.Actif);
    }

    public async Task<PlanAssEntete?> GetPlanActifExceptionAsync(string operationCode, string? familleCode, string codeArticleSage)
    {
        return await GetPlanActifMaitreAsync(operationCode, familleCode, null, null);
    }

    public async Task<PlanAssEntete?> GetPlanByIdAsync(Guid planId)
    {
        return await _context.PlanAssEntetes.FindAsync(planId);
    }

    public async Task<List<PlanAssEntete>> GetPlansActifsAsync(string operationCode, string? familleCode, string? codeArticleSage)
    {
        return await _context.PlanAssEntetes
            .Where(p => p.OperationCode == operationCode && p.FamilleProduitFiniCode == familleCode && p.Statut == StatutsPlan.Actif)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlanAssEntete>> GetModelesParFiltresAsync(string? natureComposantCode, string? operationCode, string? posteCode = null, string? familleProduitCode = null)
    {
        var query = _context.PlanAssEntetes.Where(p => p.Statut == StatutsPlan.Actif);

        if (!string.IsNullOrEmpty(natureComposantCode))
            query = query.Where(p => p.NatureComposantCode == natureComposantCode);

        if (!string.IsNullOrEmpty(operationCode))
            query = query.Where(p => p.OperationCode == operationCode);

        if (!string.IsNullOrEmpty(posteCode))
            query = query.Where(p => p.PosteCode == posteCode);

        if (!string.IsNullOrEmpty(familleProduitCode))
            query = query.Where(p => p.FamilleProduitFiniCode == familleProduitCode);

        return await query.ToListAsync();
    }

    public async Task AddPlanAsync(PlanAssEntete plan)
    {
        await _context.PlanAssEntetes.AddAsync(plan);
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
