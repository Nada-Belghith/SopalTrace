using System;
using System.Collections.Generic;
using System.Linq;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services.Execution;

/// <summary>Réponse enregistrée sur une ligne du plan d'assemblage d'un poste (contexte : PREMIERE_PIECE, DERNIERE_PIECE, REGLAGE ou 100PCT).</summary>
public sealed record ReponseLot(Guid LigneId, string Contexte, bool NonConforme, DateTime DateSaisie, Guid? SessionId, string? Equipe,
    string? DetailsNc = null, string? ActionCorrective = null);

/// <summary>
/// Poste tenu par une session d'équipe (Exec_SessionPost) : démarré, fin de poste demandée, terminé. Tant qu'il
/// n'est pas terminé, aucune autre équipe ne peut le prendre.
/// </summary>
public sealed record DemarragePoste(Guid SessionId, string PosteCode, DateTime StartedAt, string? Equipe = null,
    DateTime? ClosingRequestedAt = null, DateTime? EndedAt = null)
{
    public bool EnCours => EndedAt == null;
}

/// <summary>
/// Session d'équipe vue depuis un poste de l'OF : chaque poste suit son propre parcours, comme si l'OF n'en avait
/// qu'un (sa fin de poste ne concerne que lui).
/// </summary>
/// <param name="SessionId">Session ouverte (Guid.Empty si l'OF n'a pas d'équipe).</param>
/// <param name="DemandeCloture">Clôture de l'OF (FIN_OF), fin de poste demandée ou faite sur ce poste (FIN_POSTE), ou null.</param>
/// <param name="PosteDemarre">L'équipe a démarré ce poste : contrôles de démarrage, puis de fin de poste.</param>
/// <param name="PremierDemarrage">Ce démarrage est le premier du poste sur l'OF : contrôle « début d'OF » (LOT_OF).</param>
/// <param name="PosteDemarreSurOf">Le poste a déjà été démarré sur l'OF : contrôle « fin d'OF » à la clôture.</param>
/// <param name="PosteTermine">L'équipe a terminé ce poste (ses autres postes continuent).</param>
public sealed record ContexteEquipe(Guid SessionId, string? DemandeCloture, bool PosteDemarre, bool PremierDemarrage, bool PosteDemarreSurOf,
    bool PosteTermine = false)
{
    public static readonly ContexteEquipe SansSession = new(Guid.Empty, null, false, false, false);

    /// <summary>Contexte de la session sur le poste, d'après les postes tenus enregistrés en base.</summary>
    public static ContexteEquipe Pour(ExecControleOfSession? session, string posteCode, IEnumerable<DemarragePoste> demarrages)
    {
        if (session == null) return SansSession;
        var duPoste = demarrages.Where(d => d.PosteCode == posteCode).OrderBy(d => d.StartedAt).ToList();
        var tenu = duPoste.FirstOrDefault(d => d.SessionId == session.Id);
        var demande = session.ClosingRequest == DemandesCloture.FinOf ? DemandesCloture.FinOf
            : tenu != null && (tenu.ClosingRequestedAt != null || tenu.EndedAt != null) ? DemandesCloture.FinPoste
            : null;
        return new ContexteEquipe(session.Id, demande, tenu != null, tenu != null && duPoste[0].SessionId == session.Id,
            duPoste.Count > 0, tenu?.EndedAt != null);
    }
}

/// <summary>
/// Règles des sections de lot du plan d'assemblage, poste par poste (une seule source, utilisée par l'écran du
/// plan et par l'avancement de la session) :
/// <list type="bullet">
/// <item>LOT_POSTE : première pièce à chaque démarrage du poste (chaque session), dernière pièce en fin de poste ;</item>
/// <item>LOT_OF : première pièce au premier démarrage du poste sur l'OF, dernière pièce à la clôture de l'OF
/// (sur chaque poste démarré au moins une fois, même par une équipe venue seulement clôturer).</item>
/// </list>
/// </summary>
public static class ControlesLot
{
    public static bool EstSectionLot(string? typeSection) => typeSection is SectionTypes.LotPoste or SectionTypes.LotOf;

    public static bool PremierePieceRequise(string typeSection, ContexteEquipe equipe) =>
        equipe.PosteDemarre && (typeSection == SectionTypes.LotPoste || equipe.PremierDemarrage);

    public static bool DernierePieceRequise(string typeSection, ContexteEquipe equipe) => typeSection == SectionTypes.LotPoste
        ? equipe.PosteDemarre && equipe.DemandeCloture != null
        : equipe.DemandeCloture == DemandesCloture.FinOf && equipe.PosteDemarreSurOf;

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
