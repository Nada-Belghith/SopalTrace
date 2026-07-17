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

public class DocumentEchantillonnageEnteteRepository : IDocumentEchantillonnageEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    public DocumentEchantillonnageEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentEchantillonnageEntete?> GetByIdAsync(Guid id, bool includeRelations = true)
    {
        var query = _context.DocumentEchantillonnageEntetes.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(p => p.Nqa)
                .Include(p => p.DocumentEchantillonnageRegles);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<DocumentEchantillonnageEntete?> GetPlanActifAsync()
    {
        return await _context.DocumentEchantillonnageEntetes
            .Include(p => p.Nqa)
            .Include(p => p.DocumentEchantillonnageRegles)
            .Where(p => p.Statut == StatutsPlan.Actif)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DocumentEchantillonnageEntete>> GetAllWithRelationsAsync()
    {
        return await _context.DocumentEchantillonnageEntetes
            .Include(p => p.DocumentEchantillonnageRegles)
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();
    }

    public async Task AddAsync(DocumentEchantillonnageEntete entity)
    {
        await _context.DocumentEchantillonnageEntetes.AddAsync(entity);
    }

    public Task UpdateAsync(DocumentEchantillonnageEntete entity)
    {
        _context.DocumentEchantillonnageEntetes.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DocumentEchantillonnageEntete entity)
    {
        _context.DocumentEchantillonnageEntetes.Remove(entity);
        return Task.CompletedTask;
    }
}
