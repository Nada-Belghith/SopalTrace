using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class ModeleFabricationEnteteRepository : IModeleFabricationEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    public ModeleFabricationEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<ModeleFabricationEntete?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        var query = _context.ModeleFabricationEntetes.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(m => m.ModeleFabricationSections)
                    .ThenInclude(s => s.ModeleFabricationLignes)
                        .ThenInclude(l => l.ModeleFabricationLigneExtraColonnes)
                .Include(m => m.Formulaire);
        }

        return await query.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<ModeleFabricationEntete>> GetAllAsync(bool includeRelations = false)
    {
        var query = _context.ModeleFabricationEntetes.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(m => m.ModeleFabricationSections)
                    .ThenInclude(s => s.ModeleFabricationLignes)
                        .ThenInclude(l => l.ModeleFabricationLigneExtraColonnes);
        }

        return await query.ToListAsync();
    }

    public async Task AddAsync(ModeleFabricationEntete document)
    {
        await _context.ModeleFabricationEntetes.AddAsync(document);
    }

    public Task UpdateAsync(ModeleFabricationEntete document)
    {
        _context.ModeleFabricationEntetes.Update(document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ModeleFabricationEntete document)
    {
        _context.ModeleFabricationEntetes.Remove(document);
        return Task.CompletedTask;
    }

    public void RemoveSection(ModeleFabricationSection section)
    {
        _context.ModeleFabricationSections.Remove(section);
    }

    public void RemoveLigne(ModeleFabricationLigne ligne)
    {
        _context.ModeleFabricationLignes.Remove(ligne);
    }

    public async Task<IEnumerable<ModeleFabricationEntete>> GetByFormulaireIdAsync(Guid formulaireId)
    {
        return await _context.ModeleFabricationEntetes
            .Where(m => m.FormulaireId == formulaireId)
            .ToListAsync();
    }

    public async Task<ModeleFabricationEntete?> GetActifByReferenceAsync(string typeDocumentCode, string code, string? operationCode = null)
    {
        var query = _context.ModeleFabricationEntetes.Where(m => m.Statut == "ACTIF" && m.Code == code);
        
        if (!string.IsNullOrWhiteSpace(operationCode))
        {
            query = query.Where(m => m.OperationCode == operationCode);
        }

        return await query
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.ModeleFabricationLignes)
                    .ThenInclude(l => l.ModeleFabricationLigneExtraColonnes)
            .FirstOrDefaultAsync();
    }
    
    public async Task<IEnumerable<ModeleFabricationEntete>> GetByFiltersAsync(
        string? natureComposantCode = null, 
        string? operationCode = null, 
        string? familleProduitCode = null,
        string? statut = null)
    {
        var query = _context.ModeleFabricationEntetes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(natureComposantCode))
            query = query.Where(m => m.NatureArticleCode == natureComposantCode);

        if (!string.IsNullOrWhiteSpace(operationCode))
            query = query.Where(m => m.OperationCode == operationCode);

        if (!string.IsNullOrWhiteSpace(familleProduitCode))
        {
            query = query.Where(m => m.FamilleProduitFiniCode == familleProduitCode || 
                                     _context.FamilleProduitFinis.Any(f => f.Code == m.FamilleProduitFiniCode && f.TypeRobinetCode == familleProduitCode));
        }

        if (!string.IsNullOrWhiteSpace(statut) && statut != "ALL")
            query = query.Where(m => m.Statut == statut);

        return await query
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.ModeleFabricationLignes)
                    .ThenInclude(l => l.ModeleFabricationLigneExtraColonnes)
            .ToListAsync();
    }

    public async Task<int> GetLatestVersionAsync(
        string code, 
        string? operationCode = null,
        string? natureComposantCode = null,
        string? familleProduitCode = null)
    {
        var query = _context.ModeleFabricationEntetes.Where(m => m.Code == code);

        if (!string.IsNullOrWhiteSpace(operationCode))
            query = query.Where(m => m.OperationCode == operationCode);
        
        if (!string.IsNullOrWhiteSpace(natureComposantCode))
            query = query.Where(m => m.NatureArticleCode == natureComposantCode);

        if (!string.IsNullOrWhiteSpace(familleProduitCode))
        {
            query = query.Where(m => m.FamilleProduitFiniCode == familleProduitCode || 
                                     _context.FamilleProduitFinis.Any(f => f.Code == m.FamilleProduitFiniCode && f.TypeRobinetCode == familleProduitCode));
        }

        if (await query.AnyAsync())
        {
            return await query.MaxAsync(m => m.Version);
        }

        return 0;
    }
}
