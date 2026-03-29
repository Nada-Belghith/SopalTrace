using FluentValidation;
using SopalTrace.Application.DTOs.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Matricule)
            .NotEmpty().WithMessage("Le matricule est requis.")
            .Length(5).WithMessage("Le matricule doit faire exactement 5 caractères.");

        RuleFor(x => x.MotDePasse)
            .NotEmpty().WithMessage("Le mot de passe est requis.")
            .MinimumLength(6).WithMessage("Le mot de passe doit contenir au moins 6 caractères.");
    }
}
