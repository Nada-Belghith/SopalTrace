using System;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string text = "Caractéristiques à contrôler au réglage et en cours de production test (4 pièce /1heures)";
        string cleanText = text.Replace("\n", " ").Replace("\r", " ").Trim();
        string parenthesesContent = "";
        string naturePart = cleanText;

        var matches = Regex.Matches(cleanText, @"\((?>[^()]+|\((?<c>)|\)(?<-c>))*(?(c)(?!))\)");
        if (matches.Count > 0)
        {
            var lastMatch = matches[matches.Count - 1];
            parenthesesContent = lastMatch.Value.Substring(1, lastMatch.Value.Length - 2).Trim();
            naturePart = cleanText.Replace(lastMatch.Value, "").Trim();
        }
        Console.WriteLine("cleanText: " + cleanText);
        Console.WriteLine("parenthesesContent: " + parenthesesContent);
        Console.WriteLine("naturePart: " + naturePart);
    }
}
