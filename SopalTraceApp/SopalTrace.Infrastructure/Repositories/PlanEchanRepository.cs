using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class PlanEchanRepository : IPlanEchanRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanEchanRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistePlanActifAsync(string codeReference)
    {
        return await _context.PlanEchantillonnageEntetes.AnyAsync(p =>
            p.CodeReference == codeReference &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanEchantillonnageEntete?> GetPlanActifAsync(string codeReference)
    {
        return await _context.PlanEchantillonnageEntetes.FirstOrDefaultAsync(p =>
            p.CodeReference == codeReference &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanEchantillonnageEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanEchantillonnageEntetes
            .Include(p => p.Formulaire)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task AddPlanAsync(PlanEchantillonnageEntete plan)
    {
        await _context.PlanEchantillonnageEntetes.AddAsync(plan);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}