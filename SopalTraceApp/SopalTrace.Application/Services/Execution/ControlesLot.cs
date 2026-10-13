using System;
using System.Collections.Generic;
using System.Linq;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Application.Services.Execution;

/// <summary>Réponse enregistrée sur une ligne du plan d'assemblage d'un poste (contexte : PREMIERE_PIECE, DERNIERE_PIECE, REGLAGE ou 100PCT).</summary>
public sealed record ReponseLot(Guid LigneId, string Contexte, bool NonConforme, DateTime DateSaisie, Guid? SessionId, string? Equipe,
    string? DetailsNc = null, string? ActionCorrective = null);

/// <summary>Session d'équipe vue par les contrôles de lot.</summary>
/// <param name="SessionId">Session active (Guid.Empty si l'OF n'a plus d'équipe active).</param>
/// <param name="PremiereEquipe">La session active est la première de l'OF.</param>
/// <param name="DemandeCloture">FIN_POSTE, FIN_OF ou null.</param>
public sealed record ContexteEquipe(Guid SessionId, bool PremiereEquipe, string? DemandeCloture);

/// <summary>
/// Règles des sections de lot du plan d'assemblage (une seule source, utilisée par l'écran du plan et par
/// l'avancement de la session) :
/// <list type="bullet">
/// <item>LOT_POSTE : première pièce au début de chaque poste (chaque équipe), dernière pièce en fin de poste ;</item>
/// <item>LOT_OF : première pièce pour la première équipe de l'OF seulement (les suivantes trouvent l'OF démarré),
/// dernière pièce à la clôture de l'OF.</item>
/// </list>
/// </summary>
public static class ControlesLot
{
    public static bool EstSectionLot(string? typeSection) => typeSection is SectionTypes.LotPoste or SectionTypes.LotOf;

    public static bool PremierePieceRequise(string typeSection, ContexteEquipe equipe) =>
        typeSection == SectionTypes.LotPoste || equipe.PremiereEquipe;

    public static bool DernierePieceRequise(string typeSection, ContexteEquipe equipe) => typeSection == SectionTypes.LotPoste
        ? equipe.DemandeCloture != null
        : equipe.DemandeCloture == DemandesCloture.FinOf;

    public static bool EstSectionReglage(string? typeSection) => typeSection is SectionTypes.Reglage or SectionTypes.ReglageProd;

    /// <summary>
    /// Contexte de saisie d'une section contrôlée à la demande de l'opérateur (réglage : REGLAGE,
    /// toutes les pièces : 100PCT) ; null pour les autres sections.
    /// </summary>
    public static string? ContexteALaDemande(string? typeSection) =>
        EstSectionReglage(typeSection) ? ContextesReponse.Reglage
        : typeSection == SectionTypes.TousPieces ? ContextesReponse.CentPourCent
        : null;

    /// <summary>Réponses prises en compte : celles de la session pour LOT_POSTE, de toutes les sessions pour LOT_OF.</summary>
    public static Guid? PorteeLot(string typeSection, ContexteEquipe equipe) =>
        typeSection == SectionTypes.LotOf ? null : equipe.SessionId;

    /// <summary>
    /// État d'un contrôle : fait lorsque chaque caractéristique a une réponse pour ce contexte (dans la session
    /// donnée, ou dans toutes les sessions si <paramref name="sessionId"/> est null) ; le résultat retient la
    /// dernière réponse par ligne.
    /// </summary>
    public static ControleLotDto Etat(IReadOnlyCollection<Guid> ligneIds, string contexte, bool requis,
        Guid? sessionId, IEnumerable<ReponseLot> reponses)
    {
        var etat = new ControleLotDto { Requis = requis };
        if (ligneIds.Count == 0) return etat;

        var pertinentes = reponses
            .Where(r => r.Contexte == contexte && (sessionId == null || r.SessionId == sessionId))
            .ToList();
        var dernieres = ligneIds
            .Select(id => pertinentes.Where(r => r.LigneId == id).MaxBy(r => r.DateSaisie))
            .ToList();

        // Dernière réponse de chaque ligne déjà saisie : l'écran de saisie repart de ces réponses.
        etat.Lignes = dernieres.Where(r => r != null).Select(r => new ReponseLigneLotDto
        {
            LigneId = r!.LigneId,
            Resultat = r.NonConforme ? ResultatsControle.NonConforme : ResultatsControle.Conforme,
            Nc = r.DetailsNc,
            ActionCorrective = r.ActionCorrective
        }).ToList();
        if (dernieres.Any(r => r == null)) return etat;

        var plusRecente = dernieres.MaxBy(r => r!.DateSaisie)!;
        etat.Fait = true;
        etat.Resultat = dernieres.Any(r => r!.NonConforme) ? ResultatsControle.NonConforme : ResultatsControle.Conforme;
        etat.DateSaisie = plusRecente.DateSaisie;
        etat.Equipe = plusRecente.Equipe;
        return etat;
    }
}
