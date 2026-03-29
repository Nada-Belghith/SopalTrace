using FluentValidation;
using SopalTrace.Application.DTOs.Auth;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'adresse email est requise.")
            .EmailAddress().WithMessage("Format d'email invalide.");
    }
}