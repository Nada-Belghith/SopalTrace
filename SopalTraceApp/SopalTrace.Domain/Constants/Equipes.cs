using System.Linq;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Domain.Constants;

/// <summary>Codes d'équipe (table Team) : E1, E2, E3...</summary>
public static class Equipes
{
    /// <summary>Format accepté en entrée d'API.</summary>
    public const string FormatCode = "^E[1-9][0-9]?$";

    /// <summary>Normalise "E1", "e1", "1" ou "Equipe 1" en "E1".</summary>
    public static string Normaliser(string? valeur)
    {
        var chiffres = new string((valeur ?? string.Empty).Where(char.IsDigit).ToArray());
        if (!int.TryParse(chiffres, out var numero) || numero < 1)
            throw new RegleMetierException($"Équipe invalide : '{valeur}'.");
        return $"E{numero}";
    }

    /// <summary>Normalise une équipe facultative (vide = aucune équipe).</summary>
    public static string? NormaliserOuNull(string? valeur) =>
        string.IsNullOrWhiteSpace(valeur) ? null : Normaliser(valeur);

    /// <summary>Code de l'équipe placée en position <paramref name="index"/> (0 = première) d'un formulaire.</summary>
    public static string PourPosition(int index) => $"E{index + 1}";
}
