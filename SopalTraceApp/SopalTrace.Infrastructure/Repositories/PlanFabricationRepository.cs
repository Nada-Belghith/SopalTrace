using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class PlanFabricationRepository : IPlanFabricationRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanFabricationRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExisteArticleSageAsync(string codeArticleSage)
    {
        return await _context.Itmmasters.AnyAsync(a => a.CodeArticle == codeArticleSage);
    }

    public async Task<string?> GetDesignationArticleSageAsync(string codeArticleSage)
    {
        return await _context.Itmmasters
            .Where(a => a.CodeArticle == codeArticleSage)
            .Select(a => a.Designation)
            .FirstOrDefaultAsync();
    }

    public async Task<Itmmaster?> GetArticleItmAsync(string codeArticleSage)
    {
        return await _context.Itmmasters
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CodeArticle == codeArticleSage);
    }

    public async Task<bool> IsOperationValidePourNatureAsync(string natureCode, string operationCode)
    {
        return await _context.NatureComposantOperations
            .AnyAsync(g => g.NatureComposantCode == natureCode && g.OperationCode == operationCode);
    }

    // Modèles
    public async Task<bool> ExisteModeleActifAsync(string natureCode, string? operationCode)
    {
        return await _context.ModeleFabEntetes
            .AnyAsync(m => m.NatureComposantCode == natureCode && m.OperationCode == operationCode && m.Statut == "ACTIF");
    }

    public async Task<IReadOnlyList<ModeleFabEntete>> GetModelesParFiltresAsync(string? natureCode, string? operationCode)
    {
        var query = _context.ModeleFabEntetes.AsQueryable();
        if (!string.IsNullOrEmpty(natureCode)) query = query.Where(m => m.NatureComposantCode == natureCode);
        if (!string.IsNullOrEmpty(operationCode)) query = query.Where(m => m.OperationCode == operationCode);
        return await query.ToListAsync();
    }

    public async Task<ModeleFabEntete?> GetModeleActifAvecRelationsAsync(Guid modeleId)
    {
        return await _context.ModeleFabEntetes
            .Include(m => m.NatureComposantCodeNavigation)
            .Include(m => m.OperationCodeNavigation)
            .Include(m => m.Formulaire)
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.TypeSection)
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.RegleEchantillonnage)
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.ModeleFabLignes)
                    .ThenInclude(l => l.TypeCaracteristique)
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.ModeleFabLignes)
                    .ThenInclude(l => l.TypeControle)
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.ModeleFabLignes)
                    .ThenInclude(l => l.MoyenControle)
            .FirstOrDefaultAsync(m => m.Id == modeleId && m.Statut == "ACTIF");
    }

    public async Task<ModeleFabEntete?> GetModeleAvecRelationsAsync(Guid modeleId)
    {
        return await _context.ModeleFabEntetes
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.TypeSection)
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.RegleEchantillonnage)
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.ModeleFabLignes)
            .FirstOrDefaultAsync(m => m.Id == modeleId);
    }

    public async Task<ModeleFabEntete?> GetModelePourArchivageAsync(Guid modeleId)
    {
        return await _context.ModeleFabEntetes.FirstOrDefaultAsync(m => m.Id == modeleId);
    }

    public async Task AddModeleAsync(ModeleFabEntete modele)
    {
        await _context.ModeleFabEntetes.AddAsync(modele);
    }

    public async Task<bool> ExisteModeleParCodeAsync(string code)
    {
        return await _context.ModeleFabEntetes.AnyAsync(m => m.Code == code);
    }

    public async Task<bool> ExisteModeleParCodeEtLibelleAsync(string code, string libelle)
    {
        return await _context.ModeleFabEntetes.AnyAsync(m => m.Code == code && m.Libelle == libelle);
    }

    public void DeleteModele(ModeleFabEntete modele)
    {
        _context.ModeleFabEntetes.Remove(modele);
    }

    public async Task<int> GetDerniereVersionModeleAsync(string? natureCode, string? operationCode)
    {
        return await _context.ModeleFabEntetes
            .Where(m => m.NatureComposantCode == natureCode && m.OperationCode == operationCode)
            .MaxAsync(m => (int?)m.Version) ?? -1;
    }

    public async Task<int> GetDerniereVersionModeleParCodeAsync(string code)
    {
        return await _context.ModeleFabEntetes
            .Where(m => m.Code == code)
            .MaxAsync(m => (int?)m.Version) ?? -1;
    }

    public async Task<ModeleFabEntete?> GetBrouillonModeleLePlusRecentAsync(string? natureCode, string? operationCode)
    {
        return await _context.ModeleFabEntetes
            .Where(m => m.NatureComposantCode == natureCode && m.OperationCode == operationCode && m.Statut == "BROUILLON")
            .OrderByDescending(m => m.Version)
            .FirstOrDefaultAsync();
    }

    // Plans
    public async Task<bool> ExistePlanActifPourArticleAsync(string codeArticleSage)
    {
        return await _context.PlanFabEntetes.AnyAsync(p => p.CodeArticleSage == codeArticleSage && p.Statut == "ACTIF");
    }

    public async Task<bool> ExistePlanActifPourArticleEtOperationAsync(string codeArticleSage, string? operationCode)
    {
        return await _context.PlanFabEntetes.AnyAsync(p => p.CodeArticleSage == codeArticleSage && p.OperationCode == operationCode && p.Statut == "ACTIF");
    }

    public async Task<PlanFabEntete?> GetPlanActifPourArticleAsync(string codeArticleSage)
    {
        return await _context.PlanFabEntetes.FirstOrDefaultAsync(p => p.CodeArticleSage == codeArticleSage && p.Statut == "ACTIF");
    }

    public async Task<PlanFabEntete?> GetPlanActifPourArticleEtOperationAsync(string codeArticleSage, string operationCode)
    {
        return await _context.PlanFabEntetes.FirstOrDefaultAsync(p => p.CodeArticleSage == codeArticleSage && p.OperationCode == operationCode && p.Statut == "ACTIF");
    }

    public async Task<PlanFabEntete?> GetBrouillonLePlusRecentAsync(string codeArticleSage, Guid? modeleSourceId, string? operationCode = null)
    {
        var query = _context.PlanFabEntetes.Where(p => p.CodeArticleSage == codeArticleSage && p.Statut == "BROUILLON");
        if (modeleSourceId.HasValue) query = query.Where(p => p.ModeleSourceId == modeleSourceId);
        if (!string.IsNullOrEmpty(operationCode)) query = query.Where(p => p.OperationCode == operationCode);
        return await query.OrderByDescending(p => p.CreeLe).FirstOrDefaultAsync();
    }

    public async Task<PlanFabEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanFabEntetes
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.PlanFabLignes)
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.RegleEchantillonnage)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<PlanFabEntete?> GetPlanCompletPourMiseAJourAsync(Guid planId)
    {
        return await _context.PlanFabEntetes
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.PlanFabLignes)
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.RegleEchantillonnage)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<List<PlanFabLigne>> GetLignesDuPlanAsync(Guid planId)
    {
        return await _context.PlanFabLignes
            .Where(l => l.PlanEnteteId == planId)
            .ToListAsync();
    }

    public async Task<PlanFabEntete?> GetPlanByIdAsync(Guid planId)
    {
        return await _context.PlanFabEntetes.FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<IReadOnlyList<PlanFabEntete>> GetPlansParFiltresAsync(string? natureCode, string? operationCode)
    {
        var query = _context.PlanFabEntetes.AsQueryable();

        if (!string.IsNullOrEmpty(natureCode))
        {
            query = from p in query
                    join a in _context.Itmmasters on p.CodeArticleSage equals a.CodeArticle
                    where a.NatureComposantCode == natureCode
                    select p;
        }

        if (!string.IsNullOrEmpty(operationCode))
        {
            query = query.Where(p => p.OperationCode == operationCode);
        }

        return await query.ToListAsync();
    }

    public void Delete(PlanFabEntete plan)
    {
        _context.PlanFabEntetes.Remove(plan);
    }

    public void DeleteSection(PlanFabSection section)
    {
        _context.PlanFabSections.Remove(section);
    }

    public void DeleteLigne(PlanFabLigne ligne)
    {
        _context.PlanFabLignes.Remove(ligne);
    }

    public async Task AddPlanAsync(PlanFabEntete plan)
    {
        await _context.PlanFabEntetes.AddAsync(plan);
    }

    public async Task AddPlanSectionAsync(PlanFabSection section)
    {
        await _context.PlanFabSections.AddAsync(section);
    }

    public async Task AddPlanLigneAsync(PlanFabLigne ligne)
    {
        await _context.PlanFabLignes.AddAsync(ligne);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetDerniereVersionPlanAsync(string codeArticleSage, string? operationCode = null)
    {
        return await _context.PlanFabEntetes
            .Where(p => p.CodeArticleSage == codeArticleSage && p.OperationCode == operationCode)
            .MaxAsync(p => (int?)p.Version) ?? -1;
    }

    public async Task<ModeleFabEntete?> GetModeleActifParCriteresAsync(string natureCode, string operationCode)
    {
        return await _context.ModeleFabEntetes
            .FirstOrDefaultAsync(m => m.NatureComposantCode == natureCode && m.OperationCode == operationCode && m.Statut == "ACTIF");
    }

    public async Task<ModeleFabEntete?> GetModeleActifPourFamilleAsync(string? natureComposantCode, string? opCode)
    {
        return await _context.ModeleFabEntetes
            .FirstOrDefaultAsync(m => m.NatureComposantCode == natureComposantCode && m.OperationCode == opCode && m.Statut == "ACTIF");
    }

    public async Task DeletePlanWithChildrenAsync(Guid planId)
    {
        var plan = await _context.PlanFabEntetes
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.PlanFabLignes)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan != null)
        {
            _context.PlanFabEntetes.Remove(plan);
        }
    }
}
