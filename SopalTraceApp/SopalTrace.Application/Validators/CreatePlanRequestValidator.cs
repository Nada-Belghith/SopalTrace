using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;

namespace SopalTrace.Application.Validators;

public class CreatePlanRequestValidator : AbstractValidator<CreatePlanRequestDto>
{
    public CreatePlanRequestValidator()
    {

        RuleFor(x => x.CodeArticleSage)
            .NotEmpty().WithMessage("Le code article SAGE est obligatoire.")
            .MaximumLength(30).WithMessage("Le code article ne doit pas dépasser 30 caractères.");
    }
}
