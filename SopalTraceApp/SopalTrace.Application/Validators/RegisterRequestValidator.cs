using FluentValidation;
using SopalTrace.Application.DTOs.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Matricule)
            .NotEmpty().WithMessage("Le matricule est requis.")
            .Length(5).WithMessage("Le matricule doit faire exactement 5 caractères.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est requis.")
            .EmailAddress().WithMessage("Le format de l'email est invalide.");
    }
}

