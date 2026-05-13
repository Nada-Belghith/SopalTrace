namespace SopalTrace.Application.Utilities;

/// <summary>
/// Helper utility for cleaning up line entities (removes empty GUIDs and empty strings)
/// </summary>
public static class LineCleanupHelper
{
    /// <summary>
    /// Cleans up a model fabrication line by removing empty GUIDs and whitespace strings
    /// </summary>
    public static void CleanupModeleFabLine(SopalTrace.Domain.Entities.ModeleFabLigne ligne)
    {
        if (ligne.TypeCaracteristiqueId == System.Guid.Empty) ligne.TypeCaracteristiqueId = null;
        if (ligne.TypeControleId == System.Guid.Empty) ligne.TypeControleId = null;
        if (ligne.MoyenControleId == System.Guid.Empty) ligne.MoyenControleId = null;
        if (ligne.PeriodiciteId == System.Guid.Empty) ligne.PeriodiciteId = null;

        if (string.IsNullOrWhiteSpace(ligne.InstrumentCode)) ligne.InstrumentCode = null;
        if (string.IsNullOrWhiteSpace(ligne.LibelleAffiche)) ligne.LibelleAffiche = null;
        if (string.IsNullOrWhiteSpace(ligne.MoyenTexteLibre)) ligne.MoyenTexteLibre = null;
    }

    /// <summary>
    /// Cleans up a plan assemblage line by removing empty GUIDs and whitespace strings
    /// </summary>
    public static void CleanupPlanAssLine(SopalTrace.Domain.Entities.PlanAssLigne ligne)
    {
        if (ligne.TypeCaracteristiqueId == System.Guid.Empty) ligne.TypeCaracteristiqueId = null;
        if (ligne.TypeControleId == System.Guid.Empty) ligne.TypeControleId = null;
        if (ligne.MoyenControleId == System.Guid.Empty) ligne.MoyenControleId = null;
        if (ligne.PeriodiciteId == System.Guid.Empty) ligne.PeriodiciteId = null;

        if (string.IsNullOrWhiteSpace(ligne.InstrumentCode)) ligne.InstrumentCode = null;
        if (string.IsNullOrWhiteSpace(ligne.LibelleAffiche)) ligne.LibelleAffiche = null;
        if (string.IsNullOrWhiteSpace(ligne.MoyenTexteLibre)) ligne.MoyenTexteLibre = null;
    }

    /// <summary>
    /// Cleans up a plan fabrication line by removing empty GUIDs and whitespace strings
    /// </summary>
    public static void CleanupPlanFabLine(SopalTrace.Domain.Entities.PlanFabLigne ligne)
    {
        if (ligne.TypeCaracteristiqueId == System.Guid.Empty) ligne.TypeCaracteristiqueId = null;
        if (ligne.TypeControleId == System.Guid.Empty) ligne.TypeControleId = null;
        if (ligne.MoyenControleId == System.Guid.Empty) ligne.MoyenControleId = null;
        if (ligne.PeriodiciteId == System.Guid.Empty) ligne.PeriodiciteId = null;

        if (string.IsNullOrWhiteSpace(ligne.InstrumentCode)) ligne.InstrumentCode = null;
        if (string.IsNullOrWhiteSpace(ligne.LibelleAffiche)) ligne.LibelleAffiche = null;
        if (string.IsNullOrWhiteSpace(ligne.MoyenTexteLibre)) ligne.MoyenTexteLibre = null;
    }
}
