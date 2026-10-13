using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution;

public class PlanificationProductionRepository : IPlanificationProductionRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanificationProductionRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> GetOfsAPlanifierAsync()
    {
        return await _context.ExecControleOfs
            .AsNoTracking()
            .Where(o => o.ExecProductionPeriods.Any(p => p.EndedAt == null) ||
                        (o.TypeOf == TypesOf.Assemblage && o.ExecControleOfSessions.Any(s => s.Statut == StatutsSession.EnCours)) ||
                        (o.TypeOf == TypesOf.Fabrication && o.Statut == StatutsExecution.EnCours))
            .Select(o => o.Id)
            .ToListAsync();
    }

    public async Task<List<ExecControleOfSession>> GetSessionsAsync(Guid execControleOfId)
    {
        return await _context.ExecControleOfSessions
            .AsNoTracking()
            .Where(s => s.ExecControleOfId == execControleOfId)
            .ToListAsync();
    }

    public async Task<List<ExecProductionPeriod>> GetPeriodesAsync(Guid execControleOfId)
    {
        return await _context.ExecProductionPeriods
            .Where(p => p.ExecControleOfId == execControleOfId)
            .OrderBy(p => p.StartedAt)
            .ToListAsync();
    }

    public async Task<List<ExecPrelevementIntermediaire>> GetOccurrencesNonReponduesAsync(Guid execControleOfId)
    {
        return await _context.ExecPrelevementIntermediaires
            .Where(o => o.ExecControleOfid == execControleOfId && o.ProductionPeriodId != null && !o.EstRepondu)
            .ToListAsync();
    }

    public async Task<HashSet<string>> GetTranchesSoldeesAsync(Guid execControleOfId)
    {
        var tranches = await _context.ExecControleTranches
            .AsNoTracking()
            .Where(t => t.ExecControleOfid == execControleOfId && t.ResultatFinal != null)
            .Select(t => t.TrancheHoraire)
            .ToListAsync();
        return tranches.ToHashSet();
    }

    public void AjouterPeriode(ExecProductionPeriod periode) => _context.ExecProductionPeriods.Add(periode);

    public void AjouterOccurrences(IEnumerable<ExecPrelevementIntermediaire> occurrences) =>
        _context.ExecPrelevementIntermediaires.AddRange(occurrences);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
