using FluentValidation;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Validators;

/// <summary>
/// Validateur pour s'assurer qu'un plan BROUILLON possède tous les champs obligatoires
/// avant d'être activé (basculé au statut ACTIF).
/// 
/// Ce validateur s'applique UNIQUEMENT lors de l'activation, pas lors des sauvegardes en brouillon.
/// </summary>
public class PlanActivationValidator : AbstractValidator<PlanFabEntete>
{
    public PlanActivationValidator()
    {
        RuleFor(p => p.PlanFabSections)
            .NotEmpty().WithMessage("Le plan doit contenir au moins une section pour être activé.");

        RuleForEach(p => p.PlanFabSections)
            .ChildRules(section =>
            {
                section.RuleFor(s => s.PlanFabLignes)
                    .NotEmpty().WithMessage("Chaque section doit contenir au moins une ligne.");

                section.RuleForEach(s => s.PlanFabLignes)
                    .ChildRules(ligne =>
                    {
                        // TypeControleId est le seul champ toujours obligatoire
                        ligne.RuleFor(l => l.TypeControleId)
                            .NotEmpty().WithMessage("Chaque ligne doit avoir un type de contrôle.")
                            .Must(id => id.HasValue && id.Value != Guid.Empty).WithMessage("Le type de contrôle ne peut pas être vide.");

                        // Chaque ligne doit fournir au moins une information utile à l'opérateur :
                        // soit une caractéristique, soit une valeur nominale, soit une limite textuelle.
                        ligne.RuleFor(l => l)
                            .Must(l =>
                                (l.TypeCaracteristiqueId.HasValue && l.TypeCaracteristiqueId.Value != Guid.Empty)
                                || l.ValeurNominale.HasValue
                                || (!string.IsNullOrWhiteSpace(l.LimiteSpecTexte))
                            )
                            .WithMessage("Chaque ligne doit avoir soit une caractéristique, soit une valeur nominale, soit une limite spécifique.");

                        // Validation conditionnelle : si une valeur nominale est renseignée
                        // ET qu'il n'y a pas de limite textuelle, les tolérances sont requises.
                        ligne.When(l => l.ValeurNominale.HasValue && string.IsNullOrWhiteSpace(l.LimiteSpecTexte), () =>
                        {
                            ligne.RuleFor(l => l.ToleranceSuperieure)
                                .NotNull().WithMessage("Tolérance supérieure requise si une valeur nominale est renseignée.");

                            ligne.RuleFor(l => l.ToleranceInferieure)
                                .NotNull().WithMessage("Tolérance inférieure requise si une valeur nominale est renseignée.");
                        });
                    });
            });
    }
}
