using SopalTrace.Application.DTOs.QualityPlans.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanFabricationService
{
    Task<DocumentEnteteDto?> GetPlanByIdAsync(Guid id);
    Task<Guid> CreerPlanAsync(CreateDocumentRequestDto request);
    Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionDocumentRequestDto request);
    Task<bool> MettreAJourPlanAsync(Guid id, UpdateDocumentRequestDto request);
    Task<Guid> RestaurerPlanArchiveAsync(RestaurerDocumentRequestDto request);
    Task<IReadOnlyList<DocumentEnteteDto>> GetPlansByFiltersAsync(string? natureComposantCode = null, string? operationCode = null, string? familleProduitCode = null, string? statut = null, string? codeArticleSageVersionne = null);
    Task<bool> SupprimerPlanAsync(Guid id);
    Task ArchiverPlansByFormulaireAsync(Guid formulaireId);
}
