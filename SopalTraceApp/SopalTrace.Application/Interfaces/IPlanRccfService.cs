using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.PlanRCCF;

namespace SopalTrace.Application.Interfaces
{
    public interface IPlanRccfService
    {
        Task<IEnumerable<PlanRccfDto>> GetAllAsync(bool includeArchived = false);
        Task<PlanRccfDto> GetByIdAsync(Guid id);
        Task<PlanRccfDto> CreateAsync(CreatePlanRccfRequest request, string matricule);
        Task<PlanRccfDto> UpdateAsync(Guid id, UpdatePlanRccfRequest request, string matricule);
        Task<PlanRccfDto> ArchiveAsync(Guid id, string matricule);
        Task<PlanRccfDto> CreateNewVersionAsync(Guid id, string matricule);
        Task ValidateAsync(Guid id, string matricule);
        Task CancelValidationAsync(Guid id, string matricule);
    }
}
