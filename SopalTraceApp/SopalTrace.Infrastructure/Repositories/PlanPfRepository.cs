using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class PlanPfRepository : IPlanPfRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanPfRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlanPfEntete>> GetGenericPlansAsync()
    {
        return await _context.PlanPfEntetes
            .AsNoTracking()
            .Include(p => p.FamilleProduitFiniCodeNavigation)
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();
    }

    public async Task<PlanPfEntete?> GetPlanByIdAsync(Guid id)
    {
        return await _context.PlanPfEntetes
            .Include(p => p.FamilleProduitFiniCodeNavigation)
            .Include(p => p.PlanPfSections)
                .ThenInclude(s => s.PlanPfLignes)
                    .ThenInclude(l => l.TypeCaracteristique)
            .Include(p => p.PlanPfSections)
                .ThenInclude(s => s.PlanPfLignes)
                    .ThenInclude(l => l.TypeControle)
            .Include(p => p.PlanPfSections)
                .ThenInclude(s => s.PlanPfLignes)
                    .ThenInclude(l => l.MoyenControle)
            .Include(p => p.PlanPfSections)
                .ThenInclude(s => s.PlanPfLignes)
                    .ThenInclude(l => l.Defautheque)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PlanPfEntete?> GetPlanPourArchivageAsync(Guid id)
    {
        return await _context.PlanPfEntetes
            .Include(p => p.PlanPfSections)
                .ThenInclude(s => s.PlanPfLignes)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> ExistsActiveOrDraftPlanAsync(string familleProduitFiniCode)
    {
        return await _context.PlanPfEntetes
            .AnyAsync(p => p.FamilleProduitFiniCode == familleProduitFiniCode && p.Statut != StatutsPlan.Archive);
    }

    public async Task<PlanPfEntete?> GetDraftPlanByFamilleAsync(string familleProduitFiniCode)
    {
        return await _context.PlanPfEntetes
            .FirstOrDefaultAsync(p => p.FamilleProduitFiniCode == familleProduitFiniCode && p.Statut == StatutsPlan.Brouillon);
    }

    public Task AddPlanAsync(PlanPfEntete plan)
    {
        _context.PlanPfEntetes.Add(plan);
        return Task.CompletedTask;
    }

    public async Task<List<PlanPfEntete>> GetActivePlansByFamilleAsync(string familleProduitFiniCode)
    {
        return await _context.PlanPfEntetes
            .Include(p => p.PlanPfSections)
                .ThenInclude(s => s.PlanPfLignes)
            .Where(p => p.FamilleProduitFiniCode == familleProduitFiniCode && p.Statut == StatutsPlan.Actif)
            .ToListAsync();
    }

    public Task UpdatePlanAsync(PlanPfEntete plan)
    {
        _context.PlanPfEntetes.Update(plan);
        return Task.CompletedTask;
    }

    public async Task<int> GetDerniereVersionPlanAsync(string familleProduitFiniCode)
    {
        return await _context.PlanPfEntetes
            .Where(p => p.FamilleProduitFiniCode == familleProduitFiniCode)
            .Select(p => (int?)p.Version)
            .MaxAsync() ?? -1;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public void ClearTracking()
    {
        _context.ChangeTracker.Clear();
    }

    public void DeletePlan(PlanPfEntete plan)
    {
        _context.PlanPfEntetes.Remove(plan);
    }
}
