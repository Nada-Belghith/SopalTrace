using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IModeleFabricationService
{
    Task<ModeleResponseDto?> GetModelByIdAsync(Guid id);
    Task<Guid> CreateModelAsync(CreateModeleRequestDto request);
    Task<Guid> CreateNewVersionAsync(NouvelleVersionModeleRequestDto request);
    Task<bool> UpdateModelAsync(Guid id, CreateModeleRequestDto request);
    Task<IReadOnlyList<ModeleResponseDto>> GetModelsByFiltersAsync(string? natureComposantCode = null, string? operationCode = null, string? familleProduitCode = null, string? statut = null);
    Task ArchiveModelsByFormulaireAsync(Guid formulaireId);
}
