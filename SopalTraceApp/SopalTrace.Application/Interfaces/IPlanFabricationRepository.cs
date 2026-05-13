using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanFabricationRepository
{
    Task<bool> ExisteArticleSageAsync(string codeArticleSage);
    Task<string?> GetDesignationArticleSageAsync(string codeArticleSage);
    Task<Itmmaster?> GetArticleItmAsync(string codeArticleSage);
    Task<bool> IsOperationValidePourNatureAsync(string natureCode, string operationCode);

    // Modèles
    Task<bool> ExisteModeleActifAsync(string natureCode, string? operationCode);
    Task<IReadOnlyList<ModeleFabEntete>> GetModelesParFiltresAsync(string? natureCode, string? operationCode);
    Task<ModeleFabEntete?> GetModeleActifAvecRelationsAsync(Guid modeleId);
    Task<ModeleFabEntete?> GetModeleAvecRelationsAsync(Guid modeleId);
    Task<ModeleFabEntete?> GetModelePourArchivageAsync(Guid modeleId);
    Task AddModeleAsync(ModeleFabEntete modele);
    Task<bool> ExisteModeleParCodeAsync(string code);
    Task<bool> ExisteModeleParCodeEtLibelleAsync(string code, string libelle);
    void DeleteModele(ModeleFabEntete modele);

    Task<int> GetDerniereVersionModeleAsync(string? natureCode, string? operationCode); 
    Task<int> GetDerniereVersionModeleParCodeAsync(string code);
    Task<ModeleFabEntete?> GetBrouillonModeleLePlusRecentAsync(string? natureCode, string? operationCode); 

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
    
    Task<IReadOnlyList<PlanFabEntete>> GetPlansParFiltresAsync(string? natureCode, string? operationCode);

    void Delete(PlanFabEntete plan);
    void DeleteSection(PlanFabSection section);
    void DeleteLigne(PlanFabLigne ligne);

    Task AddPlanAsync(PlanFabEntete plan);

    Task AddPlanSectionAsync(PlanFabSection section);
    Task AddPlanLigneAsync(PlanFabLigne ligne);

    Task SaveChangesAsync();
    Task<int> GetDerniereVersionPlanAsync(string codeArticleSage, string? operationCode = null);
    Task<ModeleFabEntete?> GetModeleActifParCriteresAsync(string natureCode, string operationCode);
    Task<ModeleFabEntete?> GetModeleActifPourFamilleAsync(string? natureComposantCode, string? opCode);

    Task DeletePlanWithChildrenAsync(Guid planId);
}