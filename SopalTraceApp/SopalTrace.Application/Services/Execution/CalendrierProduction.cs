using System;
using System.Collections.Generic;
using System.Linq;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services.Execution;

/// <summary>Saisie d'un contrôle de réglage : toutes les lignes d'une section, conforme si aucune n'est NC.</summary>
public sealed record SaisieReglage(Guid SectionId, DateTime DateSaisie, bool Conforme);

/// <summary>
/// Calendrier des contrôles périodiques, dans des périodes de production : une pause, un réglage, la fin de
/// poste ou la clôture ferment la période, la reprise en ouvre une nouvelle et le calcul repart de zéro.
/// <list type="bullet">
/// <item>assemblage (ISO 2859-1) : par équipe et par poste, une pièce toutes les 60 / effectif par poste minutes
/// (fiche d'échantillonnage) ; un réglage commencé suspend les contrôles jusqu'à un réglage conforme sur tous
/// les critères ;</item>
/// <item>fabrication : par section du plan, selon sa périodicité horaire ; le réglage de l'OF les suspend.</item>
/// </list>
/// Une section de périodicité « série » n'est jamais planifiée dans le temps.
/// </summary>
public static class CalendrierProduction
{
    /// <summary>Minutes entre deux contrôles ; null sans effectif par poste (aucun contrôle planifié).</summary>
    public static double? IntervalleMinutes(int? effectifParPoste) =>
        effectifParPoste > 0 ? 60.0 / effectifParPoste.Value : null;

    /// <summary>
    /// Fabrication : minutes entre deux contrôles d'une section de périodicité horaire (N pièces toutes les H
    /// heures : 60 × H / N ; « 100 pièces » = toutes les pièces, relevé une fois par période). Null sinon.
    /// </summary>
    public static double? IntervalleSection(Periodicite? periodicite)
    {
        if (periodicite == null || periodicite.NbrPieces <= 0 ||
            !string.Equals(periodicite.Unite, UnitesPeriodicite.Heure, StringComparison.OrdinalIgnoreCase)) return null;
        var pieces = periodicite.NbrPieces == 100 ? 1 : periodicite.NbrPieces;
        return (periodicite.NbrHeures ?? 1) * 60.0 / pieces;
    }

    /// <summary>Une section de périodicité « série » n'est jamais planifiée dans le temps.</summary>
    public static bool EstPlanifiable(DocumentSection section) =>
        !string.Equals(section.Periodicite?.Unite, UnitesPeriodicite.Serie, StringComparison.OrdinalIgnoreCase);

    /// <summary>Heure du n-ième contrôle de la période (n à partir de 1).</summary>
    public static DateTime HeureControle(ExecProductionPeriod periode, int rang) =>
        periode.StartedAt.AddMinutes(periode.IntervalMinutes!.Value * rang);

    /// <summary>Contrôles devenus dus jusqu'à <paramref name="limite"/> (incluse), après ceux déjà planifiés.</summary>
    public static List<DateTime> ControlesDus(ExecProductionPeriod periode, DateTime limite)
    {
        var dus = new List<DateTime>();
        if (periode.IntervalMinutes is not > 0) return dus;
        // Garde-fou : jamais plus d'une journée de contrôles d'un coup.
        for (var rang = periode.PlannedControlCount + 1; dus.Count < 1440; rang++)
        {
            var heure = HeureControle(periode, rang);
            if (heure > limite) break;
            dus.Add(heure);
        }
        return dus;
    }

    /// <summary>Prochain contrôle d'une période ouverte.</summary>
    public static DateTime? ProchainControle(ExecProductionPeriod? periode) =>
        periode is { EndedAt: null, IntervalMinutes: > 0 } ? HeureControle(periode, periode.PlannedControlCount + 1) : null;

    /// <summary>Heure du dernier contrôle planifié dans la période (null si aucun).</summary>
    public static DateTime? DernierControle(ExecProductionPeriod periode) =>
        periode.PlannedControlCount > 0 && periode.IntervalMinutes > 0 ? HeureControle(periode, periode.PlannedControlCount) : null;

    /// <summary>
    /// Contrôle en retard : un contrôle suivant a déjà été planifié pour l'équipe sur le poste, ou un intervalle
    /// de production s'est écoulé depuis. Pendant une pause, le temps ne compte pas.
    /// </summary>
    public static bool EstEnRetard(DateTime heure, IReadOnlyCollection<ExecProductionPeriod> periodesPoste, DateTime maintenant)
    {
        var dernier = periodesPoste.Select(DernierControle).Max();
        if (dernier > heure) return true;
        var ouverte = periodesPoste.FirstOrDefault(p => p is { EndedAt: null, IntervalMinutes: > 0 });
        return ouverte != null && heure.AddMinutes(ouverte.IntervalMinutes!.Value) <= maintenant;
    }

    /// <summary>Tranche horaire d'un contrôle (H_09_10).</summary>
    public static string Tranche(DateTime heure) => $"H_{heure.Hour:00}_{(heure.Hour + 1) % 24:00}";

    /// <summary>Saisies de réglage de la session, par section (une saisie = toutes les lignes enregistrées ensemble).</summary>
    public static List<SaisieReglage> SaisiesReglage(IEnumerable<ReponseLot> reponses, Guid sessionId,
        IReadOnlyDictionary<Guid, Guid> sectionParLigne) =>
        reponses
            .Where(r => r.Contexte == ContextesReponse.Reglage && r.SessionId == sessionId && sectionParLigne.ContainsKey(r.LigneId))
            .GroupBy(r => (Section: sectionParLigne[r.LigneId], r.DateSaisie))
            .Select(g => new SaisieReglage(g.Key.Section, g.Key.DateSaisie, !g.Any(r => r.NonConforme)))
            .OrderBy(s => s.DateSaisie)
            .ToList();

    /// <summary>
    /// Fin d'un réglage commencé à <paramref name="debut"/> : chaque section de réglage a, depuis, une dernière
    /// saisie conforme sur tous ses critères. Null tant que le réglage est en cours.
    /// </summary>
    public static DateTime? FinReglage(DateTime debut, IReadOnlyCollection<SaisieReglage> saisies, IReadOnlyCollection<Guid> sectionsReglage)
    {
        DateTime fin = debut;
        foreach (var section in sectionsReglage)
        {
            var derniere = saisies.Where(s => s.SectionId == section && s.DateSaisie >= debut).MaxBy(s => s.DateSaisie);
            if (derniere is not { Conforme: true }) return null;
            if (derniere.DateSaisie > fin) fin = derniere.DateSaisie;
        }
        return fin;
    }

    /// <summary>Réglage en cours sur le poste : la dernière période s'est fermée sur un réglage pas encore conforme.</summary>
    public static bool ReglageEnCours(IReadOnlyCollection<ExecProductionPeriod> periodesPoste,
        IReadOnlyCollection<SaisieReglage> saisies, IReadOnlyCollection<Guid> sectionsReglage)
    {
        var derniere = periodesPoste.OrderBy(p => p.StartedAt).ThenBy(p => p.EndedAt ?? DateTime.MaxValue).LastOrDefault();
        return derniere is { EndReason: MotifsFinPeriode.Reglage, EndedAt: DateTime fin } &&
               FinReglage(fin, saisies, sectionsReglage) == null;
    }
}
