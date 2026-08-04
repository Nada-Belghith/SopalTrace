 using System;
using System.Text.RegularExpressions;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

public partial class OccurrenceService
{
    private static double? CalculerIntervalleMinutes(Periodicite? periodicite, RefRegleEchantillonnage? regle = null, int defaultFreq = 4)
    {
        if (periodicite == null) return null;

        int freqNum = periodicite.FrequenceNum ?? defaultFreq;
        if (freqNum <= 0) freqNum = defaultFreq;

        int freqHeures = 1;
        if (!string.IsNullOrEmpty(periodicite.FrequenceUnite))
        {
            var match = Regex.Match(periodicite.FrequenceUnite, @"\d+");
            if (match.Success) freqHeures = int.Parse(match.Value);
            if (periodicite.FrequenceUnite.Contains("PCT")) freqNum = 1;
        }

        return (freqHeures * 60.0) / Math.Max(freqNum, 1);
    }

    private static bool EstSectionProductionTemporelle(PlanFabricationSection s)
    {
        if (s.Periodicite == null && s.RegleEchantillonnage == null) return false;
        string unite = (s.Periodicite?.FrequenceUnite ?? "").ToUpper();
        string regleLib = (s.RegleEchantillonnage?.Libelle ?? "").ToLower();
        return unite.Contains("HEURE") || unite.Contains("PCT_HEURE") || regleLib.Contains("p/h") || regleLib.Contains("heure");
    }

    private static ExecPrelevementIntermediaire CreerIntermediaire(
        Guid ofId, Guid sectionId, string tranche, int numero, DateTime heure) =>
        new()
        {
            ExecControleOfid = ofId, SectionId = sectionId,
            TrancheHoraire = tranche, NumeroOccurrence = numero,
            HeureNotifPrevue = heure, EstRepondu = false, EstEnRetard = false
        };

    private static string NormaliserLibelle(string libelle, PlanFabricationSection? section = null)
    {
        string replaceWith = "4 p/h"; // fallback

        if (section?.Periodicite != null)
        {
            var p = section.Periodicite;
            string unite = p.FrequenceUnite ?? "p/h";
            replaceWith = $"{(p.FrequenceNum > 0 ? p.FrequenceNum : 4)} {unite}";
        }
        else if (section?.RegleEchantillonnage != null)
        {
            var match = Regex.Match(section.RegleEchantillonnage.Libelle, @"\d+\s*p/h", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                replaceWith = match.Value;
            }
        }

        // Replace "(4 p/h)", "(15 p/h)", "(p/h)" etc. with the new frequency.
        string result = Regex.Replace(libelle, @"\(\s*\d*\s*p/h\s*\)", $"({replaceWith})", RegexOptions.IgnoreCase);

        // Also fallback replacements for other formats if any
        return result.Replace("Effectif de l'échantillon /poste (A/B) (p/h)", $"Effectif de l'échantillon /poste (A/B) ({replaceWith})")
                     .Replace("échantillon /poste...", $"échantillon /poste ({replaceWith})");
    }
    private static string NormaliserLibelle(string libelle, DocumentSection? section)
    {
        string replaceWith = "4 p/h"; // fallback

        if (section?.Periodicite != null)
        {
            var p = section.Periodicite;
            string unite = p.FrequenceUnite ?? "p/h";
            replaceWith = $"{(p.FrequenceNum > 0 ? p.FrequenceNum : 4)} {unite}";
        }
        else if (section?.RegleEchantillonnage != null)
        {
            var match = Regex.Match(section.RegleEchantillonnage.Libelle, @"\d+\s*p/h", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                replaceWith = match.Value;
            }
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
