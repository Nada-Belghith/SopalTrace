using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
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
            m.Statut == "ACTIF");
    }

    public async Task<ModeleFabEntete?> GetModeleActifAvecRelationsAsync(Guid modeleId)
    {
        return await _context.ModeleFabEntetes
            .Include(m => m.ModeleFabSections)
                .ThenInclude(s => s.ModeleFabLignes)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modeleId && m.Statut == "ACTIF");
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
        return await _context.PlanFabEntetes.AnyAsync(p =>
            p.CodeArticleSage == codeArticleSage &&
            p.Statut != "ARCHIVE");
    }

    // --- NOUVELLE MÉTHODE AJOUTÉE ---
    public async Task<PlanFabEntete?> GetPlanActifPourArticleAsync(string codeArticleSage)
    {
        // Récupère l'ancien plan (V1) qui tourne actuellement en production
        return await _context.PlanFabEntetes
            .FirstOrDefaultAsync(p => p.CodeArticleSage == codeArticleSage && p.Statut == "ACTIF");
    }

    public async Task<PlanFabEntete?> GetPlanAvecRelationsAsync(Guid planId)
    {
        return await _context.PlanFabEntetes
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.PlanFabLignes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<PlanFabEntete?> GetPlanCompletPourMiseAJourAsync(Guid planId)
    {
        return await _context.PlanFabEntetes
            .Include(p => p.PlanFabSections)
                .ThenInclude(s => s.PlanFabLignes)
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

    // --- NOUVEAU : On utilise directement l'objet Context pour forcer l'état Added ---
    public async Task AddPlanSectionAsync(PlanFabSection section)
    {
        await _context.AddAsync(section);
    }

    public async Task AddPlanLigneAsync(PlanFabLigne ligne)
    {
        await _context.AddAsync(ligne);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExisteArticleSageAsync(string codeArticleSage)
    {
        return await _context.Itmmasters.AnyAsync(a => a.CodeArticle == codeArticleSage);
    }

    // AJOUT DU '?' APRÈS string
    public async Task<string?> GetDesignationArticleSageAsync(string codeArticleSage)
    {
        var article = await _context.Itmmasters.FirstOrDefaultAsync(a => a.CodeArticle == codeArticleSage);
        return article?.Designation;
    }
}