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

    public async Task<bool> ExistePlanActifAsync(string typeRobinetCode, string operationCode, string posteCode)
    {
        return await _context.PlanNcEntetes.AnyAsync(p =>
            p.TypeRobinetCode == typeRobinetCode &&
            p.OperationCode == operationCode &&
            p.PosteCode == posteCode &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanNcEntete?> GetPlanActifAsync(string typeRobinetCode, string operationCode, string posteCode)
    {
        return await _context.PlanNcEntetes.FirstOrDefaultAsync(p =>
            p.TypeRobinetCode == typeRobinetCode &&
            p.OperationCode == operationCode &&
            p.PosteCode == posteCode &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanNcEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanNcEntetes
            .Include(p => p.PlanNcColonnes)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task AddPlanAsync(PlanNcEntete plan)
    {
        await _context.PlanNcEntetes.AddAsync(plan);
    }

    public void AddColonne(PlanNcColonne colonne)
    {
        _context.PlanNcColonnes.Add(colonne);
    }

    public void RemoveColonne(PlanNcColonne colonne)
    {
        _context.PlanNcColonnes.Remove(colonne);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
