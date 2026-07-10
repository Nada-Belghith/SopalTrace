using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution;

public class OccurrenceRepository : IOccurrenceRepository
{
    private readonly SopalTraceDbContext _context;

    public OccurrenceRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<ExecControleOf?> GetExecControleOfWithIntermediairesAsync(Guid execControleOfId)
    {
        return await _context.ExecControleOfs
            .Include(o => o.ExecPrelevementIntermediaires)
            .Include(o => o.ExecPieceTypes)
            .Include(o => o.ExecControleTranches)
            .Include(o => o.NumeroOfNavigation)
            .FirstOrDefaultAsync(o => o.Id == execControleOfId);
    }

    public async Task<List<PlanFabricationSection>> GetSectionsActivesAsync(Guid planEnteteId)
    {
        return await _context.Set<PlanFabricationSection>()
            .Include(s => s.Periodicite)
            .Include(s => s.TypeSection)
            .Where(s => s.PlanEnteteId == planEnteteId)
            .ToListAsync();
    }

    public async Task<List<ExecPrelevementIntermediaire>> GetIntermediairesActifsAsync(Guid execControleOfId)
    {
        return await _context.ExecPrelevementIntermediaires
            .Where(p => p.ExecControleOfid == execControleOfId && !p.EstRepondu)
            .ToListAsync();
    }

    public async Task<ExecPrelevementIntermediaire?> GetIntermediaireAsync(Guid id)
    {
        return await _context.ExecPrelevementIntermediaires.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<ExecPrelevementIntermediaire>> GetIntermediairesParTrancheAsync(Guid execControleOfId, string trancheHoraire)
    {
        return await _context.ExecPrelevementIntermediaires
            .Where(o => o.ExecControleOfid == execControleOfId && o.TrancheHoraire == trancheHoraire)
            .ToListAsync();
    }

    public async Task<ExecControleTranche?> GetTrancheExistanteAsync(Guid execControleOfId, string trancheHoraire)
    {
        return await _context.ExecControleTranches
            .FirstOrDefaultAsync(t => t.ExecControleOfid == execControleOfId && t.TrancheHoraire == trancheHoraire);
    }

    public async Task<ExecControleTranche?> GetDerniereTrancheAsync(Guid execControleOfId)
    {
        return await _context.ExecControleTranches
            .Where(t => t.ExecControleOfid == execControleOfId)
            .OrderByDescending(t => t.HeureDebut)
            .FirstOrDefaultAsync();
    }

    public void AddIntermediaires(IEnumerable<ExecPrelevementIntermediaire> intermediaires)
    {
        _context.ExecPrelevementIntermediaires.AddRange(intermediaires);
    }

    public void AddTranche(ExecControleTranche tranche)
    {
        _context.ExecControleTranches.Add(tranche);
    }

    public void AddPieceType(ExecPieceType pieceType)
    {
        _context.ExecPieceTypes.Add(pieceType);
    }



    public void RemoveIntermediaires(IEnumerable<ExecPrelevementIntermediaire> intermediaires)
    {
        _context.ExecPrelevementIntermediaires.RemoveRange(intermediaires);
    }

    public async Task<List<PlanFabricationLigne>> GetLignesForSectionAsync(Guid sectionId)
    {
        return await _context.PlanFabricationLignes
            .Include(l => l.TypeControle)
            .Include(l => l.MoyenControle)
            .Where(l => l.SectionId == sectionId)
            .OrderBy(l => l.OrdreAffiche)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
