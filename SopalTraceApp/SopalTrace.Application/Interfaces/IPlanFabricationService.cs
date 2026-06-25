using SopalTrace.Application.DTOs.QualityPlans.Fabrication;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanFabricationService
{
    Task<PlanFabricationEnteteDto?> GetPlanByIdAsync(Guid id);
    Task<Guid> CreerPlanAsync(CreatePlanFabricationRequestDto request);
    Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionPlanFabricationRequestDto request);
    Task<bool> MettreAJourPlanAsync(Guid id, UpdatePlanFabricationRequestDto request);
    Task<Guid> RestaurerPlanArchiveAsync(RestaurerDocumentRequestDto request);
    Task<IReadOnlyList<PlanFabricationEnteteDto>> GetPlansByFiltersAsync(string? natureComposantCode = null, string? operationCode = null, string? familleProduitCode = null, string? statut = null, string? codeArticleSageVersionne = null);
    Task<bool> SupprimerPlanAsync(Guid id);
    Task ArchiverPlansByFormulaireAsync(Guid formulaireId);
}
