using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Application.Services;

public class PlanSer05MachineService : PlanVerifMachineService
{
    public PlanSer05MachineService(IUnitOfWork unitOfWork, IValidator<CreatePlanVerifMachineDto> createValidator, IReferentielService referentielService) 
        : base(unitOfWork, createValidator, referentielService)
    {
    }

    public override string Role => "SER05";
}
