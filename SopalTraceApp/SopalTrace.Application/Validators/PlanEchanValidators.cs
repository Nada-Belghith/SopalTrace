using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;

namespace SopalTrace.Application.Validators.QualityPlans;

public class CreatePlanEchanRequestValidator : AbstractValidator<CreatePlanEchanRequestDto>
{
    public CreatePlanEchanRequestValidator()
    {
        RuleFor(x => x.CodeReference)
            .NotEmpty().WithMessage("Le code de référence est obligatoire.");

        RuleFor(x => x.NiveauControle)
            .NotEmpty().WithMessage("Le niveau de contrôle est obligatoire.")
            .Must(x => x == "I" || x == "II" || x == "III")
            .WithMessage("Le niveau de contrôle doit être I, II ou III.");

        RuleFor(x => x.TypePlan)
            .NotEmpty().WithMessage("Le type de plan est obligatoire.")
            .Must(x => x == "SIMPLE" || x == "DOUBLE")
            .WithMessage("Le type de plan doit être SIMPLE ou DOUBLE.");

        RuleFor(x => x.ModeControle)
            .NotEmpty().WithMessage("Le mode de contrôle est obligatoire.")
            .Must(x => x == "NORMAL" || x == "REDUIT" || x == "RENFORCE")
            .WithMessage("Le mode de contrôle doit être NORMAL, REDUIT ou RENFORCE.");

        RuleFor(x => x.NqaId)
            .GreaterThan(0).WithMessage("Le NQA est obligatoire.");
    }
}

