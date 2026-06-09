using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Application.Services;

public class PlanMasMachineService : PlanVerifMachineService
{
    public PlanMasMachineService(IUnitOfWork unitOfWork, IValidator<CreatePlanVerifMachineDto> createValidator, ICatalogueReferentielService referentielService, IFormulairePrcService formulaireService) 
        : base(unitOfWork, createValidator, referentielService, formulaireService)
    {
    }

    public override string Role => "MAS";
}
