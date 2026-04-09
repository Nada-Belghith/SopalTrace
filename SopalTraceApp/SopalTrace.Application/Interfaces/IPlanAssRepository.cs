using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanAssRepository
{
    // Bien vérifier les '?' ici, ils indiquent qu'on autorise les retours null
    Task<string?> GetDesignationArticleSageAsync(string codeArticleSage);

    Task<int> GetDerniereVersionAsync(string operationCode, string typeRobinetCode, string? codeArticleSage);

    Task<bool> ExistePlanMaitreActifAsync(string operationCode, string typeRobinetCode);
    Task<bool> ExisteExceptionActiveAsync(string operationCode, string typeRobinetCode, string codeArticleSage);

    Task<PlanAssEntete?> GetPlanAvecRelationsAsync(Guid planId);
    Task<PlanAssEntete?> GetPlanActifMaitreAsync(string operationCode, string typeRobinetCode);
    Task<PlanAssEntete?> GetPlanActifExceptionAsync(string operationCode, string typeRobinetCode, string codeArticleSage);
    Task<PlanAssEntete?> GetPlanByIdAsync(Guid planId);

    Task AddPlanAsync(PlanAssEntete plan);
    Task SaveChangesAsync();
}