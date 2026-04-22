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
            .Include(p => p.TypeRobinetCodeNavigation)
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();
    }

    public async Task<PlanPfEntete?> GetPlanByIdAsync(Guid id)
    {
        return await _context.PlanPfEntetes
            .Include(p => p.TypeRobinetCodeNavigation)
            .Include(p => p.PlanPfSections)
                .ThenInclude(s => s.TypeSection)
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
            .Include(p => p.TypeRobinetCodeNavigation) // Ajouté pour éviter le NULL sur la FK
            .Include(p => p.PlanPfSections)
                .ThenInclude(s => s.PlanPfLignes)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> ExistsActiveOrDraftPlanAsync(string typeRobinetCode)
    {
        return await _context.PlanPfEntetes
            .AnyAsync(p => p.TypeRobinetCode == typeRobinetCode && p.Statut != StatutsPlan.Archive);
    }

    public async Task AddPlanAsync(PlanPfEntete plan)
    {
        _context.PlanPfEntetes.Add(plan);
    }

    public async Task<List<PlanPfEntete>> GetActivePlansByTypeRobinetAsync(string typeRobinetCode, Guid excludeId)
    {
        return await _context.PlanPfEntetes
            .Include(p => p.PlanPfSections)
                .ThenInclude(s => s.PlanPfLignes)
            .Where(p => p.TypeRobinetCode == typeRobinetCode && p.Statut == StatutsPlan.Actif && p.Id != excludeId)
            .ToListAsync();
    }

    public async Task<List<PlanPfEntete>> GetActivePlansByTypeRobinetAsync(string typeRobinetCode)
    {
        return await _context.PlanPfEntetes
            .Include(p => p.PlanPfSections)
                .ThenInclude(s => s.PlanPfLignes)
            .Where(p => p.TypeRobinetCode == typeRobinetCode && p.Statut == StatutsPlan.Actif)
            .ToListAsync();
    }

    public async Task UpdatePlanAsync(PlanPfEntete plan)
    {
        _context.PlanPfEntetes.Update(plan);
    }

    public async Task<int> GetDerniereVersionPlanAsync(string typeRobinetCode)
    {
        var derniereVersion = await _context.PlanPfEntetes
            .Where(p => p.TypeRobinetCode == typeRobinetCode)
            .OrderByDescending(p => p.Version)
            .Select(p => p.Version)
            .FirstOrDefaultAsync();

        return derniereVersion;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public void ClearTracking()
    {
        _context.ChangeTracker.Clear();
    }
}
