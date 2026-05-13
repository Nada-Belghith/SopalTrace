using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;

namespace SopalTrace.Application.Validators;

public class CreatePlanAssValidator : AbstractValidator<CreatePlanAssDto>
{
    public CreatePlanAssValidator()
    {
        RuleFor(x => x.OperationCode).NotEmpty().WithMessage("Le code opération est obligatoire.");
        RuleFor(x => x.NatureComposantCode).NotEmpty().WithMessage("La nature composant est obligatoire.");
        RuleFor(x => x.Code).NotEmpty().WithMessage("Le code du modele d'assemblage est obligatoire.");
        RuleFor(x => x.Nom).NotEmpty().WithMessage("Le nom du plan est obligatoire.");

        // RÈGLE MÉTIER : Si c'est un modèle générique, l'article DOIT être nul. Sinon il est obligatoire.
        When(x => x.EstModele, () =>
        {
            RuleFor(x => x.CodeArticleSage).Null().WithMessage("Un plan maître ne doit pas être lié à un article spécifique.");
        });

        When(x => !x.EstModele, () =>
        {
            RuleFor(x => x.CodeArticleSage).NotEmpty().WithMessage("Le code article SAGE est obligatoire pour une exception.");
        });
    }
}

public class SectionAssEditDtoValidator : AbstractValidator<SectionAssEditDto>
{
    public SectionAssEditDtoValidator()
    {
        RuleFor(x => x.LibelleSection).NotEmpty().WithMessage("Le libellé de la section est obligatoire.");
        RuleFor(x => x.TypeSectionId).NotEmpty().WithMessage("Le type de section est obligatoire.");
        RuleForEach(x => x.Lignes).SetValidator(new LigneAssEditDtoValidator());
    }
}

public class LigneAssEditDtoValidator : AbstractValidator<LigneAssEditDto>
{
    public LigneAssEditDtoValidator()
    {
        RuleFor(x => x.LibelleAffiche).NotEmpty().WithMessage("Le libellé de la caractéristique est obligatoire.");
        RuleFor(x => x.TypeCaracteristiqueId).NotEmpty().WithMessage("Le type de caractéristique est obligatoire.");
        RuleFor(x => x.TypeControleId).NotEmpty().WithMessage("Le type de contrôle est obligatoire.");

    }
}
