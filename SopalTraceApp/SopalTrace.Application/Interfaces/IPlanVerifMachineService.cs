using SopalTrace.Application.DTOs.QualityPlans.PlanVerifMachines;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanVerifMachineService
{
    Task<Guid> CreerPlanVerifMachineAsync(CreatePlanVerifMachineRequestDto request);
    Task<PlanVerifMachineEnteteDto> GetPlanVerifMachineByIdAsync(Guid id);
    Task<IEnumerable<PlanVerifMachineEnteteDto>> GetAllPlansAsync();
    Task<IEnumerable<PlanVerifMachineEnteteDto>> GetPlansByMachineCodeAsync(string machineCode);
    Task MettreAJourPlanVerifMachineAsync(Guid id, UpdatePlanVerifMachineRequestDto request);
}
