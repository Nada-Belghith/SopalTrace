using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Application.Services;

public class PlanMasMachineService : PlanVerifMachineService
{
    public PlanMasMachineService(IUnitOfWork unitOfWork, IValidator<CreatePlanVerifMachineDto> createValidator, IReferentielService referentielService) 
        : base(unitOfWork, createValidator, referentielService)
    {
    }

    public override string Role => "MAS";
}
