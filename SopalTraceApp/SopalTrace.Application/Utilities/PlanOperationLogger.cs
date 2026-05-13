using Microsoft.Extensions.Logging;
using System;

namespace SopalTrace.Application.Utilities;

/// <summary>
/// Helper pour logger les opérations communes de plan de manière cohérente.
/// </summary>
public static class PlanOperationLogger
{
    /// <summary>
    /// Enregistre la création d'un plan.
    /// </summary>
    public static void LogCreation(ILogger logger, string planType, string? codeArticle = null, string? familleCode = null, string? operationCode = null)
    {
        logger.LogInformation(
            "Creation du plan {PlanType}. Code Article: {CodeArticle}, Famille: {FamilleCode}, Operation: {OperationCode}",
            planType, codeArticle ?? "N/A", familleCode ?? "N/A", operationCode ?? "N/A");
    }

    /// <summary>
    /// Enregistre l'activation d'un plan.
    /// </summary>
    public static void LogActivation(ILogger logger, string planType, Guid planId)
    {
        logger.LogInformation("Activation du plan {PlanType} avec ID {PlanId}", planType, planId);
    }

    /// <summary>
    /// Enregistre l'archivage d'un plan.
    /// </summary>
    public static void LogArchivage(ILogger logger, string planType, Guid planId)
    {
        logger.LogInformation("Archivage du plan {PlanType} avec ID {PlanId}", planType, planId);
    }

    /// <summary>
    /// Enregistre la création d'une nouvelle version.
    /// </summary>
    public static void LogNewVersion(ILogger logger, string planType, int newVersion, Guid planId)
    {
        logger.LogInformation(
            "Creation d'une nouvelle version {NewVersion} du plan {PlanType} avec ID {PlanId}",
            newVersion, planType, planId);
    }

    /// <summary>
    /// Enregistre une erreur avec contexte.
    /// </summary>
    public static void LogError(ILogger logger, string planType, string operationName, Exception ex)
    {
        logger.LogError(ex, "Erreur lors de {OperationName} pour le plan {PlanType}", operationName, planType);
    }
}
