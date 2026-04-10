using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;

namespace SopalTrace.Application.Validators.QualityPlans;

public class CreatePlanVerifMachineDtoValidator : AbstractValidator<CreatePlanVerifMachineDto>
{
    public CreatePlanVerifMachineDtoValidator()
    {
        RuleFor(x => x.MachineCode).NotEmpty().WithMessage("Le code machine est obligatoire.");
        RuleFor(x => x.TypeRapport).NotEmpty().WithMessage("Le type de rapport (ex: BEE, MAS_ASS) est obligatoire.");
        RuleFor(x => x.Nom).NotEmpty().WithMessage("Le nom du plan est obligatoire.");
    }
}
