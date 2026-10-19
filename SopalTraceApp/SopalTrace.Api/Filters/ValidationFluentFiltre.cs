using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SopalTrace.Api.Filters;

/// <summary>
/// Exécute, avant chaque action, le validateur FluentValidation enregistré pour chaque donnée reçue.
/// Un échec lève une <see cref="ValidationException"/>, traduite en 400 avec le détail par ExceptionMiddleware.
/// Tout nouveau DTO doté d'un validateur est ainsi contrôlé sans code supplémentaire dans le contrôleur.
/// </summary>
public sealed class ValidationFluentFiltre : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFluentFiltre(IServiceProvider services)
    {
        _services = services;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null) continue;

            var type = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_services.GetService(type) is not IValidator validateur) continue;

            var resultat = await validateur.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (!resultat.IsValid)
                throw new ValidationException(resultat.Errors);
        }

        await next();
    }
}
