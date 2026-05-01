using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

public class DictionnaireQualiteRepository : IDictionnaireQualiteRepository
{
    private readonly SopalTraceDbContext _context;

    public DictionnaireQualiteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<Periodicite> GetPeriodiciteByLibelleAsync(string libelle)
    {
        return await _context.Periodicites.FirstOrDefaultAsync(p => p.Libelle == libelle);
    }

    public async Task<TypeSection> GetTypeSectionByLibelleAsync(string libelle)
    {
        return await _context.Set<TypeSection>().FirstOrDefaultAsync(t => t.Libelle == libelle);
    }

    public async Task AddTypeSectionAsync(TypeSection entite)
    {
        _context.Set<TypeSection>().Add(entite);
        await Task.CompletedTask;
    }

    public async Task<TypeCaracteristique> GetTypeCaracteristiqueByLibelleAsync(string libelle)
    {
        return await _context.Set<TypeCaracteristique>().FirstOrDefaultAsync(t => t.Libelle == libelle);
    }

    public async Task AddTypeCaracteristiqueAsync(TypeCaracteristique entite)
    {
        _context.Set<TypeCaracteristique>().Add(entite);
        await Task.CompletedTask;
    }

    public async Task<TypeControle> GetTypeControleByLibelleAsync(string libelle)
    {
        return await _context.Set<TypeControle>().FirstOrDefaultAsync(t => t.Libelle == libelle);
    }

    public async Task AddTypeControleAsync(TypeControle entite)
    {
        _context.Set<TypeControle>().Add(entite);
        await Task.CompletedTask;
    }

    public async Task<MoyenControle> GetMoyenControleByLibelleAsync(string libelle)
    {
        return await _context.Set<MoyenControle>().FirstOrDefaultAsync(t => t.Libelle == libelle);
    }

    public async Task AddMoyenControleAsync(MoyenControle entite)
    {
        _context.Set<MoyenControle>().Add(entite);
        await Task.CompletedTask;
    }
}
