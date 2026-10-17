using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

/// <summary>
/// Service centralisé pour les documents de types : CTRL_POSTE, RESULTAT_CF, PLAN_ASS, PLAN_PF.
/// 
/// Règles métier :
/// - Pas de BROUILLON : tout document est créé directement en ACTIF.
/// - Première version : V1 (jamais V0).
/// - Chaque méthode publique délègue à des méthodes privées atomiques (SRP).
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DocumentService> _logger;
    private readonly IFormulaireStructureService _formulaireStructureService;
    private readonly IEnumerable<IDocumentTypeStrategy> _strategies;

    public DocumentService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DocumentService> logger,
        IFormulaireStructureService formulaireStructureService,
        IEnumerable<IDocumentTypeStrategy> strategies)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
        _formulaireStructureService = formulaireStructureService;
        _strategies = strategies;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MÉTHODES PUBLIQUES (orchestratrices)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    /// <remarks>
    /// Tout ou rien : le formulaire (nouvelle version, archivage de l'ancien document) et le document s'enregistrent dans
    /// une même transaction ; un document refusé (valeur de colonne invalide…) ne laisse aucun changement.
    /// </remarks>
    public Task<Guid> CreateDocumentAsync(CreateDocumentRequestDto request) =>
        _unitOfWork.ExecuteInTransactionAsync(() => CreerDocumentAsync(request));

    /// <inheritdoc/>
    public Task<Guid> CreateNewVersionAsync(NouvelleVersionDocumentRequestDto request) =>
        _unitOfWork.ExecuteInTransactionAsync(() => CreerNouvelleVersionAsync(request));

    private async Task<Guid> CreerDocumentAsync(CreateDocumentRequestDto request)
    {
        var user = _currentUserService.MatriculeRequis;

        // Formulaire du document : celui indiqué, sinon celui de son champ d'application (famille, poste…). Les doublons
        // ne sont cherchés que parmi les documents de ce formulaire (PAS78 et PAS78 soupape ne s'influencent pas).
        var effectiveFormCodeRef = await CodeFormulaireAsync(request.TypeDocumentCode, request.RefFormulaireCodeReference,
            request.FamilleProduitFiniCode, request.PosteCode, request.NatureArticleCode);

        var docsExistants = await ChargerDocsExistantsAsync(
            request.TypeDocumentCode, request.NatureArticleCode,
            request.OperationCode, request.PosteCode, request.FamilleProduitFiniCode);

        // S'il y a des documents actifs liés au MÊME formulaire (même CodeReference),
        // la création doit forcer l'archivage de l'ancien via le FormulaireStructureService.
        // On filtre par formulaire exact pour éviter que PAS78 et PAS78_SOUPAPE se perturbent.
        bool forceNouvelleVersion = docsExistants.Any(d =>
            d.Statut == DocumentStatuts.Actif &&
            (effectiveFormCodeRef == null || string.Equals(d.Formulaire?.CodeReference, effectiveFormCodeRef, StringComparison.OrdinalIgnoreCase)));

        var (formulaireId, formCodeRef, formVersion, formStatut) = await ResolverFormulaireAsync(
            request.TypeDocumentCode, effectiveFormCodeRef,
            request.ConfigurationColonnesJson, request.ColonneDefs,
            forceNouvelleVersion: forceNouvelleVersion);

        var entite = BuildDocumentEntite(request, user, formulaireId, formVersion);
        await AppliquerChampFormulaireAsync(entite, formulaireId);
        entite.Statut = formStatut;
        NommerAvecVersion(entite);

        await SyncExtraColonnesAsync(entite, formulaireId);
        await CompleterReferencesAsync(entite.DocumentSections);
        await PersisterNouveauDocumentAsync(entite);

        return entite.Id;
    }

    private async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionDocumentRequestDto request)
    {
        var user = _currentUserService.MatriculeRequis;

        var ancienDoc = await ChargerDocumentOuLeverAsync(request.AncienId);
        _unitOfWork.VerifierVersionDocument(ancienDoc, request.RowVersion, obligatoire: false);

        // Le FormulaireStructureService gère l'archivage automatique des documents liés
        // lorsque le formulaire passe à une nouvelle version.

        // Colonnes de l'ancienne version : une colonne retirée peut encore avoir des valeurs dans les lignes envoyées.
        var colonnesPrecedentes = ancienDoc.FormulaireId is { } ancienFormulaire
            ? (await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(ancienFormulaire) ?? new())
                .Select(c => c.CleColonne).ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        // Une nouvelle version reste sur le formulaire de l'ancien document (sauf formulaire indiqué).
        var (formulaireId, _, formVersion, formStatut) = await ResolverFormulaireAsync(
            request.TypeDocumentCode,
            request.RefFormulaireCodeReference ?? await GetCodeRefDepuisDocAsync(ancienDoc)
                ?? await CodeFormulaireAsync(request.TypeDocumentCode, null, request.FamilleProduitFiniCode, request.PosteCode, request.NatureArticleCode),
            request.ConfigurationColonnesJson, request.ColonneDefs,
            forceNouvelleVersion: true);

        var nouveauDoc = BuildDocumentEntite(request, user, formulaireId, formVersion);
        await AppliquerChampFormulaireAsync(nouveauDoc, formulaireId);
        nouveauDoc.Statut = formStatut;
        NommerAvecVersion(nouveauDoc);

        await SyncExtraColonnesAsync(nouveauDoc, formulaireId, colonnesPrecedentes);
        await CompleterReferencesAsync(nouveauDoc.DocumentSections);
        await PersisterNouveauDocumentAsync(nouveauDoc);

        return nouveauDoc.Id;
    }

    public async Task<string?> UpdateDocumentAsync(Guid id, UpdateDocumentRequestDto request)
    {
        var doc = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (doc == null) return null;
        _unitOfWork.VerifierVersionDocument(doc, request.RowVersion, obligatoire: true);

        DocumentMapper.UpdateEntityScalaires(doc, request);
        
        // Mise à jour de la structure du formulaire en mode "Correction Mineure"
        await ResolverFormulaireAsync(
            doc.TypeDocumentCode, doc.Formulaire?.CodeReference,
            request.ConfigurationColonnesJson, null,
            isCorrectionMineure: true);

        MergerSections(doc, request.Sections);
        await CompleterReferencesAsync(doc.DocumentSections);

        await _unitOfWork.DocumentEnteteRepository.UpdateAsync(doc);
        await _unitOfWork.CommitAsync();

        return VersionsDocument.EnTexte(doc.RowVersion) ?? string.Empty;
    }

    /// <inheritdoc/>
    public async Task ArchiveDocumentsByFormulaireAsync(Guid formulaireId)
    {
        var documents = await _unitOfWork.DocumentEnteteRepository.GetByFormulaireIdAsync(formulaireId);
        foreach (var doc in documents.Where(d => d.Statut == DocumentStatuts.Actif))
        {
            doc.Statut = DocumentStatuts.Archive;
            await _unitOfWork.DocumentEnteteRepository.UpdateAsync(doc);
        }
    }

    /// <inheritdoc/>
    public async Task<DocumentEnteteDto> GetDocumentByIdAsync(Guid id)
    {
        var entite = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (entite == null) throw new Exception("Document introuvable.");

        var cols = await ChargerColonnesFormulaireAsync(entite);
        var dto = DocumentMapper.ToDto(entite, cols);

        var strategy = _strategies.FirstOrDefault(s => s.DocumentTypeCode == entite.TypeDocumentCode);
        if (strategy != null)
            await strategy.PopulateDtoAsync(entite, dto);

        return dto;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DocumentEnteteDto>> GetDocumentsByFiltersAsync(
        string typeDocumentCode,
        string? natureComposantCode  = null,
        string? operationCode        = null,
        string? posteCode            = null,
        string? familleProduitCode   = null,
        string? statut               = null,
        string? refFormulaireCodeReference = null)
    {
        var result = await _unitOfWork.DocumentEnteteRepository.GetByFiltersAsync(
            typeDocumentCode, natureComposantCode, operationCode, posteCode, familleProduitCode, statut, refFormulaireCodeReference);

        var dtos = new List<DocumentEnteteDto>();
        foreach (var doc in result)
        {
            var cols = await ChargerColonnesFormulaireAsync(doc);
            dtos.Add(DocumentMapper.ToDto(doc, cols));
        }
        return dtos;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MÉTHODES PRIVÉES ATOMIQUES (une responsabilité chacune)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Charge tous les documents existants pour un contexte donné (tous statuts).</summary>
    private async Task<IEnumerable<DocumentEntete>> ChargerDocsExistantsAsync(
        string typeCode, string? natureCode, string? opCode, string? posteCode, string? familleCode)
        => await _unitOfWork.DocumentEnteteRepository.GetByFiltersAsync(
            typeCode, natureCode, opCode, posteCode, familleCode);

    /// <summary>
    /// Résout le formulaire associé (RefFormulaire).
    /// Retourne (formulaireId, codeReference) en créant ou mettant à jour le formulaire si nécessaire.
    /// </summary>
    /// <summary>
    /// Code du formulaire d'un document : celui indiqué, sinon celui de son champ d'application (jamais fabriqué). Null pour
    /// un type de document sans formulaire de ce service.
    /// </summary>
    private async Task<string?> CodeFormulaireAsync(string typeCode, string? codeIndique, string? familleCode, string? posteCode, string? natureCode)
    {
        var champ = ChampsFormulaire.PourDocument(typeCode, familleCode, posteCode, null, natureCode);
        return champ == null ? codeIndique : await _formulaireStructureService.CodeFormulaireAsync(champ, codeIndique);
    }

    private async Task<(Guid? formulaireId, string? codeRef, int version, string statut)> ResolverFormulaireAsync(
        string typeCode,
        string? formCodeRef,
        string? configColonnesJson,
        IEnumerable<ColonneJsonDto>? colonneDefs,
        bool isCorrectionMineure = false,
        bool forceNouvelleVersion = false)
    {
        int version = 0;
        string statut = "BROUILLON";

        if (string.IsNullOrWhiteSpace(formCodeRef))
            return (null, null, version, statut);

        var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(formCodeRef);
        string role = form?.Role ?? RolesFormulaire.PourTypeDocument(typeCode);

        var colsJson = BuildColonnesJson(configColonnesJson, colonneDefs);

        var result = await _formulaireStructureService.UpdateFormulaireStructureAsync(
            role, colsJson, formCodeRef, isCorrectionMineure, forceNouvelleVersion);

        Guid? formulaireId = null;
        if (result.HasValue)
        {
            formulaireId = result.Value.Id;
        }
        else if (form != null)
        {
            formulaireId = form.Id;
        }

        if (formulaireId.HasValue)
        {
            var finalForm = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId.Value);
            if (finalForm != null)
            {
                version = finalForm.Version;
                statut = finalForm.Statut ?? "BROUILLON";
            }
        }

        // Les documents de ce service sont génériques (sans article) : ils n'ont pas de brouillon,
        // ils sont actifs dès leur création puis archivés par une nouvelle version.
        if (statut == DocumentStatuts.Brouillon)
        {
            if (formulaireId.HasValue) await _formulaireStructureService.ActiverFormulaireAsync(formulaireId.Value);
            statut = DocumentStatuts.Actif;
        }

        return (formulaireId, formCodeRef, version, statut);
    }


    /// <summary>Passe un document au statut ARCHIVE et le persiste.</summary>
    private async Task ArchiverDocumentAsync(DocumentEntete doc)
    {
        doc.Statut = DocumentStatuts.Archive;
        await _unitOfWork.DocumentEnteteRepository.UpdateAsync(doc);
    }

    /// <summary>
    /// Construit une entité DocumentEntete à partir du DTO.
    /// Toujours ACTIF, version passée en paramètre.
    /// </summary>
    /// <summary>
    /// Champ d'application du formulaire : le document reprend son poste et, si elle est renseignée, sa famille
    /// (ex. « PAS71 soupape » : PAS71, famille RBGFA_SOUPAPE). Un poste ou une famille contraires sont refusés.
    /// </summary>
    private async Task AppliquerChampFormulaireAsync(DocumentEntete document, Guid? formulaireId)
    {
        if (!formulaireId.HasValue) return;
        var formulaire = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId.Value);
        if (formulaire == null) return;

        if (!string.IsNullOrEmpty(formulaire.PosteCode))
        {
            if (!string.IsNullOrEmpty(document.PosteCode) && !string.Equals(document.PosteCode, formulaire.PosteCode, StringComparison.OrdinalIgnoreCase))
                throw new RegleMetierException(
                    $"Le formulaire {formulaire.CodeReference} est celui du poste {formulaire.PosteCode} : il ne peut pas servir au poste {document.PosteCode}.");
            document.PosteCode = formulaire.PosteCode;
        }
        if (!string.IsNullOrEmpty(formulaire.FamilleProduitFiniCode))
        {
            if (!string.IsNullOrEmpty(document.FamilleProduitFiniCode) &&
                !string.Equals(document.FamilleProduitFiniCode, formulaire.FamilleProduitFiniCode, StringComparison.OrdinalIgnoreCase))
                throw new RegleMetierException(
                    $"Le formulaire {formulaire.CodeReference} est celui de la famille {formulaire.FamilleProduitFiniCode} : il ne peut pas servir à la famille {document.FamilleProduitFiniCode}.");
            document.FamilleProduitFiniCode = formulaire.FamilleProduitFiniCode;
        }
    }

    private static DocumentEntete BuildDocumentEntite(
        CreateDocumentRequestDto request,
        string user,
        Guid? formulaireId,
        int version)
    {
        var entite = DocumentMapper.ToEntity(request, user, formulaireId);
        entite.Version = version;
        entite.Statut  = DocumentStatuts.Actif;
        return entite;
    }

    /// <summary>
    /// Met à jour le nom et la désignation du document pour intégrer le numéro de version.
    /// Ex : "Plan de Contrôle TRONC" → "Plan de Contrôle TRONC V2"
    /// </summary>
    private static void NommerAvecVersion(DocumentEntete doc)
    {
        doc.Nom = NomsDocument.AvecVersion(doc.Nom, doc.Version);
        if (!string.IsNullOrWhiteSpace(doc.Designation))
            doc.Designation = NomsDocument.AvecVersion(doc.Designation, doc.Version);
    }

    /// <summary>
    /// Ajoute les colonnes extra manquantes sur toutes les lignes de l'entité. Nouvelle version : les valeurs d'une
    /// colonne de l'ancienne version retirée du formulaire sont abandonnées (une colonne inconnue reste refusée par la base).
    /// </summary>
    private async Task SyncExtraColonnesAsync(DocumentEntete entite, Guid? formulaireId, IReadOnlySet<string>? colonnesPrecedentes = null)
    {
        if (!formulaireId.HasValue) return;

        var activeCols = await _unitOfWork.RefFormulaireRepository
            .GetColonnesActivesByFormulaireIdAsync(formulaireId.Value) ?? new();

        if (colonnesPrecedentes is { Count: > 0 })
        {
            var cles = activeCols.Select(c => c.CleColonne).ToHashSet(StringComparer.Ordinal);
            foreach (var ligne in entite.DocumentSections.SelectMany(s => s.DocumentLignes))
            foreach (var retiree in ligne.DocumentLigneExtraColonnes
                         .Where(ec => colonnesPrecedentes.Contains(ec.CleColonne) && !cles.Contains(ec.CleColonne)).ToList())
                ligne.DocumentLigneExtraColonnes.Remove(retiree);
        }

        if (activeCols.Count == 0) return;

        foreach (var section in entite.DocumentSections)
        foreach (var ligne   in section.DocumentLignes)
        foreach (var colDef  in activeCols)
        {
            if (!ligne.DocumentLigneExtraColonnes.Any(ec => ec.CleColonne == colDef.CleColonne))
            {
                ligne.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                {
                    Id            = Guid.NewGuid(),
                    LigneId       = ligne.Id,
                    CleColonne    = colDef.CleColonne,
                    ValeurColonne = null,
                    OrdreAffiche  = 0
                });
            }
        }
    }

    /// <summary>Rattache ou crée les référentiels des lignes (caractéristiques, moyens, instruments).</summary>
    private Task CompleterReferencesAsync(ICollection<DocumentSection> sections)
        => ReferencesLignesQualite.CompleterAsync(sections, _unitOfWork.DictionnaireQualiteRepository);

    /// <summary>Ajoute l'entité en base et commite.</summary>
    private async Task PersisterNouveauDocumentAsync(DocumentEntete entite)
    {
        await _unitOfWork.DocumentEnteteRepository.AddAsync(entite);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>Charge le document ou lève une exception si introuvable.</summary>
    private async Task<DocumentEntete> ChargerDocumentOuLeverAsync(Guid id)
    {
        var doc = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (doc == null) throw new Exception($"Document introuvable (Id={id}).");
        return doc;
    }

    /// <summary>Fusionne les sections (ajout, mise à jour, suppression) sur un document existant.</summary>
    private void MergerSections(DocumentEntete doc, List<CreateDocumentSectionDto> sectionsDto)
    {
        Utilities.SectionUpdateHelper.UpdateSections(
            doc.DocumentSections,
            sectionsDto,
            sec => _unitOfWork.DocumentEnteteRepository.RemoveSection(sec),
            lig => _unitOfWork.DocumentEnteteRepository.RemoveLigne(lig),
            dto => dto.Id,
            dto => dto.Id,
            sec => sec.Id,
            lig => lig.Id,
            dto => DocumentMapper.CreateSection(dto, doc.Id),
            (sec, dto) => DocumentMapper.UpdateSection(sec, dto),
            sec => sec.DocumentLignes,
            dto => dto.Lignes,
            (dto, sec) => DocumentMapper.CreateLigne(dto, doc.Id, sec.Id),
            (lig, dto) => 
            {
                DocumentMapper.UpdateLigne(lig, dto);
                MergerExtraColonnes(lig, dto.ExtraColonnes ?? new());
            },
            ajouter: _unitOfWork.Ajouter);
    }

    /// <summary>Fusionne les colonnes extra d'une ligne (ajout, mise à jour, suppression).</summary>
    private void MergerExtraColonnes(DocumentLigne lig, List<CreateDocumentExtraColonneDto> incoming)
    {
        var existing = lig.DocumentLigneExtraColonnes.ToList();

        foreach (var ec in existing.Where(e => !incoming.Any(i => i.CleColonne == e.CleColonne)))
        {
            _unitOfWork.DocumentEnteteRepository.RemoveExtraColonne(ec);
            lig.DocumentLigneExtraColonnes.Remove(ec);
        }

        foreach (var inc in incoming)
        {
            var found = existing.FirstOrDefault(e => e.CleColonne == inc.CleColonne);
            if (found != null)
            {
                found.ValeurColonne = inc.ValeurColonne;
                found.OrdreAffiche  = inc.OrdreAffiche;
            }
            else
            {
                var nouvelle = new DocumentLigneExtraColonne
                {
                    Id            = Guid.NewGuid(),
                    LigneId       = lig.Id,
                    CleColonne    = inc.CleColonne,
                    ValeurColonne = inc.ValeurColonne,
                    OrdreAffiche  = inc.OrdreAffiche
                };
                lig.DocumentLigneExtraColonnes.Add(nouvelle);
                _unitOfWork.Ajouter(nouvelle);
            }
        }
    }

    /// <summary>Charge les colonnes actives du formulaire associé à un document.</summary>
    private async Task<List<RefFormulaireColonneDef>> ChargerColonnesFormulaireAsync(DocumentEntete doc)
    {
        if (doc.FormulaireId.HasValue)
            return await _unitOfWork.RefFormulaireRepository
                .GetColonnesActivesByFormulaireIdAsync(doc.FormulaireId.Value);

        var form = await _unitOfWork.RefFormulaireRepository
            .GetFormulaireActifByCodeReferenceAsync(doc.TypeDocumentCode);

        return form != null
            ? await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(form.Id)
            : new List<RefFormulaireColonneDef>();
    }

    /// <summary>Récupère le code de référence du formulaire depuis un document existant.</summary>
    private async Task<string?> GetCodeRefDepuisDocAsync(DocumentEntete doc)
    {
        if (!doc.FormulaireId.HasValue) return null;
        var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(doc.FormulaireId.Value);
        return form?.CodeReference;
    }

    /// <summary>Construit le JSON des colonnes à partir du JSON ou de la liste de ColonneDefs.</summary>
    private static string? BuildColonnesJson(string? configColonnesJson, IEnumerable<ColonneJsonDto>? colonneDefs)
    {
        if (!string.IsNullOrWhiteSpace(configColonnesJson)) return configColonnesJson;
        if (colonneDefs != null && colonneDefs.Any())
        {
            return System.Text.Json.JsonSerializer.Serialize(
                colonneDefs,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
        }
        return null;
    }
}
