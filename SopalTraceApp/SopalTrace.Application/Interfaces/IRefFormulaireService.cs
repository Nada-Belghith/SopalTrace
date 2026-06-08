using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IRefFormulaireService
{
    Task<RefFormulaireDto> GetByIdAsync(Guid id);
    Task<bool> UpdateConfigurationAsync(Guid id, UpdateRefFormulaireDto dto);
    Task<Guid> NouvelleVersionAsync(NouvelleVersionRefFormulaireDto dto);
}
