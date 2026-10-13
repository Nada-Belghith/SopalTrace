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

/// <summary>Plans d'échantillonnage : Document_Entete de type PLAN_ECHANTILLONNAGE avec ses paramètres (SamplingPlan).</summary>
public class DocumentEchantillonnageEnteteRepository : IDocumentEchantillonnageEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    public DocumentEchantillonnageEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    private IQueryable<DocumentEntete> Plans => _context.DocumentEntetes
        .Where(p => p.TypeDocumentCode == TypesDocument.PlanEchantillonnage)
        .Include(p => p.SamplingPlan)
            .ThenInclude(s => s!.AcceptableQualityLevel);

    public async Task<DocumentEntete?> GetByIdAsync(Guid id, bool includeRelations = true)
    {
        return await Plans.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<DocumentEntete?> GetPlanActifAsync()
    {
        return await Plans
            .Where(p => p.Statut == StatutsPlan.Actif)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DocumentEntete>> GetAllWithRelationsAsync()
    {
        return await Plans
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();
    }

    public async Task AddAsync(DocumentEntete entity)
    {
        entity.TypeDocumentCode = TypesDocument.PlanEchantillonnage;
        entity.Portee = PorteesDocument.Generique;
        await _context.DocumentEntetes.AddAsync(entity);
    }

    public Task UpdateAsync(DocumentEntete entity)
    {
        // Entité suivie : le suivi des modifications d'EF Core suffit.
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DocumentEntete entity)
    {
        _context.DocumentEntetes.Remove(entity);
        return Task.CompletedTask;
    }
}
