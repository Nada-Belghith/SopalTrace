using System.Collections.Generic;

namespace SopalTrace.Domain.Constants;

/// <summary>Valeurs autorisées d'un plan d'échantillonnage (miroir des contraintes de la table SamplingPlan).</summary>
public static class SamplingPlanValues
{
    /// <summary>Niveaux de contrôle ISO 2859-1 (table InspectionLevel) : généraux I à III, spéciaux S-1 à S-4.</summary>
    public static readonly IReadOnlySet<string> InspectionLevels = new HashSet<string> { "I", "II", "III", "S-1", "S-2", "S-3", "S-4" };

    /// <summary>Type d'échantillonnage : simple ou double.</summary>
    public static readonly IReadOnlySet<string> SamplingTypes = new HashSet<string> { "SIMPLE", "DOUBLE" };

    /// <summary>Sévérité du contrôle : normal, réduit ou renforcé.</summary>
    public static readonly IReadOnlySet<string> InspectionSeverities = new HashSet<string> { "NORMAL", "REDUIT", "RENFORCE" };
}
