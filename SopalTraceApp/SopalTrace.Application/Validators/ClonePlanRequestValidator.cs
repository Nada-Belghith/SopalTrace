using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.Plans;

namespace SopalTrace.Application.Validators;

public class ClonePlanRequestValidator : AbstractValidator<ClonePlanRequestDto>
{
    public ClonePlanRequestValidator()
    {
        RuleFor(x => x.PlanExistantId)
            .NotEmpty().WithMessage("L'ID du plan source à cloner est obligatoire.");

        RuleFor(x => x.NouveauCodeArticleSage)
            .NotEmpty().WithMessage("Le code du nouvel article est obligatoire.")
            .MaximumLength(30).WithMessage("Le code article ne doit pas dépasser 30 caractères.");
            
        RuleFor(x => x.CreePar)
            .NotEmpty().WithMessage("L'auteur de la modification est requis.");
    }
}
