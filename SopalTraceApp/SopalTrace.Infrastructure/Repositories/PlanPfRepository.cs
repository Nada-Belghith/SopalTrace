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

    public async Task<List<PlanProduitFiniEntete>> GetGenericPlansAsync()
    {
        return await _context.PlanProduitFiniEntetes
            .AsNoTracking()
            .Include(p => p.FamilleProduitFiniCodeNavigation)
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();
    }

    public async Task<PlanProduitFiniEntete?> GetPlanByIdAsync(Guid id)
    {
        return await _context.PlanProduitFiniEntetes
            .Include(p => p.Formulaire)
            .Include(p => p.FamilleProduitFiniCodeNavigation)
            .Include(p => p.PlanProduitFiniSections)
                .ThenInclude(s => s.PlanProduitFiniLignes)
                    .ThenInclude(l => l.TypeCaracteristique)
            .Include(p => p.PlanProduitFiniSections)
                .ThenInclude(s => s.PlanProduitFiniLignes)
                    .ThenInclude(l => l.TypeControle)
            .Include(p => p.PlanProduitFiniSections)
                .ThenInclude(s => s.PlanProduitFiniLignes)
                    .ThenInclude(l => l.MoyenControle)
            .Include(p => p.PlanProduitFiniSections)
                .ThenInclude(s => s.PlanProduitFiniLignes)
                    .ThenInclude(l => l.Defautheque)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PlanProduitFiniEntete?> GetPlanPourArchivageAsync(Guid id)
    {
        return await _context.PlanProduitFiniEntetes
            .Include(p => p.PlanProduitFiniSections)
                .ThenInclude(s => s.PlanProduitFiniLignes)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> ExistsActiveOrDraftPlanAsync(string familleProduitFiniCode)
    {
        return await _context.PlanProduitFiniEntetes
            .AnyAsync(p => p.FamilleProduitFiniCode == familleProduitFiniCode && p.Statut != StatutsPlan.Archive);
    }

    public async Task<PlanProduitFiniEntete?> GetDraftPlanByFamilleAsync(string familleProduitFiniCode)
    {
        return await _context.PlanProduitFiniEntetes
            .FirstOrDefaultAsync(p => p.FamilleProduitFiniCode == familleProduitFiniCode && p.Statut == StatutsPlan.Brouillon);
    }

    public Task AddPlanAsync(PlanProduitFiniEntete plan)
    {
        _context.PlanProduitFiniEntetes.Add(plan);
        return Task.CompletedTask;
    }

    public async Task<List<PlanProduitFiniEntete>> GetActivePlansByFamilleAsync(string familleProduitFiniCode)
    {
        return await _context.PlanProduitFiniEntetes
            .Include(p => p.PlanProduitFiniSections)
                .ThenInclude(s => s.PlanProduitFiniLignes)
            .Where(p => p.FamilleProduitFiniCode == familleProduitFiniCode && p.Statut == StatutsPlan.Actif)
            .ToListAsync();
    }

    public Task UpdatePlanAsync(PlanProduitFiniEntete plan)
    {
        _context.PlanProduitFiniEntetes.Update(plan);
        return Task.CompletedTask;
    }

    public async Task<int> GetDerniereVersionPlanAsync(string familleProduitFiniCode)
    {
        return await _context.PlanProduitFiniEntetes
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

    public void DeletePlan(PlanProduitFiniEntete plan)
    {
        _context.PlanProduitFiniEntetes.Remove(plan);
    }
}
