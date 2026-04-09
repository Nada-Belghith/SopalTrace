using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanFabricationRepository
{
    Task<bool> ExisteArticleSageAsync(string codeArticleSage);

    // AJOUT DU '?' APRÈS string
    Task<string?> GetDesignationArticleSageAsync(string codeArticleSage);

    // Modèles
    Task<bool> ExisteModeleActifAsync(string typeRobinetCode, string natureCode, string operationCode);
    Task<ModeleFabEntete?> GetModeleActifAvecRelationsAsync(Guid modeleId);
    Task<ModeleFabEntete?> GetModeleAvecRelationsAsync(Guid modeleId);
    Task AddModeleAsync(ModeleFabEntete modele);

    // Plans
    Task<bool> ExistePlanActifPourArticleAsync(string codeArticleSage);
    Task<PlanFabEntete?> GetPlanActifPourArticleAsync(string codeArticleSage); // <-- NOUVELLE MÉTHODE
    Task<PlanFabEntete?> GetPlanAvecRelationsAsync(Guid planId);
    Task<PlanFabEntete?> GetPlanCompletPourMiseAJourAsync(Guid planId);
    Task<List<PlanFabLigne>> GetLignesDuPlanAsync(Guid planId);
    Task<PlanFabEntete?> GetPlanByIdAsync(Guid planId);

    Task AddPlanAsync(PlanFabEntete plan);

    // --- NOUVEAU : Méthodes explicites pour forcer les INSERTS ---
    Task AddPlanSectionAsync(PlanFabSection section);
    Task AddPlanLigneAsync(PlanFabLigne ligne);

    // Unité de travail
    Task SaveChangesAsync();
}