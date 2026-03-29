using FluentValidation;
using SopalTrace.Application.DTOs.Auth;

namespace SopalTrace.Application.Validators;

public class RefreshRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Le token principal est requis.")
            .MaximumLength(2000).WithMessage("Le token principal est trop long.");

    }
}