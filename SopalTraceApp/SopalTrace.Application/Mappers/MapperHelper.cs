#nullable enable

using System;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class MapperHelper
{
    public static Guid? NullIfEmpty(Guid? value) => (value == null || value == Guid.Empty) ? null : value;

    public static string? NullIfEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Equals("N/A", StringComparison.OrdinalIgnoreCase)) return null;
        if (trimmed.Equals("TOUS", StringComparison.OrdinalIgnoreCase)) return null;
        return trimmed;
    }

    public static bool IsCustomInstrumentCode(string? instrumentCode)
        => !string.IsNullOrWhiteSpace(instrumentCode)
           && instrumentCode.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-' && ch != '_' && ch != '/');

    public static (string? InstrumentCode, string? MoyenTexteLibre) NormalizeInstrumentCode(string? instrumentCode, string? moyenTexteLibre = null)
    {
        var normalizedInstrumentCode = NullIfEmpty(instrumentCode);
        var normalizedMoyenTexteLibre = NullIfEmpty(moyenTexteLibre);

        if (!string.IsNullOrWhiteSpace(normalizedMoyenTexteLibre))
        {
            return (normalizedInstrumentCode, normalizedMoyenTexteLibre);
        }

        if (IsCustomInstrumentCode(normalizedInstrumentCode))
        {
            return (null, normalizedInstrumentCode);
        }

        return (normalizedInstrumentCode, null);
    }
}
