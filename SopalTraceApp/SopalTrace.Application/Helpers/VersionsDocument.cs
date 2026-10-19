using System;

namespace SopalTrace.Application.Helpers;

/// <summary>Version d'un document (RowVersion) échangée avec le front sous forme de texte base64.</summary>
public static class VersionsDocument
{
    public static string? EnTexte(byte[]? version)
        => version is { Length: > 0 } ? Convert.ToBase64String(version) : null;
}
