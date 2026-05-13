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

    public async Task<bool> ExistePlanActifAsync()
    {
        return await _context.PlanEchantillonnageEntetes.AnyAsync(p =>
            p.Statut == "ACTIF");
    }

    public async Task<PlanEchantillonnageEntete?> GetPlanActifAsync()
    {
        return await _context.PlanEchantillonnageEntetes.FirstOrDefaultAsync(p =>
            p.Statut == "ACTIF");
    }

    public async Task<PlanEchantillonnageEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanEchantillonnageEntetes
            .Include(p => p.Nqa)
            .Include(p => p.PlanEchantillonnageRegles)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task AddPlanAsync(PlanEchantillonnageEntete plan)
    {
        await _context.PlanEchantillonnageEntetes.AddAsync(plan);
    }
    
    public async Task<int> GetOrCreateNqaAsync(double valeur)
    {
        var nqa = await _context.Nqas.FirstOrDefaultAsync(n => n.ValeurNqa == valeur);
        if (nqa == null)
        {
            nqa = new Nqa { ValeurNqa = valeur };
            await _context.Nqas.AddAsync(nqa);
            await _context.SaveChangesAsync();
        }
        return nqa.Id;
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw ex.ToDomainExceptionOrSelf("Le plan d'échantillonnage a été modifié/créé en parallèle.");
        }
    }
}