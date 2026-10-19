using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services.Execution;

/// <summary>Équipe définie par le superviseur dans un document de contrôle au poste.</summary>
/// <param name="Code">Code de l'équipe (E1, E2…), celui de la session d'équipe.</param>
/// <param name="Nom">Nom affiché dans le document.</param>
/// <param name="Debut">Heure de début (0–23).</param>
/// <param name="Fin">Heure de fin (0–23), exclue ; inférieure au début pour une équipe de nuit.</param>
/// <param name="Ordre">Ordre d'affichage ; la première équipe ouvre la journée de production.</param>
public sealed record EquipePoste(string Code, string Nom, int Debut, int Fin, int Ordre);

/// <summary>
/// Équipes et heures du contrôle au poste : chaque équipe saisit seulement ses tranches horaires (HH-HH) ; le
/// document couvre une journée de production qui commence à l'heure de début de la première équipe (l'équipe
/// de nuit reste sur le même document).
/// </summary>
public static partial class EquipesControlePoste
{
    /// <summary>Équipes d'un document qui n'en définit pas (3 × 8 h).</summary>
    public static readonly IReadOnlyList<EquipePoste> ParDefaut = new[]
    {
        new EquipePoste("E1", "Équipe 1", 6, 14, 1),
        new EquipePoste("E2", "Équipe 2", 14, 22, 2),
        new EquipePoste("E3", "Équipe 3", 22, 6, 3)
    };

    [GeneratedRegex(@"^([01]\d|2[0-3])-([01]\d|2[0-3])$")]
    private static partial Regex FormatTranche();

    /// <summary>Équipes actives du formulaire du document, dans l'ordre ; à défaut, les 3 équipes standard.</summary>
    public static IReadOnlyList<EquipePoste> Depuis(IEnumerable<RefFormulaireEquipe> equipes)
    {
        var definies = equipes
            .Where(e => e.Actif)
            .OrderBy(e => e.OrdreAffiche)
            .Select(e => new EquipePoste(e.TeamCode, e.NomEquipe, e.HeureDebut, e.HeureFin, e.OrdreAffiche))
            .ToList();
        return definies.Count > 0 ? definies : ParDefaut;
    }

    /// <summary>Tranches horaires de l'équipe (06-07, 07-08…), à cheval sur minuit pour une équipe de nuit.</summary>
    public static List<string> Tranches(EquipePoste equipe)
    {
        var tranches = new List<string>();
        if (equipe.Debut == equipe.Fin) return tranches;
        for (var heure = equipe.Debut; heure != equipe.Fin; heure = (heure + 1) % 24)
            tranches.Add($"{heure:00}-{(heure + 1) % 24:00}");
        return tranches;
    }

    public static EquipePoste? Equipe(IEnumerable<EquipePoste> equipes, string? code) =>
        equipes.FirstOrDefault(e => string.Equals(e.Code, code, StringComparison.OrdinalIgnoreCase));

    /// <summary>Heure de début de la journée de production : celle de la première équipe.</summary>
    public static int HeureDebutJournee(IReadOnlyCollection<EquipePoste> equipes) =>
        equipes.OrderBy(e => e.Ordre).Select(e => e.Debut).DefaultIfEmpty(0).First();

    /// <summary>Jour de production d'un instant (ex. 02:00 le 14 avec une journée commençant à 06:00 → le 13).</summary>
    public static DateTime JourProduction(DateTime instant, int heureDebutJournee) =>
        instant.AddHours(-heureDebutJournee).Date;

    public static bool EstTranche(string? tranche) => tranche != null && FormatTranche().IsMatch(tranche);

    /// <summary>
    /// Erreur de configuration des équipes d'un document (null si valide) : code d'équipe connu et unique,
    /// heures de 0 à 23, début différent de la fin, aucune heure couverte par deux équipes.
    /// </summary>
    public static string? Erreur(IReadOnlyCollection<EquipePoste> equipes, IReadOnlyCollection<string> codesConnus)
    {
        foreach (var e in equipes)
        {
            if (string.IsNullOrWhiteSpace(e.Nom)) return "Chaque équipe doit avoir un nom.";
            if (!codesConnus.Contains(e.Code, StringComparer.OrdinalIgnoreCase))
                return $"L'équipe « {e.Nom} » doit être rattachée à une équipe connue ({string.Join(", ", codesConnus)}).";
            if (e.Debut is < 0 or > 23 || e.Fin is < 0 or > 23) return $"Heures de l'équipe « {e.Nom} » : de 0 à 23.";
            if (e.Debut == e.Fin) return $"L'équipe « {e.Nom} » doit commencer et finir à des heures différentes.";
        }
        var doublon = equipes.GroupBy(e => e.Code, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (doublon != null) return $"L'équipe {doublon.Key} est définie deux fois.";

        var heures = new Dictionary<string, string>();
        foreach (var e in equipes)
        foreach (var tranche in Tranches(e))
        {
            if (heures.TryGetValue(tranche, out var autre))
                return $"La tranche {tranche} est couverte par deux équipes ({autre} et {e.Code}).";
            heures[tranche] = e.Code;
        }
        return null;
    }
}
