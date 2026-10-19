using System;
using System.Globalization;

namespace SopalTrace.Application.Helpers;

/// <summary>
/// Cotes nominales du tronçonnage (longueur, diamètre). Une ligne de plan les désigne par son libellé
/// (« Longueur = 120 », « Diamètre = 32 ») : la valeur après « = » est la cote du plan, remplacée à l'affichage
/// par celle saisie au démarrage de l'OF. Le plan lui-même n'est jamais modifié.
/// </summary>
public static class CotesTronconnage
{
    public static bool EstLongueur(string? libelle)
        => libelle != null && libelle.Contains("longueur", StringComparison.OrdinalIgnoreCase);

    public static bool EstDiametre(string? libelle)
        => libelle != null && (libelle.Contains("diametre", StringComparison.OrdinalIgnoreCase)
                               || libelle.Contains("diamètre", StringComparison.OrdinalIgnoreCase));

    /// <summary>Cote écrite dans le libellé (« … = 120 »), ou null.</summary>
    public static decimal? Lire(string? libelle)
    {
        var parties = libelle?.Split('=');
        return parties is { Length: > 1 }
               && decimal.TryParse(parties[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var valeur)
            ? valeur
            : null;
    }

    /// <summary>Libellé affiché pendant l'OF : la cote saisie au démarrage remplace celle du plan.</summary>
    public static string Appliquer(string libelle, decimal? longueur, decimal? diametre)
    {
        var cote = EstLongueur(libelle) ? longueur : EstDiametre(libelle) ? diametre : null;
        return cote.HasValue
            ? $"{libelle.Split('=')[0].TrimEnd()} = {cote.Value.ToString("0.###", CultureInfo.InvariantCulture)}"
            : libelle;
    }
}
