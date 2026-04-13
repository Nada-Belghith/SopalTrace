using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.PlansNC;

namespace SopalTrace.Application.Validators.QualityPlans;

public class CreatePlanNcRequestValidator : AbstractValidator<CreatePlanNcRequestDto>
{
    public CreatePlanNcRequestValidator()
    {
        RuleFor(x => x.TypeRobinetCode).NotEmpty().WithMessage("Le type de robinet est obligatoire.");
        RuleFor(x => x.OperationCode).NotEmpty().WithMessage("Le code de l'opération est obligatoire.");
        RuleFor(x => x.PosteCode).NotEmpty().WithMessage("Le code du poste de travail est obligatoire.");
        RuleFor(x => x.Nom).NotEmpty().WithMessage("Le nom du plan est obligatoire.");
    }
}

public class ColonneNcEditDtoValidator : AbstractValidator<ColonneNcEditDto>
{
    public ColonneNcEditDtoValidator()
    {
        RuleFor(x => x.MachineCode).NotEmpty().WithMessage("La machine (ou banc d'essai) est obligatoire.");
        RuleFor(x => x.LibelleDefaut).NotEmpty().WithMessage("Le libellé du défaut ou de l'essai est obligatoire.");
    }
}
