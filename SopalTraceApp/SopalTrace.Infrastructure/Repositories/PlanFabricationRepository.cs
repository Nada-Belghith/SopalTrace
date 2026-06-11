using SopalTrace.Domain.Constants;
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
        return await _context.ProduitFinis.AnyAsync(a => a.CodeArticle == codeArticleSage);
    }

    public async Task<string?> GetDesignationArticleSageAsync(string codeArticleSage)
    {
        return await _context.Articles
            .Where(a => a.CodeArticle == codeArticleSage)
            .Select(a => a.Designation)
            .FirstOrDefaultAsync();
    }

    public async Task<Itmmaster?> GetArticleItmAsync(string codeArticleSage)
    {
        var data = await _context.Articles
            .AsNoTracking()
            .Include(a => a.ProduitFini)
            .FirstOrDefaultAsync(a => a.CodeArticle == codeArticleSage);

        if (data == null) return null;

        return new Itmmaster
        {
            CodeArticle = data.CodeArticle,
            Designation = data.Designation,
            NatureComposantCode = data.NatureArticleCode,
            TypeRobinetCode = data.ProduitFini?.TypeRobinetCode,
            FamilleProduitFini = data.ProduitFini?.FamilleProduitFiniCode
        };
    }



    public async Task<bool> IsOperationValidePourNatureAsync(string natureCode, string operationCode)
    {
        // ✅ Vérification dans la table de gamme opératoire (NatureArticle_Operation)
        // et NON dans les modèles existants, pour permettre la création du premier modèle.
        return await _context.NatureArticleOperations
            .AnyAsync(g => g.NatureArticleCode == natureCode && g.OperationCode == operationCode);
    }

    public Task<bool> IsNatureGeneriqueAsync(string natureCode)
    {
        return Task.FromResult(natureCode == "MATIERE" || natureCode == "SEMI_FINI");
    }

    // Modèles
    public async Task<bool> ExisteModeleActifAsync(string natureCode, string? operationCode)
    {
        return await _context.ModeleFabricationEntetes
            .AnyAsync(m => m.NatureArticleCode == natureCode && m.OperationCode == operationCode && m.Statut == StatutsPlan.Actif);
    }

    public async Task<IReadOnlyList<ModeleFabricationEntete>> GetModelesParFiltresAsync(string? natureCode, string? operationCode, string? typeRobinetCode = null, string? posteCode = null, string? familleProduitCode = null)
    {
        var query = _context.ModeleFabricationEntetes
            .Include(m => m.Formulaire)
            .Include(m => m.FamilleProduitFiniCodeNavigation)
            .Where(m => m.Statut == StatutsPlan.Actif)
            .AsQueryable();

        if (!string.IsNullOrEmpty(natureCode))
            query = query.Where(m => m.NatureArticleCode == natureCode);
        if (!string.IsNullOrEmpty(operationCode))
            query = query.Where(m => m.OperationCode == operationCode);

        if (!string.IsNullOrEmpty(familleProduitCode))
            query = query.Where(m => m.FamilleProduitFiniCode == familleProduitCode);

        // Filtre par typeRobinetCode via la famille du modèle
        if (!string.IsNullOrEmpty(typeRobinetCode))
            query = query.Where(m =>
                m.FamilleProduitFiniCode == null ||
                m.FamilleProduitFiniCodeNavigation!.TypeRobinetCode == typeRobinetCode);

        return await query.ToListAsync();
    }

    public async Task<ModeleFabricationEntete?> GetModeleActifAvecRelationsAsync(Guid modeleId)
    {
        return await _context.ModeleFabricationEntetes
            .Include(m => m.NatureArticleCodeNavigation)
            .Include(m => m.OperationCodeNavigation)
            .Include(m => m.Formulaire)
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.TypeSection)
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.RegleEchantillonnage)
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.Periodicite)
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.ModeleFabricationLignes)
                    .ThenInclude(l => l.TypeCaracteristique)
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.ModeleFabricationLignes)
                    .ThenInclude(l => l.TypeControle)
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.ModeleFabricationLignes)
                    .ThenInclude(l => l.MoyenControle)
            .FirstOrDefaultAsync(m => m.Id == modeleId && m.Statut == StatutsPlan.Actif);
    }

    public async Task<ModeleFabricationEntete?> GetModeleAvecRelationsAsync(Guid modeleId)
    {
        return await _context.ModeleFabricationEntetes
            .Include(m => m.Formulaire)
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.TypeSection)
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.RegleEchantillonnage)
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.Periodicite)
            .Include(m => m.ModeleFabricationSections)
                .ThenInclude(s => s.ModeleFabricationLignes)
            .FirstOrDefaultAsync(m => m.Id == modeleId);
    }

    public async Task<ModeleFabricationEntete?> GetModelePourArchivageAsync(Guid modeleId)
    {
        return await _context.ModeleFabricationEntetes.FirstOrDefaultAsync(m => m.Id == modeleId);
    }

    public async Task AddModeleAsync(ModeleFabricationEntete modele)
    {
        await _context.ModeleFabricationEntetes.AddAsync(modele);
    }

    public async Task<bool> ExisteModeleParCodeAsync(string code)
    {
        return await _context.ModeleFabricationEntetes.AnyAsync(m => m.Code == code);
    }

    public async Task<bool> ExisteModeleParCodeEtLibelleAsync(string code, string libelle)
    {
        return await _context.ModeleFabricationEntetes.AnyAsync(m => m.Code == code && m.Libelle == libelle);
    }

    public void DeleteModele(ModeleFabricationEntete modele)
    {
        _context.ModeleFabricationEntetes.Remove(modele);
    }

    public async Task<int> GetDerniereVersionModeleAsync(string? natureCode, string? operationCode)
    {
        return await _context.ModeleFabricationEntetes
            .Where(m => m.NatureArticleCode == natureCode && m.OperationCode == operationCode)
            .MaxAsync(m => (int?)m.Version) ?? -1;
    }

    public async Task<int> GetDerniereVersionModeleParCodeAsync(string code)
    {
        return await _context.ModeleFabricationEntetes
            .Where(m => m.Code == code)
            .MaxAsync(m => (int?)m.Version) ?? -1;
    }

    public async Task<ModeleFabricationEntete?> GetBrouillonModeleLePlusRecentAsync(string? natureCode, string? operationCode)
    {
        return await _context.ModeleFabricationEntetes
            .Where(m => m.NatureArticleCode == natureCode && m.OperationCode == operationCode && m.Statut == StatutsPlan.Brouillon)
            .OrderByDescending(m => m.Version)
            .FirstOrDefaultAsync();
    }

    // Plans
    public async Task<bool> ExistePlanActifPourArticleAsync(string codeArticleSage)
    {
        return await _context.PlanFabricationEntetes.AnyAsync(p => p.CodeArticleSageVersionne == codeArticleSage && p.Statut == StatutsPlan.Actif);
    }

    public async Task<bool> ExistePlanActifPourArticleEtOperationAsync(string codeArticleSage, string? operationCode)
    {
        return await _context.PlanFabricationEntetes.AnyAsync(p => p.CodeArticleSageVersionne == codeArticleSage && p.OperationCode == operationCode && p.Statut == StatutsPlan.Actif);
    }

    public async Task<PlanFabricationEntete?> GetPlanActifPourArticleAsync(string codeArticleSage)
    {
        return await _context.PlanFabricationEntetes.FirstOrDefaultAsync(p => p.CodeArticleSageVersionne == codeArticleSage && p.Statut == StatutsPlan.Actif);
    }

    public async Task<PlanFabricationEntete?> GetPlanActifPourArticleEtOperationAsync(string codeArticleSage, string operationCode)
    {
        return await _context.PlanFabricationEntetes.FirstOrDefaultAsync(p => p.CodeArticleSageVersionne == codeArticleSage && p.OperationCode == operationCode && p.Statut == StatutsPlan.Actif);
    }

    public async Task<List<PlanFabricationEntete>> GetPlansActifsPourBaseArticleEtOperationAsync(string baseCode, string operationCode)
    {
        return await _context.PlanFabricationEntetes
            .Where(p => (p.CodeArticleSageVersionne == baseCode || p.CodeArticleSageVersionne.StartsWith(baseCode + ".")) 
                        && p.OperationCode == operationCode 
                        && p.Statut == StatutsPlan.Actif)
            .ToListAsync();
    }

    public async Task<PlanFabricationEntete?> GetBrouillonLePlusRecentAsync(string codeArticleSage, Guid? modeleSourceId, string? operationCode = null)
    {
        var query = _context.PlanFabricationEntetes.Where(p => p.CodeArticleSageVersionne == codeArticleSage && p.Statut == StatutsPlan.Brouillon);
        if (modeleSourceId.HasValue) query = query.Where(p => p.ModeleSourceId == modeleSourceId);
        if (!string.IsNullOrEmpty(operationCode)) query = query.Where(p => p.OperationCode == operationCode);
        return await query.OrderByDescending(p => p.CreeLe).FirstOrDefaultAsync();
    }

    public async Task<PlanFabricationEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanFabricationEntetes
            .Include(p => p.Formulaire)
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.PlanFabricationLignes)
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.RegleEchantillonnage)
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.Periodicite)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<PlanFabricationEntete?> GetPlanCompletPourMiseAJourAsync(Guid planId)
    {
        return await _context.PlanFabricationEntetes
            .Include(p => p.Formulaire)
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.PlanFabricationLignes)
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.RegleEchantillonnage)
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.Periodicite)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<List<PlanFabricationLigne>> GetLignesDuPlanAsync(Guid planId)
    {
        return await _context.PlanFabricationLignes
            .Where(l => l.PlanEnteteId == planId)
            .ToListAsync();
    }

    public async Task<PlanFabricationEntete?> GetPlanByIdAsync(Guid planId)
    {
        return await _context.PlanFabricationEntetes.FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<IReadOnlyList<PlanFabricationEntete>> GetPlansParFiltresAsync(string? natureCode, string? operationCode)
    {
        var query = _context.PlanFabricationEntetes
            .Include(p => p.Formulaire)
            .AsQueryable();

        if (!string.IsNullOrEmpty(natureCode))
        {
            query = from p in query
                    join a in _context.Articles on p.CodeArticleSageVersionne equals a.CodeArticle
                    where a.NatureArticleCode == natureCode
                    select p;
        }

        if (!string.IsNullOrEmpty(operationCode))
        {
            query = query.Where(p => p.OperationCode == operationCode);
        }

        return await query.ToListAsync();
    }

    public void Delete(PlanFabricationEntete plan)
    {
        _context.PlanFabricationEntetes.Remove(plan);
    }

    public void DeleteSection(PlanFabricationSection section)
    {
        _context.PlanFabricationSections.Remove(section);
    }

    public void DeleteLigne(PlanFabricationLigne ligne)
    {
        _context.PlanFabricationLignes.Remove(ligne);
    }

    public async Task AddPlanAsync(PlanFabricationEntete plan)
    {
        await _context.PlanFabricationEntetes.AddAsync(plan);
    }

    public async Task AddPlanSectionAsync(PlanFabricationSection section)
    {
        await _context.PlanFabricationSections.AddAsync(section);
    }

    public async Task AddPlanLigneAsync(PlanFabricationLigne ligne)
    {
        await _context.PlanFabricationLignes.AddAsync(ligne);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetDerniereVersionPlanAsync(string codeArticleSage, string? operationCode = null)
    {
        return await _context.PlanFabricationEntetes
            .Where(p => p.CodeArticleSageVersionne == codeArticleSage && p.OperationCode == operationCode)
            .MaxAsync(p => (int?)p.Version) ?? -1;
    }

    public async Task<ModeleFabricationEntete?> GetModeleActifParCriteresAsync(string natureCode, string operationCode)
    {
        return await _context.ModeleFabricationEntetes
            .FirstOrDefaultAsync(m => m.NatureArticleCode == natureCode && m.OperationCode == operationCode && m.Statut == StatutsPlan.Actif);
    }

    public async Task<ModeleFabricationEntete?> GetModeleActifPourFamilleAsync(string? natureComposantCode, string? opCode, string? posteCode, string? familleProduitCode)
    {
        return await _context.ModeleFabricationEntetes
            .FirstOrDefaultAsync(m => m.NatureArticleCode == natureComposantCode && 
                                      m.OperationCode == opCode && 
                                      m.FamilleProduitFiniCode == familleProduitCode &&
                                      m.Statut == StatutsPlan.Actif);
    }

    public async Task<ModeleFabricationEntete?> GetModeleActifParCodeEtLibelleAsync(string code, string libelle)
    {
        return await _context.ModeleFabricationEntetes
            .FirstOrDefaultAsync(m => m.Code == code && m.Libelle == libelle && m.Statut == StatutsPlan.Actif);
    }

    public async Task DeletePlanWithChildrenAsync(Guid planId)
    {
        var plan = await _context.PlanFabricationEntetes
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.PlanFabricationLignes)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan != null)
        {
            _context.PlanFabricationEntetes.Remove(plan);
        }
    }
}
