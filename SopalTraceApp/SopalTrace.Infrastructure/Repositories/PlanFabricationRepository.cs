using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Constants;
using SopalTrace.Infrastructure;
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

    public async Task<bool> ExisteModeleActifAsync(string typeRobinetCode, string natureCode, string operationCode)
    {
        return await _context.ModeleFabEntetes.AnyAsync(m =>
            m.TypeRobinetCode == typeRobinetCode &&
            m.NatureComposantCode == natureCode &&
            m.OperationCode == operationCode &&
            m.Statut == StatutsPlan.Actif);
    }

    public async Task<ModeleFabEntete?> GetModeleActifAvecRelationsAsync(Guid modeleId)
    {
        return await _context.ModeleFabEntetes
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.ModeleFabLignes)
            .Include(m => m.NatureComposantCodeNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modeleId && m.Statut == StatutsPlan.Actif);
    }

    public async Task<ModeleFabEntete?> GetModeleAvecRelationsAsync(Guid modeleId)
    {
        return await _context.ModeleFabEntetes
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.ModeleFabLignes)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modeleId);
    }

    public async Task AddModeleAsync(ModeleFabEntete modele)
    {
        await _context.ModeleFabEntetes.AddAsync(modele);
    }

    public async Task<bool> ExistePlanActifPourArticleAsync(string codeArticleSage)
    {
        return await _context.PlanFabEntetes.AnyAsync(p => p.CodeArticleSage == codeArticleSage && p.Statut == StatutsPlan.Actif);
    }

    public async Task<bool> ExistePlanActifPourArticleEtOperationAsync(string codeArticleSage, string? operationCode)
    {
        return await _context.PlanFabEntetes.AnyAsync(p =>
            p.CodeArticleSage == codeArticleSage
            && p.Statut == StatutsPlan.Actif
            && p.ModeleSource.OperationCode == operationCode);
    }

    public async Task<IReadOnlyList<ModeleFabEntete>> GetModelesParFiltresAsync(string? typeRobinetCode, string? natureCode, string? operationCode)
    {
        var query = _context.ModeleFabEntetes.Where(m => m.Statut == StatutsPlan.Actif);

        if (!string.IsNullOrWhiteSpace(typeRobinetCode))
            query = query.Where(m => m.TypeRobinetCode == typeRobinetCode);

        if (!string.IsNullOrWhiteSpace(natureCode))
            query = query.Where(m => m.NatureComposantCode == natureCode);

        if (!string.IsNullOrWhiteSpace(operationCode))
            query = query.Where(m => m.OperationCode == operationCode);

        return await query
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.ModeleFabLignes)
            .OrderByDescending(m => m.CreeLe)
            .ToListAsync();
    }

    public async Task<PlanFabEntete?> GetPlanActifPourArticleAsync(string codeArticleSage)
    {
        return await _context.PlanFabEntetes
            .FirstOrDefaultAsync(p => p.CodeArticleSage == codeArticleSage && p.Statut == StatutsPlan.Actif);
    }

    public async Task<PlanFabEntete?> GetPlanActifPourArticleEtOperationAsync(string codeArticleSage, string operationCode)
    {
        return await _context.PlanFabEntetes
            .FirstOrDefaultAsync(p => p.CodeArticleSage == codeArticleSage 
                                 && p.OperationCode == operationCode 
                                 && p.Statut == StatutsPlan.Actif);
    }

    // Méthode publique avec paramètre nullable Guid? — correspond à l'interface
    public async Task<PlanFabEntete?> GetBrouillonLePlusRecentAsync(string codeArticleSage, Guid? modeleSourceId, string? operationCode = null)
    {
        var query = _context.PlanFabEntetes
            .Where(p => p.CodeArticleSage == codeArticleSage
                        && p.ModeleSourceId == modeleSourceId
                        && p.Statut == StatutsPlan.Brouillon);

        if (!string.IsNullOrEmpty(operationCode))
        {
            query = query.Where(p => p.OperationCode == operationCode);
        }

        return await query
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync();
    }



    public async Task<PlanFabEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanFabEntetes
            .Include(p => p.ModeleSource)
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.PlanFabLignes)
                    .ThenInclude(l => l.TypeCaracteristique)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<PlanFabEntete?> GetPlanCompletPourMiseAJourAsync(Guid planId)
    {
        return await _context.PlanFabEntetes
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.PlanFabLignes)
                    .ThenInclude(l => l.TypeControle)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<List<PlanFabLigne>> GetLignesDuPlanAsync(Guid planId)
    {
        return await _context.PlanFabLignes
            .Include(l => l.Section)
            .Where(l => l.Section.PlanEnteteId == planId)
            .ToListAsync();
    }

    public async Task<PlanFabEntete?> GetPlanByIdAsync(Guid planId)
    {
        return await _context.PlanFabEntetes.FindAsync(planId);
    }

    public async Task AddPlanAsync(PlanFabEntete plan)
    {
        await _context.PlanFabEntetes.AddAsync(plan);
    }

    public async Task AddPlanSectionAsync(PlanFabSection section)
    {
        await _context.AddAsync(section);
    }

    public async Task AddPlanLigneAsync(PlanFabLigne ligne)
    {
        await _context.AddAsync(ligne);
    }

    /// <summary>
    /// Supprime un plan et toutes ses sections et lignes associées
    /// </summary>
    public async Task DeletePlanWithChildrenAsync(Guid planId)
    {
        var plan = await _context.PlanFabEntetes
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.PlanFabLignes)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan != null)
        {
            // Supprimer toutes les lignes et sections en cascade
            foreach (var section in plan.PlanFabSections.ToList())
            {
                _context.PlanFabLignes.RemoveRange(section.PlanFabLignes);
                _context.PlanFabSections.Remove(section);
            }

            _context.PlanFabEntetes.Remove(plan);
        }
    }

    // Conservez l'ancienne méthode pour compatibilité
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

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw ex.ToDomainExceptionOrSelf("Un enregistrement identique existe déjà pour ce plan/modèle de fabrication.");
        }
    }

    public async Task<bool> ExisteArticleSageAsync(string codeArticleSage)
    {
        return await _context.Itmmasters.AnyAsync(a => a.CodeArticle == codeArticleSage);
    }

    public async Task<string?> GetDesignationArticleSageAsync(string codeArticleSage)
    {
        var article = await _context.Itmmasters.FirstOrDefaultAsync(a => a.CodeArticle == codeArticleSage);
        return article?.Designation;
    }

    public async Task<ModeleFabEntete?> GetModelePourArchivageAsync(Guid modeleId)
    {
        return await _context.ModeleFabEntetes.FindAsync(modeleId);
    }

    public async Task<int> GetDerniereVersionModeleAsync(string typeRobinetCode, string natureCode, string operationCode)
    {
        return await _context.ModeleFabEntetes
            .Where(m => m.TypeRobinetCode == typeRobinetCode
                        && m.NatureComposantCode == natureCode
                        && m.OperationCode == operationCode
                        && (m.Statut == StatutsPlan.Brouillon || m.Statut == StatutsPlan.Actif || m.Statut == StatutsPlan.Archive))
            .Select(m => (int?)m.Version)
            .MaxAsync() ?? 0;
    }

    public async Task<int> GetDerniereVersionPlanAsync(string codeArticleSage, string? operationCode = null)
    {
        var query = _context.PlanFabEntetes
            .Where(p => p.CodeArticleSage == codeArticleSage
                && (p.Statut == StatutsPlan.Brouillon || p.Statut == StatutsPlan.Actif || p.Statut == StatutsPlan.Archive));
        
        if (!string.IsNullOrEmpty(operationCode))
        {
            query = query.Where(p => p.OperationCode == operationCode);
        }

        return await query
            .Select(p => (int?)p.Version)
            .MaxAsync() ?? 0;
    }

    public async Task<ModeleFabEntete?> GetModeleActifParCriteresAsync(string typeRobinetCode, string natureCode, string operationCode)
    {
        return await _context.ModeleFabEntetes.FirstOrDefaultAsync(m =>
            m.TypeRobinetCode == typeRobinetCode &&
            m.NatureComposantCode == natureCode &&
            m.OperationCode == operationCode &&
            m.Statut == StatutsPlan.Actif);
    }

    public async Task<ModeleFabEntete?> GetModeleActifPourFamilleAsync(string typeRobinetCode, string natureComposantCode, string opCode)
    {
        return await _context.ModeleFabEntetes
            .FirstOrDefaultAsync(m => m.TypeRobinetCode == typeRobinetCode
                               && m.NatureComposantCode == natureComposantCode
                               && m.OperationCode == opCode
                               && m.Statut == StatutsPlan.Actif);
    }

    public async Task<IReadOnlyList<PlanFabEntete>> GetPlansParFiltresAsync(string? typeRobinetCode, string? natureCode, string? operationCode)
    {
        // On part d'une requête de base qui joint le Plan avec les infos Article de SAGE (Itmmasters)
        // car un plan peut être "Vierge" (sans modèle source) mais il appartient toujours à un article
        // qui possède lui-même une Nature et un Type de Robinet.
        var query = _context.PlanFabEntetes
            .Join(_context.Itmmasters, 
                p => p.CodeArticleSage, 
                a => a.CodeArticle, 
                (p, a) => new { Plan = p, Article = a })
            .Where(x => x.Plan.Statut == StatutsPlan.Actif);

        // Filtre par Opération
        if (!string.IsNullOrWhiteSpace(operationCode))
        {
            query = query.Where(x => x.Plan.OperationCode == operationCode);
        }

        // Filtre par Type de Robinet (Depuis l'article ou le modèle)
        if (!string.IsNullOrWhiteSpace(typeRobinetCode))
        {
            query = query.Where(x => x.Article.TypeRobinetCode == typeRobinetCode);
        }

        // Filtre par Nature de Composant (Depuis l'article ou le modèle)
        if (!string.IsNullOrWhiteSpace(natureCode))
        {
            query = query.Where(x => x.Article.NatureComposantCode == natureCode);
        }

        var results = await query
            .Select(x => x.Plan)
            .Include(p => p.ModeleSource)
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.PlanFabLignes)
                    .ThenInclude(l => l.TypeCaracteristique)
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();

        return results;
    }
}