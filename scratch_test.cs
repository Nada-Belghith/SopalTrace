using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

public class Test
{
    protected static readonly string[] CaractKeywords = { "caracteristique", "carac", "cote", "parametre", "defaut", "risque" };
    protected static readonly string[] TypeKeywords = { "type", "methode" }; 
    protected static readonly string[] MoyenKeywords = { "moyen", "instrument", "appareil", "outil", "equipement" };
    protected static readonly string[] ControleKeywords = { "controle", "verif", "detec" };
    protected static readonly string[] ObsKeywords = { "observation", "remarque", "note", "instruction" };
    protected static readonly string[] PerioKeywords = { "period", "frequence", "echeance", "taux" };
    protected static readonly string[] ConformiteKeywords = { "conformit" };

    protected static string NormalizeForSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "").Replace("\n", "").Replace("\r", "");
    }

    protected static bool MatchesAny(string normalizedText, string[] keywords)
    {
        return keywords.Any(k => normalizedText.Contains(k));
    }

    public static void Main()
    {
        string[] headers = { "Test de conformité", "Méthode de", "Périodicité", "mas22", "Moyen de détection", "Famille Corps (30, 35)" };
        
        int? moyenDetCol = null;
        int methodeCol = 0;
        
        for (int i=0; i<headers.Length; i++)
        {
            string val = NormalizeForSearch(headers[i]);
            Console.WriteLine($"Col {i+1} '{headers[i]}' -> '{val}'");
            
            // BuildVMMap logic
            if (!val.Contains("num") && MatchesAny(val, MoyenKeywords) && MatchesAny(val, ControleKeywords))
            {
                Console.WriteLine($"  BuildVMMap: MoyenDetCol = {i+1}");
            }
            
            // Inline logic
            if (MatchesAny(val, TypeKeywords)) 
                Console.WriteLine($"  Inline: MethodeCol = {i+1}");
            else if (!val.Contains("num") && MatchesAny(val, ControleKeywords)) 
                Console.WriteLine($"  Inline: MoyenDetCol = {i+1}");
        }
    }
}
