using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SopalTrace.Application.Helpers;

/// <summary>
/// Comparaison tolérante des libellés saisis à la main (Excel, formulaires) avec ceux des référentiels :
/// casse, accents, apostrophes typographiques et espaces multiples sont ignorés.
/// </summary>
public static class TexteReference
{
    private static readonly Regex Espaces = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Forme de comparaison : « Banc d’essai » et « banc  d'ESSAI » donnent la même clé.</summary>
    public static string Cle(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte)) return string.Empty;

        var decompose = texte.Replace('’', '\'').Replace('‘', '\'').Replace('`', '\'')
                             .Normalize(NormalizationForm.FormD);
        var sansAccents = new string(decompose
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        return Espaces.Replace(sansAccents, " ").Trim().ToLowerInvariant();
    }

    /// <summary>Libellé sans la précision entre parenthèses finale : « P à C (Pied à coulisse) » → « P à C ».</summary>
    public static string SansPrecisionFinale(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte)) return string.Empty;
        return Regex.Replace(texte, @"\s*\([^()]*\)\s*$", "").Trim();
    }

    /// <summary>Espaces et retours à la ligne réduits à un seul espace.</summary>
    public static string SurUneLigne(string? texte)
        => string.IsNullOrWhiteSpace(texte) ? string.Empty : Espaces.Replace(texte, " ").Trim();

    /// <summary>Coupe le texte à la longueur de la colonne SQL.</summary>
    public static string Tronquer(string texte, int longueurMax)
        => texte.Length <= longueurMax ? texte : texte.Substring(0, longueurMax).TrimEnd();
}
