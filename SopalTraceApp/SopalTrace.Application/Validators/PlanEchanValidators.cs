using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;

namespace SopalTrace.Application.Validators.QualityPlans;

public class CreatePlanEchanRequestValidator : AbstractValidator<CreatePlanEchanRequestDto>
{
    public CreatePlanEchanRequestValidator()
    {

        RuleFor(x => x.NiveauControle)
            .NotEmpty().WithMessage("Le niveau de contrôle est obligatoire.")
            .Must(x => x == "NIVEAU I" || x == "NIVEAU II" || x == "NIVEAU III" || x == "I" || x == "II" || x == "III")
            .WithMessage("Le niveau de contrôle doit être I, II ou III.");

        RuleFor(x => x.TypePlan)
            .NotEmpty().WithMessage("Le type de plan est obligatoire.")
            .Must(x => x == "SIMPLE" || x == "DOUBLE")
            .WithMessage("Le type de plan doit être SIMPLE ou DOUBLE.");

        RuleFor(x => x.ModeControle)
            .NotEmpty().WithMessage("Le mode de contrôle est obligatoire.")
            .Must(x => x == "NORMAL" || x == "REDUIT" || x == "RÉDUIT" || x == "RENFORCE" || x == "RENFORCÉ")
            .WithMessage("Le mode de contrôle doit être NORMAL, RÉDUIT ou RENFORCÉ.");

        RuleFor(x => x.NqaId)
            .Must((dto, nqaId) => (nqaId.HasValue && nqaId > 0) || (dto.ValeurNqa.HasValue && dto.ValeurNqa > 0))
            .WithMessage("Le NQA est obligatoire.");
    }
}

