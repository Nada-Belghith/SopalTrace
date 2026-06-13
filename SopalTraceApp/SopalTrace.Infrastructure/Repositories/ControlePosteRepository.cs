using SopalTrace.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class ControlePosteRepository : IControlePosteRepository
{
    private readonly SopalTraceDbContext _context;

    public ControlePosteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistePlanActifAsync(string posteCode)
    {
        return await _context.PlanControlePosteEntetes.AnyAsync(p =>
            p.PosteCode == posteCode &&
            p.Statut == StatutsPlan.Actif);
    }

    public async Task<bool> ExistePlanActifParFormulaireAsync(Guid formulaireId)
    {
        return await _context.PlanControlePosteEntetes.AnyAsync(p =>
            p.FormulaireId == formulaireId &&
            p.Statut == StatutsPlan.Actif);
    }

    public async Task<PlanControlePosteEntete?> GetPlanActifAsync(string posteCode)
    {
        return await _context.PlanControlePosteEntetes.FirstOrDefaultAsync(p =>
            p.PosteCode == posteCode &&
            p.Statut == StatutsPlan.Actif);
    }

    public async Task<PlanControlePosteEntete?> GetPlanActifParFormulaireAsync(Guid formulaireId)
    {
        return await _context.PlanControlePosteEntetes.FirstOrDefaultAsync(p =>
            p.FormulaireId == formulaireId &&
            p.Statut == StatutsPlan.Actif);
    }

    public async Task<List<PlanControlePosteEntete>> GetTousLesPlansAsync()
    {
        return await _context.PlanControlePosteEntetes
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();
    }

    public async Task<PlanControlePosteEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanControlePosteEntetes
            .Include(p => p.PlanControlePosteLignes)
                .ThenInclude(l => l.RisqueDefaut)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task AddPlanAsync(PlanControlePosteEntete plan)
    {
        await _context.PlanControlePosteEntetes.AddAsync(plan);
    }

    public void AddLigne(PlanControlePosteLigne ligne)
    {
        _context.PlanControlePosteLignes.Add(ligne);
    }

    public void RemoveLigne(PlanControlePosteLigne ligne)
    {
        _context.PlanControlePosteLignes.Remove(ligne);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
