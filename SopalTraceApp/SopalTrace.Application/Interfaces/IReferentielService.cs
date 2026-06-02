using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IReferentielService
{
    Task<ReferentielsResponseDto> GetFabricationReferentielsAsync(string? natureComposantCode = null, string? operationCode = null);

    /// <summary>Retourne les machines actives et les périodicités pour le module Vérif Machine.</summary>
    Task<VerifMachineReferentielsDto> GetVerifMachineReferentielsAsync();
    
    Task<PlanNcReferentielsDto> GetPlanNcReferentielsAsync();

    Task<ArticleDto?> GetArticleInfosAsync(string codeArticle);

    Task<Guid> CreatePeriodiciteAsync(CreatePeriodiciteDto request);

    Task<Guid> CreateCaracteristiqueAsync(CreateCaracteristiqueDto request);

    Task<PieceRefDto> CreatePieceReferenceAsync(CreatePieceReferenceDto request);

    Task<FormulaireStructureDto?> GetFormulaireByRoleAsync(string role);
    Task<IEnumerable<FormulaireReferenceItemDto>> GetFormulairesListByRoleAsync(string role);
    Task<bool> UpdateFormulaireStructureAsync(string role, string? configurationStructureJson);
}