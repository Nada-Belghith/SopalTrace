using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Specifications;

/// <summary>
/// Centralise les règles métier pour les plans d'assemblage
/// Applique le pattern Specification pour éviter la duplication de logique
/// </summary>
public static class PlanAssSpecification
{
    /// <summary>
    /// Vérifie si un plan maître actif peut être créé
    /// </summary>
    public static void ValidatePlanMaitreCreation(bool planMaitreActifExists, string operationCode, string typeRobinetCode)
    {
        if (planMaitreActifExists)
        {
            throw new PlanMaitreAlreadyExistsException(operationCode, typeRobinetCode);
        }
    }

    /// <summary>
    /// Vérifie si un plan d'exception actif peut être créé
    /// </summary>
    public static void ValidatePlanExceptionCreation(bool planExceptionActifExists, string operationCode, 
        string typeRobinetCode, string codeArticle)
    {
        if (planExceptionActifExists)
        {
            throw new PlanExceptionAlreadyExistsException(operationCode, typeRobinetCode, codeArticle);
        }
    }

    /// <summary>
    /// Valide qu'un code article est fourni pour un plan d'exception
    /// </summary>
    public static void ValidateArticleCodeForException(string? codeArticle)
    {
        if (string.IsNullOrWhiteSpace(codeArticle))
        {
            throw new MissingArticleCodeException();
        }
    }

    /// <summary>
    /// Valide qu'un article existe dans l'ERP
    /// </summary>
    public static void ValidateArticleExistsInErp(string? designation, string codeArticle)
    {
        if (string.IsNullOrWhiteSpace(designation))
        {
            throw new ArticleNotFoundInErpException(codeArticle);
        }
    }

    /// <summary>
    /// Valide qu'un plan existe
    /// </summary>
    public static void ValidatePlanExists(PlanAssEntete? plan, Guid planId)
    {
        if (plan is null)
        {
            throw new PlanNotFoundException(planId);
        }
    }

    /// <summary>
    /// Retourne le statut correct pour un plan dupliqué
    /// </summary>
    public static string GetStatusForDuplicatedPlan() => StatutsPlan.Brouillon;

    /// <summary>
    /// Retourne le nom pour un plan dupliqué (exception)
    /// </summary>
    public static string GetNameForDuplicatedPlanException(string? codeArticle) 
        => $"PC-{codeArticle}-v1";
}