using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class PlanVerifMachineEnteteRepository : IPlanVerifMachineEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanVerifMachineEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<PlanVerifMachineEntete?> GetByIdAsync(Guid id, bool includeRelations = true)
    {
        var query = _context.PlanVerifMachineEntetes.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(p => p.Formulaire)
                .Include(p => p.PlanVerifMachineFamilles)
                .Include(p => p.PlanVerifMachineLignes).ThenInclude(l => l.PlanVerifMachineLigneExtraColonnes).Include(p => p.PlanVerifMachineLignes)
                    .ThenInclude(l => l.PlanVerifMachineEcheances)
                        .ThenInclude(e => e.PlanVerifMachineMatricePieces);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<PlanVerifMachineEntete>> GetAllWithRelationsAsync()
    {
        return await _context.PlanVerifMachineEntetes
            .Include(p => p.Formulaire)
            .Include(p => p.PlanVerifMachineFamilles)
            .Include(p => p.PlanVerifMachineLignes).ThenInclude(l => l.PlanVerifMachineLigneExtraColonnes).Include(p => p.PlanVerifMachineLignes)
                .ThenInclude(l => l.PlanVerifMachineEcheances)
                    .ThenInclude(e => e.PlanVerifMachineMatricePieces)
            .ToListAsync();
    }

    public async Task<IEnumerable<PlanVerifMachineEntete>> GetByMachineCodeAsync(string machineCode)
    {
        return await _context.PlanVerifMachineEntetes
            .Include(p => p.Formulaire)
            .Include(p => p.PlanVerifMachineFamilles)
            .Include(p => p.PlanVerifMachineLignes).ThenInclude(l => l.PlanVerifMachineLigneExtraColonnes).Include(p => p.PlanVerifMachineLignes)
                .ThenInclude(l => l.PlanVerifMachineEcheances)
                    .ThenInclude(e => e.PlanVerifMachineMatricePieces)
            .Where(p => p.MachineCode == machineCode)
            .ToListAsync();
    }

    public async Task AddAsync(PlanVerifMachineEntete entity)
    {
        await _context.PlanVerifMachineEntetes.AddAsync(entity);
    }

    public Task UpdateAsync(PlanVerifMachineEntete entity)
    {
        _context.PlanVerifMachineEntetes.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PlanVerifMachineEntete entity)
    {
        _context.PlanVerifMachineEntetes.Remove(entity);
        return Task.CompletedTask;
    }
}
