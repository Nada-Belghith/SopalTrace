using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
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

    public async Task<int> GetDerniereVersionAsync(string operationCode, string typeRobinetCode, string? codeArticleSage)
    {
        var query = _context.PlanAssEntetes
            .Where(p => p.OperationCode == operationCode && p.TypeRobinetCode == typeRobinetCode);

        query = string.IsNullOrEmpty(codeArticleSage)
            ? query.Where(p => p.EstModele)
            : query.Where(p => !p.EstModele && p.CodeArticleSage == codeArticleSage);

        return await query.Select(p => (int?)p.Version).MaxAsync() ?? 0;
    }

    public async Task<bool> ExistePlanMaitreActifAsync(string operationCode, string typeRobinetCode)
    {
        return await _context.PlanAssEntetes.AnyAsync(p =>
            p.OperationCode == operationCode &&
            p.TypeRobinetCode == typeRobinetCode &&
            p.EstModele == true &&
            p.Statut == "ACTIF");
    }

    public async Task<bool> ExisteExceptionActiveAsync(string operationCode, string typeRobinetCode, string codeArticleSage)
    {
        return await _context.PlanAssEntetes.AnyAsync(p =>
            p.OperationCode == operationCode &&
            p.TypeRobinetCode == typeRobinetCode &&
            p.CodeArticleSage == codeArticleSage &&
            p.EstModele == false &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanAssEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanAssEntetes
            .Include(p => p.PlanAssSections)
                .ThenInclude(s => s.PlanAssLignes)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<PlanAssEntete?> GetPlanActifMaitreAsync(string operationCode, string typeRobinetCode)
    {
        return await _context.PlanAssEntetes
            .FirstOrDefaultAsync(p => p.OperationCode == operationCode && p.TypeRobinetCode == typeRobinetCode && p.EstModele == true && p.Statut == "ACTIF");
    }

    public async Task<PlanAssEntete?> GetPlanActifExceptionAsync(string operationCode, string typeRobinetCode, string codeArticleSage)
    {
        return await _context.PlanAssEntetes
            .FirstOrDefaultAsync(p => p.OperationCode == operationCode && p.TypeRobinetCode == typeRobinetCode && p.CodeArticleSage == codeArticleSage && p.EstModele == false && p.Statut == "ACTIF");
    }

    public async Task<PlanAssEntete?> GetPlanByIdAsync(Guid planId)
    {
        return await _context.PlanAssEntetes.FindAsync(planId);
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

    public async Task<List<PlanAssEntete>> GetPlansActifsAsync(string operationCode, string typeRobinetCode, string? codeArticleSage)
    {
        var query = _context.PlanAssEntetes.Where(p =>
            p.OperationCode == operationCode &&
            p.TypeRobinetCode == typeRobinetCode &&
            p.Statut == "ACTIF");

        query = string.IsNullOrEmpty(codeArticleSage)
            ? query.Where(p => p.EstModele)
            : query.Where(p => !p.EstModele && p.CodeArticleSage == codeArticleSage);

        return await query.ToListAsync();
    }
}