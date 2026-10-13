using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

/// <summary>Plans de fabrication stockés dans Document_Entete (type PLAN_FAB, portée SPECIFIQUE).</summary>
public class PlanFabricationEnteteRepository : IPlanFabricationEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanFabricationEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    private IQueryable<DocumentEntete> Plans =>
        _context.DocumentEntetes.Where(p => p.TypeDocumentCode == TypesDocument.PlanFabrication);

    private static IQueryable<DocumentEntete> AvecStructure(IQueryable<DocumentEntete> query) => query
        .AsSplitQuery()
        .Include(p => p.DocumentSections)
            .ThenInclude(s => s.DocumentLignes)
                .ThenInclude(l => l.DocumentLigneExtraColonnes)
        .Include(p => p.DocumentSections)
            .ThenInclude(s => s.DocumentLignes)
                .ThenInclude(l => l.DocumentLineImage);

    public async Task<DocumentEntete?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        var query = Plans;

        if (includeRelations)
            query = AvecStructure(query).Include(p => p.Formulaire).Include(p => p.DocumentSource);

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<DocumentEntete>> GetAllAsync(bool includeRelations = false)
    {
        var query = Plans;

        if (includeRelations)
            query = AvecStructure(query);

        return await query.ToListAsync();
    }

    public async Task AddAsync(DocumentEntete document)
    {
        document.TypeDocumentCode = TypesDocument.PlanFabrication;
        document.Portee = PorteesDocument.Specifique;
        await _context.DocumentEntetes.AddAsync(document);
    }

    public Task UpdateAsync(DocumentEntete document)
    {
        _context.DocumentEntetes.Update(document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DocumentEntete document)
    {
        _context.DocumentEntetes.Remove(document);
        return Task.CompletedTask;
    }

    public void RemoveSection(DocumentSection section)
    {
        _context.DocumentSections.Remove(section);
    }

    public void RemoveLigne(DocumentLigne ligne)
    {
        _context.DocumentLignes.Remove(ligne);
    }

    public void RemoveExtraColonne(DocumentLigneExtraColonne extraColonne)
    {
        _context.DocumentLigneExtraColonnes.Remove(extraColonne);
    }

    /// <summary>Insert une nouvelle section directement dans le DbSet — EntityState.Added garanti.</summary>
    public void AddSection(DocumentSection section)
    {
        _context.DocumentSections.Add(section);
    }

    public async Task<IEnumerable<DocumentEntete>> GetByFormulaireIdAsync(Guid formulaireId)
    {
        return await Plans
            .Where(p => p.FormulaireId == formulaireId)
            .ToListAsync();
    }

    public async Task<DocumentEntete?> GetActifByReferenceAsync(string typeDocumentCode, string codeArticleSageVersionne, string? operationCode = null)
    {
        var query = Plans.Where(p => p.Statut == DocumentStatuts.Actif && p.CodeArticleSageVersionne == codeArticleSageVersionne);

        if (!string.IsNullOrWhiteSpace(operationCode))
            query = query.Where(p => p.OperationCode == operationCode);

        return await AvecStructure(query).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DocumentEntete>> GetByFiltersAsync(
        string? operationCode = null,
        string? statut = null)
    {
        var query = Plans;

        if (!string.IsNullOrWhiteSpace(operationCode))
            query = query.Where(p => p.OperationCode == operationCode);

        if (!string.IsNullOrWhiteSpace(statut) && statut != "ALL")
            query = query.Where(p => p.Statut == statut);

        return await AvecStructure(query).ToListAsync();
    }

    public async Task<int> GetLatestVersionAsync(
        string codeArticleSageVersionne,
        string? operationCode = null)
    {
        var query = Plans.Where(p => p.CodeArticleSageVersionne == codeArticleSageVersionne);

        if (!string.IsNullOrWhiteSpace(operationCode))
            query = query.Where(p => p.OperationCode == operationCode);

        return await query.MaxAsync(p => (int?)p.Version) ?? 0;
    }
}
