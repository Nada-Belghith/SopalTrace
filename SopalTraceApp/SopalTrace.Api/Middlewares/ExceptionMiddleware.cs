using System.Net;
using System.Text.Json;
using FluentValidation; // <-- AJOUT POUR FLUENTVALIDATION
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Une erreur s'est produite.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        context.Response.StatusCode = exception switch
        {
            ValidationException => (int)HttpStatusCode.BadRequest, // <-- NOUVEAU: Erreur de formulaire (400)
            AuthException => (int)HttpStatusCode.Unauthorized,     // (401) Toutes les erreurs d'authentification
            DomainException => (int)HttpStatusCode.BadRequest,     // Erreur métier Sopal (400)
            _ => (int)HttpStatusCode.InternalServerError           // Erreur système (500)
        };

        // Si l'erreur vient de nos nouveaux validateurs (FluentValidation)
        if (exception is ValidationException validationException)
        {
            // On renvoie un tableau détaillé des erreurs (ex: "Le code est obligatoire", "Le libellé est trop long")
            var validationErrors = validationException.Errors.Select(e => e.ErrorMessage);
            var result = JsonSerializer.Serialize(new { error = "Erreur de validation", details = validationErrors });
            return context.Response.WriteAsync(result);
        }
        // Sinon, c'est une erreur classique (AuthException, DomainException, etc.)
        else
        {
            var result = JsonSerializer.Serialize(new { error = exception.Message });
            return context.Response.WriteAsync(result);
        }
    }
}