using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class PlanNcRepository : IPlanNcRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanNcRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistePlanActifAsync(string posteCode)
    {
        return await _context.PlanNonConformiteEntetes.AnyAsync(p =>
            p.PosteCode == posteCode &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanNonConformiteEntete?> GetPlanActifAsync(string posteCode)
    {
        return await _context.PlanNonConformiteEntetes.FirstOrDefaultAsync(p =>
            p.PosteCode == posteCode &&
            p.Statut == "ACTIF");
    }

    public async Task<List<PlanNonConformiteEntete>> GetTousLesPlansAsync()
    {
        return await _context.PlanNonConformiteEntetes
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();
    }

    public async Task<PlanNonConformiteEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanNonConformiteEntetes
            .Include(p => p.PlanNonConformiteLignes)
                .ThenInclude(l => l.RisqueDefaut)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task AddPlanAsync(PlanNonConformiteEntete plan)
    {
        await _context.PlanNonConformiteEntetes.AddAsync(plan);
    }

    public void AddLigne(PlanNonConformiteLigne ligne)
    {
        _context.PlanNonConformiteLignes.Add(ligne);
    }

    public void RemoveLigne(PlanNonConformiteLigne ligne)
    {
        _context.PlanNonConformiteLignes.Remove(ligne);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
