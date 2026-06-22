using System.Collections.Generic;
using SopalTrace.Domain.Entities;
using System.Text.RegularExpressions;

namespace SopalTrace.Application.Helpers;

public static class TranchesHorairesGenerator
{
    public static List<string> GenererTranchesHoraires(RefFormulaireEquipe equipe)
    {
        var tranches = new List<string>();
        for (int h = equipe.HeureDebut; h < equipe.HeureFin; h++)
        {
            // "EQ1_H6_7", "EQ1_H7_8" ...
            var cle = $"{Slugify(equipe.NomEquipe)}_H{h}_{h + 1}";
            tranches.Add(cle);
        }
        return tranches;
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var result = text.ToUpperInvariant();
        result = Regex.Replace(result, @"[^A-Z0-9]+", "");
        return result;
    }
}
