using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IReferentielService
{
    Task<ReferentielsResponseDto> GetFabricationReferentielsAsync();
}