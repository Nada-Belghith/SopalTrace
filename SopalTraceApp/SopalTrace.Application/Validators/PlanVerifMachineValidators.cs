using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.PlanVerifMachines;

namespace SopalTrace.Application.Validators.QualityPlans;

// ============================================================
// Validator principal : CreatePlanVerifMachineRequestDto
// Validé à la création ET à la mise à jour (PUT)
// ============================================================
public class CreatePlanVerifMachineRequestDtoValidator : AbstractValidator<CreatePlanVerifMachineRequestDto>
{
    public CreatePlanVerifMachineRequestDtoValidator()
    {
        // --- Entête ---
        RuleFor(x => x.MachineCode)
            .NotEmpty().WithMessage("Le code machine est obligatoire.");

        RuleFor(x => x.Nom)
            .NotEmpty().WithMessage("Le nom du plan est obligatoire.")
            .MinimumLength(2).WithMessage("Le nom du plan doit contenir au moins 2 caractères.");

        // --- Lignes Risques : au moins 1 obligatoire ---
        RuleFor(x => x.LignesRisques)
            .NotEmpty().WithMessage("Au moins un risque/défaut est obligatoire.");

        // --- Validation de chaque ligne risque ---
        RuleForEach(x => x.LignesRisques)
            .SetValidator(new CreatePlanVerifMachineLigneDtoValidator("Risque"));

        // --- Validation de chaque ligne conformité (si présentes) ---
        RuleForEach(x => x.LignesConformite)
            .SetValidator(new CreatePlanVerifMachineLigneDtoValidator("Conformité"));
    }
}

// ============================================================
// Validator d'une ligne (Risque ou Conformité)
// ============================================================
public class CreatePlanVerifMachineLigneDtoValidator : AbstractValidator<CreatePlanVerifMachineLigneDto>
{
    public CreatePlanVerifMachineLigneDtoValidator(string sectionName = "Ligne")
    {
        RuleFor(x => x.LibelleRisque)
            .NotEmpty().WithMessage($"{sectionName} : Le libellé Test/Risque est obligatoire.")
            .MaximumLength(500).WithMessage($"{sectionName} : Le libellé ne peut pas dépasser 500 caractères.");

        RuleFor(x => x.LibelleMethode)
            .NotEmpty().WithMessage($"{sectionName} : Le Moyen/Méthode de contrôle est obligatoire.")
            .MaximumLength(500).WithMessage($"{sectionName} : La méthode ne peut pas dépasser 500 caractères.");

        RuleFor(x => x.Echeances)
            .NotEmpty().WithMessage($"{sectionName} : Au moins une périodicité est obligatoire.");

        RuleForEach(x => x.Echeances)
            .SetValidator(new CreatePlanVerifMachineEcheanceDtoValidator(sectionName));
    }
}

// ============================================================
// Validator d'une échéance (périodicité + moyen)
// ============================================================
public class CreatePlanVerifMachineEcheanceDtoValidator : AbstractValidator<CreatePlanVerifMachineEcheanceDto>
{
    public CreatePlanVerifMachineEcheanceDtoValidator(string sectionName = "Ligne")
    {
        RuleFor(x => x.PeriodiciteId)
            .NotEmpty().WithMessage($"{sectionName} : La périodicité est obligatoire.");
    }
}
