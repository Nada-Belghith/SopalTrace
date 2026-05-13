namespace SopalTrace.Application.Utilities;

/// <summary>
/// Centralizes common plan metadata normalization and initialization.
/// Ensures consistent handling of creation, update, and archival metadata across all plan types.
/// Provides single-point-of-truth for user name truncation and timestamp generation.
/// </summary>
public static class PlanMetadataHelper
{
    /// <summary>
    /// Initializes creation metadata for a new plan.
    /// Normalizes user name and sets current UTC timestamp.
    /// </summary>
    public static (string creePar, System.DateTime creeLe) InitializeCreationMetadata(string creePar)
    {
        var normalizedUser = creePar ?? "SYSTEM";
        return (normalizedUser, System.DateTime.UtcNow);
    }

    /// <summary>
    /// Initializes update metadata for a modified plan.
    /// Normalizes user name and sets current UTC timestamp.
    /// </summary>
    public static (string modifiePar, System.DateTime modifieLe) InitializeUpdateMetadata(string? modifiePar)
    {
        var normalizedUser = modifiePar ?? "SYSTEM";
        return (normalizedUser, System.DateTime.UtcNow);
    }

    /// <summary>
    /// Initializes archival metadata when a plan is archived.
    /// Normalizes user name and sets current UTC timestamp.
    /// </summary>
    public static (string archivePar, System.DateTime archiveLe) InitializeArchivalMetadata(string? archivePar)
    {
        var normalizedUser = archivePar ?? "SYSTEM";
        return (normalizedUser, System.DateTime.UtcNow);
    }

    /// <summary>
    /// Safely normalizes user/author name by truncating to maximum length.
    /// Used consistently across all plan services for author field normalization.
    /// Business rule: Author names are limited to 50 characters in database.
    /// Defaults to "SYSTEM" if input is null or empty.
    /// </summary>
    /// <param name="authorName">The author name to normalize</param>
    /// <param name="maxLength">Maximum allowed length (default: 50 characters)</param>
    /// <returns>Normalized author name, trimmed and truncated</returns>
    public static string NormalizeAuthorNameWithTruncation(string? authorName, int maxLength = 50)
    {
        var val = (authorName ?? "SYSTEM").Trim();
        return val.Length > maxLength ? val.Substring(0, maxLength) : val;
    }

    /// <summary>
    /// Alias for backward compatibility. Prefer NormalizeAuthorNameWithTruncation for new code.
    /// </summary>
    public static string SecuriserNomAuteur(string? auteur, int maxLength = 50)
    {
        return NormalizeAuthorNameWithTruncation(auteur, maxLength);
    }
}
