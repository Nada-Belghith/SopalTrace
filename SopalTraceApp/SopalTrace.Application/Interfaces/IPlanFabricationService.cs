using SopalTrace.Application.DTOs.QualityPlans.Fabrication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanFabricationService
{
    Task<PlanFabricationEnteteDto?> GetPlanByIdAsync(Guid id);
    Task<Guid> CreatePlanAsync(CreatePlanFabricationRequestDto request);
    Task<Guid> CreateNewVersionAsync(NouvelleVersionPlanFabricationRequestDto request);
    Task<bool> UpdatePlanAsync(Guid id, UpdatePlanFabricationRequestDto request);
    Task<Guid> UpgradeArchivedPlanAsync(Guid archiveId);
    Task<IReadOnlyList<PlanFabricationEnteteDto>> GetPlansByFiltersAsync(string? natureComposantCode = null, string? operationCode = null, string? familleProduitCode = null, string? statut = null, string? codeArticleSageVersionne = null);
    Task<bool> DeletePlanAsync(Guid id);
    Task ArchiveDocumentsByFormulaireAsync(Guid formulaireId);
}
