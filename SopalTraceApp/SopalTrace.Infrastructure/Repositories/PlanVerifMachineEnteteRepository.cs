using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class DocumentVerifMachineEnteteRepository : IDocumentVerifMachineEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    public DocumentVerifMachineEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentVerifMachineEntete?> GetByIdAsync(Guid id, bool includeRelations = true)
    {
        var query = _context.DocumentVerifMachineEntetes.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(p => p.Formulaire)
                .Include(p => p.DocumentVerifMachineFamilles)
                .Include(p => p.DocumentVerifMachineLignes).ThenInclude(l => l.DocumentVerifMachineLigneExtraColonnes).Include(p => p.DocumentVerifMachineLignes)
                    .ThenInclude(l => l.DocumentVerifMachineEcheances)
                        .ThenInclude(e => e.DocumentVerifMachineMatricePieces);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<DocumentVerifMachineEntete>> GetAllWithRelationsAsync()
    {
        return await _context.DocumentVerifMachineEntetes
            .Include(p => p.Formulaire)
            .Include(p => p.DocumentVerifMachineFamilles)
            .Include(p => p.DocumentVerifMachineLignes).ThenInclude(l => l.DocumentVerifMachineLigneExtraColonnes).Include(p => p.DocumentVerifMachineLignes)
                .ThenInclude(l => l.DocumentVerifMachineEcheances)
                    .ThenInclude(e => e.DocumentVerifMachineMatricePieces)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentVerifMachineEntete>> GetByMachineCodeAsync(string machineCode)
    {
        return await _context.DocumentVerifMachineEntetes
            .Include(p => p.Formulaire)
            .Include(p => p.DocumentVerifMachineFamilles)
            .Include(p => p.DocumentVerifMachineLignes).ThenInclude(l => l.DocumentVerifMachineLigneExtraColonnes).Include(p => p.DocumentVerifMachineLignes)
                .ThenInclude(l => l.DocumentVerifMachineEcheances)
                    .ThenInclude(e => e.DocumentVerifMachineMatricePieces)
            .Where(p => p.MachineCode == machineCode)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentVerifMachineEntete>> GetByFormulaireIdAsync(Guid formulaireId)
    {
        return await _context.DocumentVerifMachineEntetes
            .Where(p => p.FormulaireId == formulaireId)
            .ToListAsync();
    }

    public async Task AddAsync(DocumentVerifMachineEntete entity)
    {
        await _context.DocumentVerifMachineEntetes.AddAsync(entity);
    }

    public Task UpdateAsync(DocumentVerifMachineEntete entity)
    {
        _context.DocumentVerifMachineEntetes.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DocumentVerifMachineEntete entity)
    {
        _context.DocumentVerifMachineEntetes.Remove(entity);
        return Task.CompletedTask;
    }
}
