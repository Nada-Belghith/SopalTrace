using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IModeleFabricationService
{
    Task<IReadOnlyList<ModeleResponseDto>> GetModelesByFiltersAsync(string? typeRobinetCode, string? natureComposantCode, string? operationCode);
    Task<Guid> CreerModeleAsync(CreateModeleRequestDto request);
    Task<ModeleResponseDto> GetModeleByIdAsync(Guid modeleId);
    Task<Guid> CreerNouvelleVersionModeleAsync(NouvelleVersionModeleRequestDto request);
    Task<Guid> RestaurerModeleArchiveAsync(RestaurerModeleRequestDto request);
}
