using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

public class AlerteRepository : IAlerteRepository
{
    private readonly SopalTraceDbContext _context;

    public AlerteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<Alerte?> GetByIdAsync(Guid id)
    {
        return await _context.Alertes.FindAsync(id);
    }

    public async Task AddAsync(Alerte alerte)
    {
        await _context.Alertes.AddAsync(alerte);
    }

    public Task UpdateAsync(Alerte alerte)
    {
        _context.Alertes.Update(alerte);
        return Task.CompletedTask;
    }

    public async Task<bool> ExisteNonResolueDepuisAsync(string typeAlerte, string cleEntite, DateTime depuis)
    {
        return await _context.Alertes.AnyAsync(a => 
            a.TypeAlerte == typeAlerte && 
            a.CleEntite == cleEntite && 
            a.DateAlerte >= depuis && 
            !a.EstResolu);
    }

    public async Task<System.Collections.Generic.List<Alerte>> GetNonResoluesParArticleAsync(string articleCode)
    {
        // La clé contient souvent l'article à la fin, ex: "TRONC_TRN26_n-25B0A01"
        return await _context.Alertes
            .Where(a => !a.EstResolu && a.TypeAlerte == "PLAN_MANQUANT" && a.CleEntite.EndsWith(articleCode))
            .ToListAsync();
    }

    public async Task ResoudreAlertesPourEntiteAsync(string cleEntite, string resoluPar)
    {
        var alertes = await _context.Alertes
            .Where(a => a.CleEntite == cleEntite && !a.EstResolu)
            .ToListAsync();

        foreach (var alerte in alertes)
        {
            alerte.EstResolu = true;
            alerte.DateResolution = DateTime.UtcNow;
        }
    }
}
