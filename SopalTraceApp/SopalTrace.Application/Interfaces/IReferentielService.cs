using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IReferentielService
{
    Task<ReferentielsResponseDto> GetFabricationReferentielsAsync();

    /// <summary>Retourne les machines actives et les périodicités pour le module Vérif Machine.</summary>
    Task<VerifMachineReferentielsDto> GetVerifMachineReferentielsAsync();
    
    Task<PlanNcReferentielsDto> GetPlanNcReferentielsAsync();

    Task<ArticleDto?> GetArticleInfosAsync(string codeArticle);

    Task<Guid> CreatePeriodiciteAsync(CreatePeriodiciteDto request);

    Task<Guid> CreateCaracteristiqueAsync(CreateCaracteristiqueDto request);
}