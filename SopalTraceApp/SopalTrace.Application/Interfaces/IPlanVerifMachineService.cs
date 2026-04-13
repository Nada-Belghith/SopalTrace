using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanVerifMachineService
{
    Task<Guid> CreerPlanAsync(CreatePlanVerifMachineDto request, string creePar);
    Task<PlanVerifMachineResponseDto> GetPlanByIdAsync(Guid planId);
    Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<VerifMachineLigneEditDto> lignesModifiees);
    Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionVerifMachineDto request);
}
