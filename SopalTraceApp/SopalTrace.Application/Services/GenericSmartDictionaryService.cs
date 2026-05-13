using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Utilities;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

/// <summary>
/// Generic smart dictionary pass service to centralize quality dictionary resolution and creation
/// across all plan types (FAB, PF, ASS, NC, etc.)
/// 
/// Reduces duplication of the repeating pattern:
/// - Iterate sections
/// - For each line: extract libre text values and IDs
/// - Resolve/create missing characteristics, controls means, instruments
/// - Apply resolved IDs back to entities
/// </summary>
public class GenericSmartDictionaryService
{
    /// <summary>
    /// Executes smart dictionary pass for any plan section collection
    /// </summary>
    /// <typeparam name="TSection">Section entity type (PlanFabSection, PlanPfSection, PlanAssSection, etc.)</typeparam>
    /// <typeparam name="TLine">Line entity type (PlanFabLigne, PlanPfLigne, PlanAssLigne, etc.)</typeparam>
    /// <param name="sections">Collection of sections to process</param>
    /// <param name="repo">Dictionary repository for lookups and creation</param>
    /// <param name="lineExtractor">Function to extract line collection from section</param>
    /// <param name="lineProcessor">Function to extract (caracLibelle, caracSetter, moyenLibelle, moyenSetter, instrumentCode) tuple from line</param>
    /// <param name="regleProcessor">Optional function to process sampling rule from section</param>
    public static async Task ExecuteSmartDictionaryPassAsync<TSection, TLine>(
        IEnumerable<TSection> sections,
        IDictionnaireQualiteRepository repo,
        Func<TSection, ICollection<TLine>> lineExtractor,
        Func<TLine, (string? caracLibelle, Action<Guid?> caracSetter, string? moyenLibelle, Action<Guid?> moyenSetter, string? instrumentCode)> lineProcessor,
        Func<TSection, (string? regleLibelle, Action<Guid?> regleSetter)>? regleProcessor = null)
    {
        var addedCaracs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedInstruments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedMoyens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedRegles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var section in sections)
        {
            // Process sampling rule if handler provided
            string? regleLibelle = null;
            Action<Guid?>? setRegleId = null;

            if (regleProcessor != null)
            {
                var (libelle, setter) = regleProcessor(section);
                regleLibelle = libelle;
                setRegleId = setter;
            }

            // Extract lines and their data
            var lines = lineExtractor(section)
                .Select(l =>
                {
                    // Clean up strings
                    CleanupLineStrings(l);
                    return lineProcessor(l);
                });

            // Execute the centralized smart dictionary resolution
            await SmartDictionaryHelper.ResolveAndCreateMissingReferencesAsync(
                repo,
                regleLibelle,
                setRegleId ?? (_ => { }),
                lines,
                addedRegles,
                addedCaracs,
                addedMoyens,
                addedInstruments
            );
        }
    }

    /// <summary>
    /// Generic cleanup for line strings (nullify empty strings on common properties)
    /// </summary>
    private static void CleanupLineStrings<TLine>(TLine line)
    {
        var properties = line!.GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (prop.PropertyType == typeof(string) && prop.CanRead && prop.CanWrite)
            {
                var value = prop.GetValue(line) as string;
                if (string.IsNullOrWhiteSpace(value))
                {
                    prop.SetValue(line, null);
                }
            }
        }
    }
}
