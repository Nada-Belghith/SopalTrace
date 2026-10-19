using System;
using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.Echantillonnage;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Application.Validators.QualityPlans;

/// <summary>Règles communes aux paramètres ISO 2859-1 (mêmes valeurs que les contraintes de la table SamplingPlan).
/// Ac et Re sont libres : aucune règle métier arrêtée entre eux pour le moment.</summary>
internal static class ReglesPlanEchantillonnage
{
    public static void Appliquer<T>(AbstractValidator<T> validator,
        Func<T, string> niveau, Func<T, string> type, Func<T, string> severite,
        Func<T, int?> nqaId, Func<T, decimal?> valeurNqa)
    {
        validator.RuleFor(x => niveau(x))
            .Must(v => v != null && SamplingPlanValues.InspectionLevels.Contains(v))
            .WithName("NiveauControle")
            .WithMessage("Le niveau de contrôle doit être I, II, III ou S-1 à S-4.");

        validator.RuleFor(x => type(x))
            .Must(v => v != null && SamplingPlanValues.SamplingTypes.Contains(v))
            .WithName("TypePlan")
            .WithMessage("Le type de plan doit être SIMPLE ou DOUBLE.");

        validator.RuleFor(x => severite(x))
            .Must(v => v != null && SamplingPlanValues.InspectionSeverities.Contains(v))
            .WithName("ModeControle")
            .WithMessage("Le mode de contrôle doit être NORMAL, REDUIT ou RENFORCE.");

        validator.RuleFor(x => x)
            .Must(x => nqaId(x) is > 0 || valeurNqa(x) is > 0)
            .WithName("NqaId")
            .WithMessage("Le niveau de qualité acceptable (NQA) est obligatoire.");
    }
}

public class CreateDocumentEchantillonnageRequestValidator : AbstractValidator<CreateDocumentEchantillonnageRequestDto>
{
    public CreateDocumentEchantillonnageRequestValidator()
    {
        ReglesPlanEchantillonnage.Appliquer(this, x => x.NiveauControle, x => x.TypePlan, x => x.ModeControle,
            x => x.NqaId, x => x.ValeurNqa);
    }
}

public class UpdateDocumentEchantillonnageRequestValidator : AbstractValidator<UpdateDocumentEchantillonnageRequestDto>
{
    public UpdateDocumentEchantillonnageRequestValidator()
    {
        ReglesPlanEchantillonnage.Appliquer(this, x => x.NiveauControle, x => x.TypePlan, x => x.ModeControle,
            x => x.NqaId, x => x.ValeurNqa);
    }
}
