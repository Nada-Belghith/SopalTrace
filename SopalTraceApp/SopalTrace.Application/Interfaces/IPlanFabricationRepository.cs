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
    Task<bool> IsNatureGeneriqueAsync(string natureCode);

    // Modèles
    Task<bool> ExisteModeleActifAsync(string natureCode, string? operationCode);
    Task<IReadOnlyList<ModeleFabricationEntete>> GetModelesParFiltresAsync(string? natureCode, string? operationCode, string? typeRobinetCode = null, string? posteCode = null, string? familleProduitCode = null);
    Task<ModeleFabricationEntete?> GetModeleActifAvecRelationsAsync(Guid modeleId);
    Task<ModeleFabricationEntete?> GetModeleAvecRelationsAsync(Guid modeleId);
    Task<ModeleFabricationEntete?> GetModelePourArchivageAsync(Guid modeleId);
    Task AddModeleAsync(ModeleFabricationEntete modele);
    Task<bool> ExisteModeleParCodeAsync(string code);
    Task<bool> ExisteModeleParCodeEtLibelleAsync(string code, string libelle);
    void DeleteModele(ModeleFabricationEntete modele);

    Task<int> GetDerniereVersionModeleAsync(string? natureCode, string? operationCode); 
    Task<int> GetDerniereVersionModeleParCodeAsync(string code);
    Task<ModeleFabricationEntete?> GetBrouillonModeleLePlusRecentAsync(string? natureCode, string? operationCode); 

    // Plans
    Task<bool> ExistePlanActifPourArticleAsync(string codeArticleSage);
    Task<bool> ExistePlanActifPourArticleEtOperationAsync(string codeArticleSage, string? operationCode);
    Task<PlanFabricationEntete?> GetPlanActifPourArticleAsync(string codeArticleSage);
    Task<PlanFabricationEntete?> GetPlanActifPourArticleEtOperationAsync(string codeArticleSage, string operationCode);
    Task<PlanFabricationEntete?> GetBrouillonLePlusRecentAsync(string codeArticleSage, Guid? modeleSourceId, string? operationCode = null);
    Task<PlanFabricationEntete?> GetPlanAvecRelationsAsync(Guid planId);
    Task<PlanFabricationEntete?> GetPlanCompletPourMiseAJourAsync(Guid planId);
    Task<List<PlanFabricationLigne>> GetLignesDuPlanAsync(Guid planId);
    Task<PlanFabricationEntete?> GetPlanByIdAsync(Guid planId);
    
    Task<IReadOnlyList<PlanFabricationEntete>> GetPlansParFiltresAsync(string? natureCode, string? operationCode);

    void Delete(PlanFabricationEntete plan);
    void DeleteSection(PlanFabricationSection section);
    void DeleteLigne(PlanFabricationLigne ligne);

    Task AddPlanAsync(PlanFabricationEntete plan);

    Task AddPlanSectionAsync(PlanFabricationSection section);
    Task AddPlanLigneAsync(PlanFabricationLigne ligne);

    Task SaveChangesAsync();
    Task<int> GetDerniereVersionPlanAsync(string codeArticleSage, string? operationCode = null);
    Task<ModeleFabricationEntete?> GetModeleActifParCriteresAsync(string natureCode, string operationCode);
    Task<ModeleFabricationEntete?> GetModeleActifPourFamilleAsync(string? natureComposantCode, string? opCode, string? posteCode, string? familleProduitCode);
    Task<ModeleFabricationEntete?> GetModeleActifParCodeEtLibelleAsync(string code, string libelle);

    Task DeletePlanWithChildrenAsync(Guid planId);
}