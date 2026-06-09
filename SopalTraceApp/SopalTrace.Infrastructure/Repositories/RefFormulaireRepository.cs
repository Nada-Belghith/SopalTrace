using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class RefFormulaireRepository : IRefFormulaireRepository
{
    private readonly SopalTraceDbContext _context;

    public RefFormulaireRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<RefFormulaire?> GetByIdAsync(Guid id)
    {
        return await _context.RefFormulaires.FirstOrDefaultAsync(r => r.Id == id);
    }

    public Task UpdateAsync(RefFormulaire entity)
    {
        _context.RefFormulaires.Update(entity);
        return Task.CompletedTask;
    }

    public async Task AddAsync(RefFormulaire entity)
    {
        await _context.RefFormulaires.AddAsync(entity);
    }

    public async Task<RefFormulaire?> GetFormulaireActifByRoleAsync(string role)
    {
        var roleTrimmed = role?.Trim();
        // Priorité : ACTIF d'abord, puis BROUILLON si pas encore activé
        return await _context.RefFormulaires
            .Where(f => (f.Role != null && f.Role.Trim() == roleTrimmed)
                     && (f.Statut != null && (f.Statut.Trim() == "ACTIF" || f.Statut.Trim() == "BROUILLON")))
            .OrderBy(f => f.Statut!.Trim() == "ACTIF" ? 0 : 1) // ACTIF en premier
            .ThenByDescending(f => f.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<RefFormulaire?> GetFormulaireActifByCodeReferenceAsync(string codeReference)
    {
        var codeRefTrimmed = codeReference?.Trim();
        // Priorité : ACTIF d'abord, puis BROUILLON si pas encore activé
        return await _context.RefFormulaires
            .Where(f => (f.CodeReference != null && f.CodeReference.Trim() == codeRefTrimmed)
                     && (f.Statut != null && (f.Statut.Trim() == "ACTIF" || f.Statut.Trim() == "BROUILLON")))
            .OrderBy(f => f.Statut!.Trim() == "ACTIF" ? 0 : 1) // ACTIF en premier
            .ThenByDescending(f => f.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<System.Collections.Generic.IEnumerable<RefFormulaire>> GetFormulairesByRoleAsync(string role)
    {
        var roleTrimmed = role?.Trim();
        var formulaires = await _context.RefFormulaires
            .AsNoTracking()
            .Where(f => (f.Role != null && f.Role.Trim() == roleTrimmed) && (f.Statut != null && (f.Statut.Trim() == "ACTIF" || f.Statut.Trim() == "BROUILLON")))
            .ToListAsync();

        // Une seule entrée par codeReference : priorité ACTIF, puis version la plus élevée
        return formulaires
            .GroupBy(f => f.CodeReference?.Trim() ?? string.Empty)
            .Select(g => g
                .OrderBy(f => f.Statut!.Trim() == "ACTIF" ? 0 : 1)
                .ThenByDescending(f => f.Version)
                .First())
            .OrderBy(f => f.Designation)
            .ToList();
    }

    public async Task<int> GetMaxVersionByCodeReferenceAsync(string codeReference)
    {
        var codeRefTrimmed = codeReference?.Trim();
        return await _context.RefFormulaires
            .Where(f => f.CodeReference != null && f.CodeReference.Trim() == codeRefTrimmed)
            .MaxAsync(f => (int?)f.Version) ?? 0;
    }
}
