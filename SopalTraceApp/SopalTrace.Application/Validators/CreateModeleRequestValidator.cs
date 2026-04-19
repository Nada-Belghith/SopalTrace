using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;

namespace SopalTrace.Application.Validators;

public class CreateModeleRequestValidator : AbstractValidator<CreateModeleRequestDto>
{
    public CreateModeleRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Le code du modèle est obligatoire.");
        RuleFor(x => x.OperationCode).NotEmpty().WithMessage("Le code de l'opération est obligatoire.");
        RuleFor(x => x.NatureComposantCode).NotEmpty().WithMessage("La nature du composant est obligatoire.");

        // NOTE: On NE met PAS TypeRobinetCode en 'NotEmpty()' car il est optionnel (peut être null).

        RuleFor(x => x.Sections).NotEmpty().WithMessage("Un modèle doit contenir au moins une section.");

        RuleForEach(x => x.Sections).ChildRules(section =>
        {
            section.RuleFor(s => s.LibelleSection).NotEmpty().WithMessage("Toutes les sections doivent avoir un libellé.");
        });
    }
}

public class ModeleCreateLigneValidator : AbstractValidator<LigneModeleEditDto>
{
    public ModeleCreateLigneValidator()
    {
        RuleFor(l => l.TypeCaracteristiqueId)
            .NotEmpty().WithMessage("La catégorie de la caractéristique est obligatoire.");

        RuleFor(l => l.TypeControleId)
            .NotEmpty().WithMessage("Le type de contrôle est obligatoire.");
    }
}