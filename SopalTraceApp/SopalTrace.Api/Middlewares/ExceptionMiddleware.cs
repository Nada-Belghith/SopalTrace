using System.Net;
using System.Text.Json;
using FluentValidation;
using SopalTrace.Domain.Exceptions;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Api.Middlewares;

/// <summary>
/// Convertit les exceptions en réponses JSON homogènes.
/// Seuls les messages des exceptions métier (DomainException et ses dérivées, ValidationException)
/// sont renvoyés au client ; toute autre exception produit un message générique
/// et l'identifiant de trace permet de retrouver le détail dans les logs serveur.
/// </summary>
public class ExceptionMiddleware
{
    private const string MessageErreurInterne = "Une erreur interne est survenue. Contactez le support en indiquant l'identifiant de trace.";

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
        catch (Exception exceptionInterceptee)
        {
            // Erreurs SQL connues (concurrence, unicité, triggers d'intégrité) -> exceptions métier.
            var ex = DbUpdateExceptionExtensions.TraduireErreurBase(exceptionInterceptee);
            var statusCode = GetStatusCode(ex);
            if (statusCode == (int)HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Erreur non gérée (trace {TraceId}).", context.TraceIdentifier);
            else if (!ReferenceEquals(ex, exceptionInterceptee))
                // Erreur de base traduite : l'exception SQL d'origine est conservée dans les logs serveur.
                _logger.LogWarning(exceptionInterceptee, "Erreur base traduite en {Type} (trace {TraceId}) : {Message}", ex.GetType().Name, context.TraceIdentifier, ex.Message);
            else
                _logger.LogWarning("Erreur métier {Type} (trace {TraceId}) : {Message}", ex.GetType().Name, context.TraceIdentifier, ex.Message);

            if (context.Response.HasStarted) throw;
            await WriteResponseAsync(context, ex, statusCode);
        }
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        ValidationException => (int)HttpStatusCode.BadRequest,
        AuthException => (int)HttpStatusCode.Unauthorized,
        RessourceIntrouvableException => (int)HttpStatusCode.NotFound,
        ConflitConcurrenceException or EtatExecutionInvalideException or ScanEnDoubleException => (int)HttpStatusCode.Conflict,
        DomainException => (int)HttpStatusCode.BadRequest,
        _ => (int)HttpStatusCode.InternalServerError
    };

    private static Task WriteResponseAsync(HttpContext context, Exception exception, int statusCode)
    {
        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        object body = exception switch
        {
            ValidationException validation => new
            {
                error = "Erreur de validation",
                message = "Erreur de validation",
                details = validation.Errors.Select(e => e.ErrorMessage),
                traceId = context.TraceIdentifier
            },
            DomainException => new
            {
                error = exception.Message,
                message = exception.Message,
                traceId = context.TraceIdentifier
            },
            _ => new
            {
                error = MessageErreurInterne,
                message = MessageErreurInterne,
                traceId = context.TraceIdentifier
            }
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
