using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;

namespace SopalTrace.Application.Validators;

public class CreateModeleRequestValidator : AbstractValidator<CreateModeleRequestDto>
{
    public CreateModeleRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Le code du modèle est obligatoire.")
            .MaximumLength(60).WithMessage("Le code ne doit pas dépasser 60 caractères.");

        RuleFor(x => x.Libelle)
            .NotEmpty().WithMessage("Le libellé est obligatoire.")
            .MaximumLength(150).WithMessage("Le libellé ne doit pas dépasser 150 caractères.");

        RuleFor(x => x.TypeRobinetCode)
            .NotEmpty().WithMessage("Le type de robinet est obligatoire.");

        RuleFor(x => x.NatureComposantCode)
            .NotEmpty().WithMessage("La nature du composant est obligatoire.");

        RuleFor(x => x.OperationCode)
            .MaximumLength(20).WithMessage("Le code de l'opération ne doit pas dépasser 20 caractères.");

        RuleFor(x => x.Sections)
            .NotEmpty().WithMessage("Un modèle doit contenir au moins une section.");

        RuleForEach(x => x.Sections).ChildRules(section =>
        {
            section.RuleFor(s => s.LibelleSection)
                   .NotEmpty().WithMessage("Toutes les sections doivent avoir un libellé.");

            section.RuleFor(s => s.Lignes)
                   .NotEmpty().WithMessage("Chaque section doit contenir au moins une ligne de contrôle.");

            section.RuleForEach(s => s.Lignes)
                   .SetValidator(new ModeleCreateLigneValidator());
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