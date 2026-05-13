using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Utilities;

/// <summary>
/// Centralizes sampling rule (Règle d'Échantillonnage) resolution and creation.
/// Provides single-point-of-truth for looking up existing rules by libelle or creating new ones on-demand.
/// Used consistently across PlanAssService and potentially other services for intelligent dictionary resolution.
/// Business logic: A sampling rule is looked up by its libelle name; if not found, a new one is auto-created.
/// </summary>
public static class SamplingRuleHelper
{
    /// <summary>
    /// Resolves a sampling rule by its libelle (name).
    /// If rule exists in database, returns its ID.
    /// If rule does not exist, creates a new one automatically.
    /// Returns null if input libelle is null or empty.
    /// </summary>
    /// <param name="dictionaryRepository">The quality dictionary repository for lookups and creation</param>
    /// <param name="ruleLibelle">The libelle (descriptive name) of the sampling rule to find or create</param>
    /// <returns>The ID of the resolved or newly created sampling rule, or null if input is null/empty</returns>
    public static async Task<Guid?> ResolveOrCreateSamplingRuleByLibelleAsync(
        IDictionnaireQualiteRepository dictionaryRepository,
        string? ruleLibelle)
    {
        if (string.IsNullOrWhiteSpace(ruleLibelle))
            return null;

        var trimmedLibelle = ruleLibelle.Trim();

        // Try to find existing rule in database
        var existingRule = await dictionaryRepository.GetRegleEchantillonnageByLibelleAsync(trimmedLibelle);
        if (existingRule != null)
            return existingRule.Id;

        // Create new rule if not found (intelligent dictionary creation)
        var newRule = new RefRegleEchantillonnage
        {
            Id = Guid.NewGuid(),
            Libelle = TruncateTextToMaxLength(trimmedLibelle, 100),
            Code = GenerateAutoCode("REG"),
            Actif = true
        };

        await dictionaryRepository.AddRegleEchantillonnageAsync(newRule);
        return newRule.Id;
    }

    /// <summary>
    /// Helper to truncate text to maximum database length
    /// </summary>
    private static string TruncateTextToMaxLength(string text, int maxLength)
    {
        return text.Length > maxLength ? text.Substring(0, maxLength) : text;
    }

    /// <summary>
    /// Helper to generate auto-code for new dictionary entries (e.g., REG-ABC12)
    /// </summary>
    private static string GenerateAutoCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";
    }
}

