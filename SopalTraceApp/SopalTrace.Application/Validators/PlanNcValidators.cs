using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.PlansNC;

namespace SopalTrace.Application.Validators.QualityPlans;

public class CreatePlanNcRequestValidator : AbstractValidator<CreatePlanNcRequestDto>
{
    public CreatePlanNcRequestValidator()
    {
        RuleFor(x => x.PosteCode).NotEmpty().WithMessage("Le code du poste de travail est obligatoire.");
        RuleFor(x => x.Nom).NotEmpty().WithMessage("Le nom du plan est obligatoire.");
        RuleForEach(x => x.Lignes).SetValidator(new LigneNcEditDtoValidator());
    }
}

public class LigneNcEditDtoValidator : AbstractValidator<LigneNcEditDto>
{
    public LigneNcEditDtoValidator()
    {
        RuleFor(x => x.MachineCode).NotEmpty().WithMessage("La machine (ou banc d'essai) est obligatoire.");
        
        RuleFor(x => x)
            .Must(x => x.RisqueDefautId.HasValue || !string.IsNullOrWhiteSpace(x.LibelleDefaut))
            .WithMessage("Le défaut (existant ou nouveau) est obligatoire.");
    }
}
