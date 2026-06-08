using System;
using System.Linq;

public class TestVM
{
    protected static readonly string[] ConformiteKeywords = { "conformite", "conform" };
    protected static readonly string[] CaractKeywords = { "caracteristique", "carac", "cote", "parametre", "defaut", "risque" };
    protected static readonly string[] TypeKeywords = { "type", "methode" };
    protected static readonly string[] PerioKeywords = { "period", "frequence", "echeance", "taux" };
    protected static readonly string[] MoyenKeywords = { "moyen", "instrument", "appareil", "outil", "equipement" };
    protected static readonly string[] ControleKeywords = { "controle", "verif", "detec" };
    protected static readonly string[] ObsKeywords = { "observation", "action", "commentaire", "remarque" };

    public static string NormalizeForSearch(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        string normalized = input.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (char c in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().ToLowerInvariant().Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("'", "");
    }

    public static bool MatchesAny(string text, string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        foreach (var kw in keywords)
        {
            if (text.Contains(kw)) return true;
        }
        return false;
    }

    public static void Main()
    {
        string[] headers = { "ue/ Défaut", "Moyen/ Méthode de", "Périodicité", "Moyen de détection", "Famille Corps (30, 35)" };
        
        int? MoyenDetCol = 5; // Suppose it was 5 from the first row

        for (int i = 0; i < headers.Length; i++)
        {
            int cNum = i + 1;
            string val = NormalizeForSearch(headers[i]);
            Console.WriteLine($"Col {cNum}: val='{val}'");
            
            if (MatchesAny(val, ConformiteKeywords) && !MatchesAny(val, ObsKeywords) && !val.Contains("non")) 
                Console.WriteLine($"  -> Matches Risque");
            else if (MatchesAny(val, CaractKeywords)) 
                Console.WriteLine($"  -> Matches Risque (Caract)");
            else if (MatchesAny(val, TypeKeywords)) 
                Console.WriteLine($"  -> Matches Methode");
            else if (MatchesAny(val, PerioKeywords)) 
                Console.WriteLine($"  -> Matches Perio");
            else if (!val.Contains("num") && (val == "moyendedetection" || val == "moyendecontrole" || (MatchesAny(val, MoyenKeywords) && MatchesAny(val, ControleKeywords)))) 
            {
                Console.WriteLine($"  -> Matches MoyenDetCol");
                if (MoyenDetCol == null || val == "moyendedetection") MoyenDetCol = cNum;
            }
        }
        
        Console.WriteLine($"Final MoyenDetCol: {MoyenDetCol}");
    }
}
