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

/// <summary>Modèles de fabrication stockés dans Document_Entete (type MODELE_FAB).</summary>
public class ModeleFabricationEnteteRepository : IModeleFabricationEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    public ModeleFabricationEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    private IQueryable<DocumentEntete> Modeles =>
        _context.DocumentEntetes.Where(m => m.TypeDocumentCode == TypesDocument.ModeleFabrication);

    private static IQueryable<DocumentEntete> AvecStructure(IQueryable<DocumentEntete> query) => query
        .AsSplitQuery()
        .Include(m => m.DocumentSections)
            .ThenInclude(s => s.DocumentLignes)
                .ThenInclude(l => l.DocumentLigneExtraColonnes)
        .Include(m => m.DocumentSections)
            .ThenInclude(s => s.DocumentLignes)
                .ThenInclude(l => l.DocumentLineImage);

    public async Task<DocumentEntete?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        var query = Modeles;

        if (includeRelations)
            query = AvecStructure(query).Include(m => m.Formulaire);

        return await query.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<DocumentEntete>> GetAllAsync(bool includeRelations = false)
    {
        var query = Modeles;

        if (includeRelations)
            query = AvecStructure(query);

        return await query.ToListAsync();
    }

    public async Task AddAsync(DocumentEntete document)
    {
        document.TypeDocumentCode = TypesDocument.ModeleFabrication;
        document.Portee = PorteesDocument.Generique;
        document.CodeArticleSageVersionne = null;
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

    public async Task<IEnumerable<DocumentEntete>> GetByFormulaireIdAsync(Guid formulaireId)
    {
        return await Modeles
            .Where(m => m.FormulaireId == formulaireId)
            .ToListAsync();
    }

    public async Task<DocumentEntete?> GetActifByReferenceAsync(string typeDocumentCode, string code, string? operationCode = null)
    {
        var query = Modeles.Where(m => m.Statut == DocumentStatuts.Actif && m.Nom == code);

        if (!string.IsNullOrWhiteSpace(operationCode))
            query = query.Where(m => m.OperationCode == operationCode);

        return await AvecStructure(query).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DocumentEntete>> GetByFiltersAsync(
        string? natureComposantCode = null,
        string? operationCode = null,
        string? familleProduitCode = null,
        string? statut = null)
    {
        var query = FiltrerParCriteres(Modeles, operationCode, natureComposantCode, familleProduitCode);

        if (!string.IsNullOrWhiteSpace(statut) && statut != "ALL")
            query = query.Where(m => m.Statut == statut);

        return await AvecStructure(query.Include(m => m.Formulaire)).ToListAsync();
    }

    public async Task<int> GetLatestVersionAsync(
        string code,
        string? operationCode = null,
        string? natureComposantCode = null,
        string? familleProduitCode = null)
    {
        var query = FiltrerParCriteres(Modeles.Where(m => m.Nom == code), operationCode, natureComposantCode, familleProduitCode);

        return await query.MaxAsync(m => (int?)m.Version) ?? 0;
    }

    /// <summary>La famille filtrée peut être une famille de produit fini ou un type de robinet (familles rattachées).</summary>
    private IQueryable<DocumentEntete> FiltrerParCriteres(
        IQueryable<DocumentEntete> query, string? operationCode, string? natureComposantCode, string? familleProduitCode)
    {
        if (!string.IsNullOrWhiteSpace(operationCode))
            query = query.Where(m => m.OperationCode == operationCode);

        if (!string.IsNullOrWhiteSpace(natureComposantCode))
            query = query.Where(m => m.NatureArticleCode == natureComposantCode);

        if (!string.IsNullOrWhiteSpace(familleProduitCode))
        {
            query = query.Where(m => m.FamilleProduitFiniCode == familleProduitCode ||
                                     _context.FamilleProduitFinis.Any(f => f.Code == m.FamilleProduitFiniCode && f.TypeRobinetCode == familleProduitCode));
        }

        return query;
    }
}
