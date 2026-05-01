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
    Task<bool> ExisteModeleActifAsync(string typeRobinetCode, string natureCode, string? operationCode);
    Task<IReadOnlyList<ModeleFabEntete>> GetModelesParFiltresAsync(string? typeRobinetCode, string? natureCode, string? operationCode);
    Task<ModeleFabEntete?> GetModeleActifAvecRelationsAsync(Guid modeleId);
    Task<ModeleFabEntete?> GetModeleAvecRelationsAsync(Guid modeleId);
    Task<ModeleFabEntete?> GetModelePourArchivageAsync(Guid modeleId);
    Task AddModeleAsync(ModeleFabEntete modele);
    void DeleteModele(ModeleFabEntete modele);

    // --> AJOUTEZ CECI POUR SÉCURISER VOTRE CRÉATION V2
    Task<int> GetDerniereVersionModeleAsync(string? typeRobinetCode, string? natureCode, string? operationCode); 
    Task<ModeleFabEntete?> GetBrouillonModeleLePlusRecentAsync(string? typeRobinetCode, string? natureCode, string? operationCode); 

    // Plans
    Task<bool> ExistePlanActifPourArticleAsync(string codeArticleSage);
    Task<bool> ExistePlanActifPourArticleEtOperationAsync(string codeArticleSage, string? operationCode);
    Task<PlanFabEntete?> GetPlanActifPourArticleAsync(string codeArticleSage);
    Task<PlanFabEntete?> GetPlanActifPourArticleEtOperationAsync(string codeArticleSage, string operationCode);
    Task<PlanFabEntete?> GetBrouillonLePlusRecentAsync(string codeArticleSage, Guid? modeleSourceId, string? operationCode = null);
    Task<PlanFabEntete?> GetPlanAvecRelationsAsync(Guid planId);
    Task<PlanFabEntete?> GetPlanCompletPourMiseAJourAsync(Guid planId);
    Task<List<PlanFabLigne>> GetLignesDuPlanAsync(Guid planId);
    Task<PlanFabEntete?> GetPlanByIdAsync(Guid planId);
    
    // --> AJOUT : Méthode nécessaire pour l'UI de clonage
    Task<IReadOnlyList<PlanFabEntete>> GetPlansParFiltresAsync(string? typeRobinetCode, string? natureCode, string? operationCode);

    void Delete(PlanFabEntete plan);
    void DeleteSection(PlanFabSection section);
    void DeleteLigne(PlanFabLigne ligne);

    Task AddPlanAsync(PlanFabEntete plan);

    // --- NOUVEAU : Méthodes explicites pour forcer les INSERTS ---
    Task AddPlanSectionAsync(PlanFabSection section);
    Task AddPlanLigneAsync(PlanFabLigne ligne);

    // Unité de travail
    Task SaveChangesAsync();
    Task<int> GetDerniereVersionPlanAsync(string codeArticleSage, string? operationCode = null);
    Task<ModeleFabEntete?> GetModeleActifParCriteresAsync(string typeRobinetCode, string natureCode, string operationCode);
    Task<ModeleFabEntete?> GetModeleActifPourFamilleAsync(string? typeRobinetCode, string? natureComposantCode, string? opCode);

    /// <summary>
    /// Supprime un plan et toutes ses sections et lignes en cascade
    /// </summary>
    Task DeletePlanWithChildrenAsync(Guid planId);
}