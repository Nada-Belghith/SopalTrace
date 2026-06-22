using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Constants;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SopalTrace.Infrastructure.Repositories;

public class PlanEchantillonnageEnteteRepository : IPlanEchantillonnageEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanEchantillonnageEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<PlanEchantillonnageEntete?> GetByIdAsync(Guid id, bool includeRelations = true)
    {
        var query = _context.PlanEchantillonnageEntetes.AsQueryable();

        if (includeRelations)
        {
            query = query.Include(p => p.PlanEchantillonnageRegles);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PlanEchantillonnageEntete?> GetPlanActifAsync()
    {
        return await _context.PlanEchantillonnageEntetes
            .Include(p => p.PlanEchantillonnageRegles)
            .Where(p => p.Statut == StatutsPlan.Actif)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<PlanEchantillonnageEntete>> GetAllWithRelationsAsync()
    {
        return await _context.PlanEchantillonnageEntetes
            .Include(p => p.PlanEchantillonnageRegles)
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();
    }

    public async Task AddAsync(PlanEchantillonnageEntete entity)
    {
        await _context.PlanEchantillonnageEntetes.AddAsync(entity);
    }

    public Task UpdateAsync(PlanEchantillonnageEntete entity)
    {
        _context.PlanEchantillonnageEntetes.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PlanEchantillonnageEntete entity)
    {
        _context.PlanEchantillonnageEntetes.Remove(entity);
        return Task.CompletedTask;
    }
}
