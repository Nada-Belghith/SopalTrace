using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Utilities;

/// <summary>
/// Centralizes type characteristic (TypeCaracteristique) resolution and creation.
/// Provides single-point-of-truth for looking up existing characteristics by libelle or creating new ones on-demand.
/// Used consistently across PlanAssService and other services for intelligent quality dictionary resolution.
/// Business logic: A characteristic is looked up by its libelle name; if not found, a new one is auto-created.
/// </summary>
public static class TypeCaracteristiqueHelper
{
    /// <summary>
    /// Resolves a type characteristic by its libelle (descriptive name).
    /// If characteristic exists in database, returns its ID.
    /// If characteristic does not exist, creates a new one automatically.
    /// Returns null if input libelle is null or empty.
    /// </summary>
    /// <param name="dictionaryRepository">The quality dictionary repository for lookups and creation</param>
    /// <param name="characteristicLibelle">The libelle (descriptive name) of the characteristic to find or create</param>
    /// <returns>The ID of the resolved or newly created characteristic, or null if input is null/empty</returns>
    public static async Task<Guid?> ResolveOrCreateTypeCaracteristiqueByLibelleAsync(
        IDictionnaireQualiteRepository dictionaryRepository,
        string? characteristicLibelle)
    {
        if (string.IsNullOrWhiteSpace(characteristicLibelle))
            return null;

        var trimmedLibelle = characteristicLibelle.Trim();

        // Try to find existing characteristic in database
        var existingCharacteristic = await dictionaryRepository.GetTypeCaracteristiqueByLibelleAsync(trimmedLibelle);
        if (existingCharacteristic != null)
            return existingCharacteristic.Id;

        // Create new characteristic if not found (intelligent dictionary creation)
        var newCharacteristic = new TypeCaracteristique
        {
            Id = Guid.NewGuid(),
            Libelle = TruncateTextToMaxLength(trimmedLibelle, 80),
            Code = GenerateAutoCode("CAR"),
            Actif = true
        };

        await dictionaryRepository.AddTypeCaracteristiqueAsync(newCharacteristic);
        return newCharacteristic.Id;
    }

    /// <summary>
    /// Helper to truncate text to maximum database length
    /// </summary>
    private static string TruncateTextToMaxLength(string text, int maxLength)
    {
        return text.Length > maxLength ? text.Substring(0, maxLength) : text;
    }

    /// <summary>
    /// Helper to generate auto-code for new dictionary entries (e.g., CAR-ABC123)
    /// </summary>
    private static string GenerateAutoCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
    }
}
