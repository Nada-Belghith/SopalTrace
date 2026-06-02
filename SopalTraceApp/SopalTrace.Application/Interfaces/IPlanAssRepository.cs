using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanAssRepository
{
    Task<string?> GetDesignationArticleSageAsync(string codeArticleSage);
    
    // Versions
    Task<int> GetDerniereVersionAsync(string operationCode, string? familleCode, string? codeArticleSage);
    Task<int> GetDerniereVersionParCodeAsync(string code);

    // Existence
    Task<bool> ExistePlanMaitreActifAsync(string operationCode, string? familleCode, string? natureComposantCode, string? posteCode);
    Task<bool> ExisteExceptionActiveAsync(string opCode, string? familleCode, string? natureComposantCode, string? posteCode, string articleCode);
    Task<bool> IsOperationValidePourNatureAsync(string natureCode, string operationCode);
    Task<bool> ExisteParCodeAsync(string code);
    Task<bool> ExisteParCodeEtLibelleAsync(string code, string libelle);

    // Récupération
    Task<PlanAssemblageEntete?> GetPlanAvecRelationsAsync(Guid planId);
    Task<PlanAssemblageEntete?> GetPlanActifMaitreAsync(string operationCode, string? familleCode, string? natureComposantCode, string? posteCode);
    Task<PlanAssemblageEntete?> GetPlanActifExceptionAsync(string operationCode, string? familleCode, string codeArticleSage);
    Task<PlanAssemblageEntete?> GetPlanByIdAsync(Guid planId);
    Task<List<PlanAssemblageEntete>> GetPlansActifsAsync(string operationCode, string? familleCode, string? codeArticleSage);
    Task<IReadOnlyList<PlanAssemblageEntete>> GetModelesParFiltresAsync(string? natureComposantCode, string? operationCode, string? posteCode = null, string? familleProduitCode = null);

    // Persistance
    Task AddPlanAsync(PlanAssemblageEntete plan);
    Task SaveChangesAsync();
}
