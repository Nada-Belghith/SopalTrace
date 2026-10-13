using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Application.Validators.QualityPlans;

/// <summary>
/// Plan de vérification machine (création, mise à jour, nouvelle version).
/// Les règles reflètent les contraintes de la base : un message clair au lieu d'un refus technique.
/// </summary>
public class CreateDocumentVerifMachineRequestDtoValidator : AbstractValidator<CreateDocumentVerifMachineRequestDto>
{
    public CreateDocumentVerifMachineRequestDtoValidator()
    {
        RuleFor(x => x.MachineCode)
            .NotEmpty().WithMessage("Le code machine est obligatoire.")
            .MaximumLength(30).WithMessage("Le code machine ne peut pas dépasser 30 caractères.");

        RuleFor(x => x.Nom)
            .NotEmpty().WithMessage("Le nom du plan est obligatoire.")
            .MaximumLength(140).WithMessage("Le nom du plan ne peut pas dépasser 140 caractères.");

        RuleForEach(x => x.LignesRisques)
            .SetValidator(new CreateDocumentVerifMachineLigneDtoValidator("Risque", TypesLigneVerifMachine.Risque));

        RuleForEach(x => x.LignesConformite)
            .SetValidator(new CreateDocumentVerifMachineLigneDtoValidator("Conformité", TypesLigneVerifMachine.Conformite));
    }
}

/// <summary>Ligne de risque ou de conformité.</summary>
internal sealed class CreateDocumentVerifMachineLigneDtoValidator : AbstractValidator<CreateDocumentVerifMachineLigneDto>
{
    public CreateDocumentVerifMachineLigneDtoValidator(string section, string typeAttendu)
    {
        RuleFor(x => x.TypeLigne)
            .Equal(typeAttendu).WithMessage($"{section} : type de ligne attendu {typeAttendu}.");

        RuleFor(x => x.LibelleRisque)
            .NotEmpty().WithMessage($"{section} : le libellé est obligatoire.")
            .MaximumLength(250).WithMessage($"{section} : le libellé ne peut pas dépasser 250 caractères.");

        RuleForEach(x => x.Echeances)
            .SetValidator(new CreateDocumentVerifMachineEcheanceDtoValidator(section));
    }
}

/// <summary>Échéance (périodicité) et ses pièces de référence.</summary>
internal sealed class CreateDocumentVerifMachineEcheanceDtoValidator : AbstractValidator<CreateDocumentVerifMachineEcheanceDto>
{
    private static readonly string[] RolesPiece = { "PRC", "PRNC", "FEC", "FENC" };

    public CreateDocumentVerifMachineEcheanceDtoValidator(string section)
    {
        RuleFor(x => x.PeriodiciteMachineId)
            .NotEmpty().WithMessage($"{section} : la périodicité est obligatoire.");

        RuleForEach(x => x.MatricePieces)
            .Must(m => System.Array.IndexOf(RolesPiece, m.RoleVerif) >= 0)
            .WithMessage($"{section} : rôle de pièce de référence inconnu (PRC, PRNC, FEC ou FENC attendu).");
    }
}
