using SopalTrace.Application.DTOs.QualityPlans.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

/// <summary>
/// Service centralisé pour la gestion des documents de types :
/// CTRL_POSTE, RESULTAT_CF, PLAN_ASS, PLAN_PF.
/// 
/// Règles métier appliquées :
/// - Pas de statut BROUILLON : tout document est créé directement en ACTIF.
/// - Version de départ : V1 (jamais V0).
/// - Création  → archive les actifs existants, crée en V1.
/// - Nouvelle version → archive l'actif, crée en V(n+1).
/// - Correction → modifie en place (même ID, même version).
/// </summary>
public interface IDocumentService
{
    // ── Lecture ───────────────────────────────────────────────────────────────

    Task<DocumentEnteteDto> GetDocumentByIdAsync(Guid id);

    Task<IReadOnlyList<DocumentEnteteDto>> GetDocumentsByFiltersAsync(
        string typeDocumentCode,
        string? natureComposantCode  = null,
        string? operationCode        = null,
        string? posteCode            = null,
        string? familleProduitCode   = null,
        string? statut               = null);

    // ── Écriture ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Création initiale d'un document.
    /// Si un document ACTIF existe déjà pour ce contexte, il est archivé.
    /// Le nouveau document est créé en V1 et statut ACTIF.
    /// </summary>
    Task<Guid> CreateDocumentAsync(CreateDocumentRequestDto request);

    /// <summary>
    /// Crée une nouvelle version d'un document existant.
    /// L'ancien document (AncienId) est archivé automatiquement.
    /// Le nouveau document est créé en V(n+1) et statut ACTIF.
    /// </summary>
    Task<Guid> CreateNewVersionAsync(NouvelleVersionDocumentRequestDto request);

    /// <summary>
    /// Correction mineure : modifie le document en place.
    /// Aucun changement de version, aucun archivage.
    /// </summary>
    Task<bool> UpdateDocumentAsync(Guid id, UpdateDocumentRequestDto request);

    /// <summary>
    /// Archive en cascade tous les documents liés à un formulaire donné.
    /// Appelé lors d'une nouvelle version d'un formulaire.
    /// </summary>
    Task ArchiveDocumentsByFormulaireAsync(Guid formulaireId);
}
