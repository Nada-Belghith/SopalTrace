 using System;
using System.Text.RegularExpressions;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

public partial class OccurrenceService
{
    private static ExecPrelevementIntermediaire CreerIntermediaire(
        Guid ofId, Guid sectionId, string tranche, int numero, DateTime heure, Guid? sessionId = null) =>
        new()
        {
            ExecControleOfid = ofId, SectionId = sectionId,
            TrancheHoraire = tranche, NumeroOccurrence = numero,
            HeureNotifPrevue = heure, EstRepondu = false, EstEnRetard = false,
            SessionId = sessionId
        };

    private static string NormaliserLibelle(string libelle, DocumentSection? section = null)
    {
        string replaceWith = "4 p/h"; // fallback

        if (section?.Periodicite != null)
        {
            var p = section.Periodicite;
            string unite = p.Unite == "HEURE" ? "p/h" : p.Unite;
            replaceWith = $"{(p.NbrPieces > 0 ? p.NbrPieces : 4)} {unite}";
        }

        // Replace "(4 p/h)", "(15 p/h)", "(p/h)" etc. with the new frequency.
        string result = Regex.Replace(libelle, @"\(\s*\d*\s*p/h\s*\)", $"({replaceWith})", RegexOptions.IgnoreCase);

        // Also fallback replacements for other formats if any
        return result.Replace("Effectif de l'échantillon /poste (A/B) (p/h)", $"Effectif de l'échantillon /poste (A/B) ({replaceWith})")
                     .Replace("échantillon /poste...", $"échantillon /poste ({replaceWith})");
    }

    private static (DateTime debut, DateTime fin) ParseHeureTranche(string trancheHoraire, DateTime fallback)
    {
        var match = Regex.Match(trancheHoraire, @"H_(\d+)_(\d+)");
        if (!match.Success) return (fallback, fallback.AddHours(1));

        int h1 = int.Parse(match.Groups[1].Value);
        int h2 = int.Parse(match.Groups[2].Value);
        DateTime today = DateTime.Today;
        DateTime debut = today.AddHours(h1);
        DateTime fin = (h2 == 0 || h2 == 24 || h2 < h1) ? today.AddDays(1) : today.AddHours(h2);
        return (debut, fin);
    }
}
