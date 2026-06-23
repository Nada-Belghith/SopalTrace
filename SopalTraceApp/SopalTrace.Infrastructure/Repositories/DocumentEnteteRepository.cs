using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class DocumentEnteteRepository : IDocumentEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    private string GetBaseNom(string nom)
    {
        if (string.IsNullOrWhiteSpace(nom)) return nom;
        var regex = new Regex(@"([ -]*)V\d+$", RegexOptions.IgnoreCase);
        return regex.Replace(nom, string.Empty).TrimEnd('-').Trim();
    }

    public DocumentEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentEntete?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        var query = _context.DocumentEntetes.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(d => d.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                .Include(d => d.TypeDocumentCodeNavigation)
                .Include(e => e.Formulaire)
                .Include(e => e.DocumentSections)
                .ThenInclude(s => s.DocumentLignes)
                    .ThenInclude(l => l.DocumentLigneExtraColonnes);
        }

        return await query.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<DocumentEntete>> GetAllAsync(bool includeRelations = false)
    {
        var query = _context.DocumentEntetes.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(d => d.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                .Include(d => d.TypeDocumentCodeNavigation);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<DocumentEntete>> GetByFormulaireIdAsync(Guid formulaireId)
    {
        return await _context.DocumentEntetes
            .Where(d => d.FormulaireId == formulaireId)
            .ToListAsync();
    }

    public async Task AddAsync(DocumentEntete document)
    {
        await _context.DocumentEntetes.AddAsync(document);
    }

    public Task UpdateAsync(DocumentEntete document)
    {
        if (_context.Entry(document).State == EntityState.Detached)
        {
            _context.DocumentEntetes.Attach(document);
        }
        _context.Entry(document).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DocumentEntete document)
    {
        if (document.DocumentSections != null)
        {
            foreach (var section in document.DocumentSections.ToList())
            {
                if (section.DocumentLignes != null)
                {
                    foreach (var ligne in section.DocumentLignes.ToList())
                    {
                        if (ligne.DocumentLigneExtraColonnes != null)
                        {
                            _context.DocumentLigneExtraColonnes.RemoveRange(ligne.DocumentLigneExtraColonnes);
                        }
                    }
                    _context.DocumentLignes.RemoveRange(section.DocumentLignes);
                }
            }
            _context.DocumentSections.RemoveRange(document.DocumentSections);
        }
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

    public void RemoveExtraColonne(DocumentLigneExtraColonne ec)
    {
        _context.DocumentLigneExtraColonnes.Remove(ec);
    }



    public async Task<DocumentEntete?> GetActifByReferenceAsync(string typeDocumentCode, string nom, string? operationCode = null, string? posteCode = null)
    {
        var baseNom = GetBaseNom(nom);
        var query = _context.DocumentEntetes
            .Include(d => d.TypeDocumentCodeNavigation)
            .Where(d => d.TypeDocumentCodeNavigation != null && d.TypeDocumentCodeNavigation.Code == typeDocumentCode && d.Statut == "ACTIF")
            .Where(d => d.Nom.StartsWith(baseNom));

        if (!string.IsNullOrEmpty(operationCode))
        {
            query = query.Where(d => d.OperationCode == operationCode);
        }

        if (!string.IsNullOrEmpty(posteCode))
        {
            query = query.Where(d => d.PosteCode == posteCode);
        }

        var docs = await query.ToListAsync();
        return docs.Where(d => GetBaseNom(d.Nom) == baseNom).OrderByDescending(d => d.Version).FirstOrDefault();
    }

    public async Task<IEnumerable<DocumentEntete>> GetByFiltersAsync(
        string typeDocumentCode, 
        string? natureComposantCode = null, 
        string? operationCode = null, 
        string? posteCode = null, 
        string? familleProduitCode = null,
        string? statut = null)
    {
        var query = _context.DocumentEntetes
            .Include(d => d.TypeDocumentCodeNavigation)
            .Where(d => d.TypeDocumentCodeNavigation != null && d.TypeDocumentCodeNavigation.Code == typeDocumentCode)
            .AsQueryable();

        if (!string.IsNullOrEmpty(natureComposantCode))
            query = query.Where(d => d.NatureArticleCode == natureComposantCode);

        if (!string.IsNullOrEmpty(operationCode))
            query = query.Where(d => d.OperationCode == operationCode);

        if (!string.IsNullOrEmpty(posteCode))
            query = query.Where(d => d.PosteCode == posteCode);

        if (!string.IsNullOrEmpty(familleProduitCode))
            query = query.Where(d => d.FamilleProduitFiniCode == familleProduitCode);
            
        if (!string.IsNullOrEmpty(statut))
            query = query.Where(d => d.Statut == statut);

        return await query.ToListAsync();
    }

    public async Task<bool> ExistsByNomAndDesignationAsync(string nom, string designation, string typeDocumentCode)
    {
        var baseNom = GetBaseNom(nom);
        var docs = await _context.DocumentEntetes
            .Include(d => d.TypeDocumentCodeNavigation)
            .Where(d => d.TypeDocumentCodeNavigation != null && d.TypeDocumentCodeNavigation.Code == typeDocumentCode 
                        && d.Nom.StartsWith(baseNom))
            .ToListAsync();
            
        return docs.Any(d => GetBaseNom(d.Nom) == baseNom && d.Designation == designation);
    }

    public async Task<int> GetLatestVersionAsync(
        string typeDocumentCode, 
        string nom, 
        string? operationCode = null,
        string? posteCode = null,
        string? natureComposantCode = null,
        string? familleProduitCode = null)
    {
        var baseNom = GetBaseNom(nom);
        var query = _context.DocumentEntetes
            .Include(d => d.TypeDocumentCodeNavigation)
            .Where(d => d.TypeDocumentCodeNavigation != null && d.TypeDocumentCodeNavigation.Code == typeDocumentCode 
                        && d.Nom.StartsWith(baseNom));
        
        if (!string.IsNullOrEmpty(operationCode))
        {
            query = query.Where(d => d.OperationCode == operationCode);
        }

        if (!string.IsNullOrEmpty(posteCode))
        {
            query = query.Where(d => d.PosteCode == posteCode);
        }

        if (!string.IsNullOrEmpty(natureComposantCode))
        {
            query = query.Where(d => d.NatureArticleCode == natureComposantCode);
        }

        if (!string.IsNullOrEmpty(familleProduitCode))
        {
            query = query.Where(d => d.FamilleProduitFiniCode == familleProduitCode);
        }

        var docs = await query.ToListAsync();
        var doc = docs.Where(d => GetBaseNom(d.Nom) == baseNom).OrderByDescending(d => d.Version).FirstOrDefault();

        return doc?.Version ?? 0;
    }
}
