using System.Text.RegularExpressions;

namespace SopalTrace.Application.Helpers;

/// <summary>Nom d'un document : il porte sa version en suffixe (« Plan de contrôle TRONC V2 »).</summary>
public static class NomsDocument
{
    private static readonly Regex SuffixeVersion = new(@"([ -]*)V\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Remplace ou ajoute le suffixe « V{n} » du texte.</summary>
    public static string AvecVersion(string texte, int version)
    {
        if (string.IsNullOrWhiteSpace(texte)) return texte;
        return SuffixeVersion.IsMatch(texte)
            ? SuffixeVersion.Replace(texte, $"$1V{version}")
            : $"{texte.TrimEnd('-').TrimEnd()} V{version}";
    }
}
