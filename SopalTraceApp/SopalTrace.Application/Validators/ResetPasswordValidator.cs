using FluentValidation;
using SopalTrace.Application.DTOs.Auth;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress();

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Le code de récupération est requis.")
            .Length(6).WithMessage("Le code doit comporter 6 chiffres.");

        RuleFor(x => x.NouveauMotDePasse)
            .NotEmpty().WithMessage("Le nouveau mot de passe est requis.")
            .MinimumLength(6).WithMessage("Le mot de passe doit contenir au moins 6 caractères.");
    }
}