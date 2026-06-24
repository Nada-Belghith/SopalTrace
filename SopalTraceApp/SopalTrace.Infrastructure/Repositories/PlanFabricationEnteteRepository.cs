using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class PlanFabricationEnteteRepository : IPlanFabricationEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanFabricationEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<PlanFabricationEntete?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        var query = _context.PlanFabricationEntetes.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(p => p.PlanFabricationSections)
                    .ThenInclude(s => s.PlanFabricationLignes)
                        .ThenInclude(l => l.PlanFabricationLigneExtraColonnes)
                .Include(p => p.Formulaire)
                .Include(p => p.ModeleSource);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<PlanFabricationEntete>> GetAllAsync(bool includeRelations = false)
    {
        var query = _context.PlanFabricationEntetes.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(p => p.PlanFabricationSections)
                    .ThenInclude(s => s.PlanFabricationLignes)
                        .ThenInclude(l => l.PlanFabricationLigneExtraColonnes);
        }

        return await query.ToListAsync();
    }

    public async Task AddAsync(PlanFabricationEntete document)
    {
        await _context.PlanFabricationEntetes.AddAsync(document);
    }

    public Task UpdateAsync(PlanFabricationEntete document)
    {
        _context.PlanFabricationEntetes.Update(document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PlanFabricationEntete document)
    {
        _context.PlanFabricationEntetes.Remove(document);
        return Task.CompletedTask;
    }

    public void RemoveSection(PlanFabricationSection section)
    {
        _context.PlanFabricationSections.Remove(section);
    }

    public void RemoveLigne(PlanFabricationLigne ligne)
    {
        _context.PlanFabricationLignes.Remove(ligne);
    }

    public void RemoveExtraColonne(PlanFabricationLigneExtraColonne extraColonne)
    {
        _context.Set<PlanFabricationLigneExtraColonne>().Remove(extraColonne);
    }

    public async Task<IEnumerable<PlanFabricationEntete>> GetByFormulaireIdAsync(Guid formulaireId)
    {
        return await _context.PlanFabricationEntetes
            .Where(p => p.FormulaireId == formulaireId)
            .ToListAsync();
    }

    public async Task<PlanFabricationEntete?> GetActifByReferenceAsync(string typeDocumentCode, string codeArticleSageVersionne, string? operationCode = null)
    {
        var query = _context.PlanFabricationEntetes.Where(p => p.Statut == "ACTIF" && p.CodeArticleSageVersionne == codeArticleSageVersionne);
        
        if (!string.IsNullOrWhiteSpace(operationCode))
        {
            query = query.Where(p => p.OperationCode == operationCode);
        }

        return await query
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.PlanFabricationLignes)
                    .ThenInclude(l => l.PlanFabricationLigneExtraColonnes)
            .FirstOrDefaultAsync();
    }
    
    public async Task<IEnumerable<PlanFabricationEntete>> GetByFiltersAsync(
        string? operationCode = null, 
        string? statut = null)
    {
        var query = _context.PlanFabricationEntetes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(operationCode))
            query = query.Where(p => p.OperationCode == operationCode);

        if (!string.IsNullOrWhiteSpace(statut) && statut != "ALL")
            query = query.Where(p => p.Statut == statut);

        return await query
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.PlanFabricationLignes)
                    .ThenInclude(l => l.PlanFabricationLigneExtraColonnes)
            .ToListAsync();
    }

    public async Task<int> GetLatestVersionAsync(
        string codeArticleSageVersionne, 
        string? operationCode = null)
    {
        var query = _context.PlanFabricationEntetes.Where(p => p.CodeArticleSageVersionne == codeArticleSageVersionne);

        if (!string.IsNullOrWhiteSpace(operationCode))
            query = query.Where(p => p.OperationCode == operationCode);
        
        if (await query.AnyAsync())
        {
            return await query.MaxAsync(p => p.Version);
        }

        return 0;
    }
}
