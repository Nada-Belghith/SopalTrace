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
        return await _context.PlanNcEntetes.AnyAsync(p =>
            p.PosteCode == posteCode &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanNcEntete?> GetPlanActifAsync(string posteCode)
    {
        return await _context.PlanNcEntetes.FirstOrDefaultAsync(p =>
            p.PosteCode == posteCode &&
            p.Statut == "ACTIF");
    }

    public async Task<List<PlanNcEntete>> GetTousLesPlansAsync()
    {
        return await _context.PlanNcEntetes
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();
    }

    public async Task<PlanNcEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanNcEntetes
            .Include(p => p.PlanNcLignes)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task AddPlanAsync(PlanNcEntete plan)
    {
        await _context.PlanNcEntetes.AddAsync(plan);
    }

    public void AddLigne(PlanNcLigne ligne)
    {
        _context.PlanNcLignes.Add(ligne);
    }

    public void RemoveLigne(PlanNcLigne ligne)
    {
        _context.PlanNcLignes.Remove(ligne);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
