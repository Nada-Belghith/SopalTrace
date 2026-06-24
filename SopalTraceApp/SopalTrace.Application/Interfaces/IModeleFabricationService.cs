using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IModeleFabricationService
{
    Task<ModeleResponseDto?> GetModeleByIdAsync(Guid id);
    Task<Guid> CreerModeleAsync(CreateModeleRequestDto request);
    Task<Guid> CreerNouvelleVersionModeleAsync(NouvelleVersionModeleRequestDto request);
    Task<bool> MettreAJourModeleAsync(Guid id, CreateModeleRequestDto request);
    Task<Guid> RestaurerModeleArchiveAsync(RestaurerModeleRequestDto request);
    Task<IReadOnlyList<ModeleResponseDto>> GetModelesByFiltersAsync(string? natureComposantCode = null, string? operationCode = null, string? familleProduitCode = null, string? statut = null);
    Task<bool> SupprimerModeleAsync(Guid id);
    Task ArchiverModelesByFormulaireAsync(Guid formulaireId);
}
