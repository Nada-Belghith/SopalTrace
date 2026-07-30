using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
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
    public async Task<Guid> CreateDocumentAsync(CreateDocumentRequestDto request)
    {
        var user = _currentUserService.UserInfo;

        // Calculer d'abord le formCodeRef effectif (même logique que ResolverFormulaireAsync)
        // afin de ne chercher les doublons que parmi les documents liés au MÊME formulaire.
        // Cela évite le cas où FE-RC-PAS78 et FE-RC-PAS78_SOUPAPE s'influencent mutuellement.
        var effectiveFormCodeRef = !string.IsNullOrWhiteSpace(request.RefFormulaireCodeReference)
            ? request.RefFormulaireCodeReference
            : request.TypeDocumentCode switch
            {
                TypesDocument.ControlePoste   when !string.IsNullOrWhiteSpace(request.PosteCode)             => $"FE-RC-{request.PosteCode.Trim()}",
                TypesDocument.PlanProduitFini when !string.IsNullOrWhiteSpace(request.FamilleProduitFiniCode) => $"FE-PF-{request.FamilleProduitFiniCode.Trim()}",
                TypesDocument.PlanAssemblage  when !string.IsNullOrWhiteSpace(request.NatureArticleCode)     => $"FE-ASS-{request.NatureArticleCode.Trim()}",
                _ => null
            };

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
            request.TypeDocumentCode, request.RefFormulaireCodeReference,
            request.PosteCode, request.FamilleProduitFiniCode, request.NatureArticleCode,
            request.ConfigurationColonnesJson, request.ColonneDefs, versionInitiale: 0,
            forceNouvelleVersion: forceNouvelleVersion);

        var entite = BuildDocumentEntite(request, user, formulaireId, formVersion);
        entite.Statut = formStatut;
        NommerAvecVersion(entite);

        await SyncExtraColonnesAsync(entite, formulaireId);
        await AppliquerSmartDictionaryAsync(entite.DocumentSections);
        await PersisterNouveauDocumentAsync(entite);

        return entite.Id;
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateNewVersionAsync(NouvelleVersionDocumentRequestDto request)
    {
        var user = _currentUserService.UserInfo;

        var ancienDoc = await ChargerDocumentOuLeverAsync(request.AncienId);

        // Le FormulaireStructureService gère l'archivage automatique des documents liés
        // lorsque le formulaire passe à une nouvelle version.

        var (formulaireId, _, formVersion, formStatut) = await ResolverFormulaireAsync(
            request.TypeDocumentCode,
            request.RefFormulaireCodeReference ?? await GetCodeRefDepuisDocAsync(ancienDoc),
            request.PosteCode, request.FamilleProduitFiniCode, request.NatureArticleCode,
            request.ConfigurationColonnesJson, request.ColonneDefs,
            versionInitiale: 0,
            forceNouvelleVersion: true);

        var nouveauDoc = BuildDocumentEntite(request, user, formulaireId, formVersion);
        nouveauDoc.Statut = formStatut;
        NommerAvecVersion(nouveauDoc);

        await SyncExtraColonnesAsync(nouveauDoc, formulaireId);
        await AppliquerSmartDictionaryAsync(nouveauDoc.DocumentSections);
        await PersisterNouveauDocumentAsync(nouveauDoc);

        return nouveauDoc.Id;
    }

    public async Task<bool> UpdateDocumentAsync(Guid id, UpdateDocumentRequestDto request)
    {
        var doc = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (doc == null) return false;

        DocumentMapper.UpdateEntityScalaires(doc, request);
        
        // Mise à jour de la structure du formulaire en mode "Correction Mineure"
        await ResolverFormulaireAsync(
            doc.TypeDocumentCode, doc.Formulaire?.CodeReference,
            doc.PosteCode, doc.FamilleProduitFiniCode, doc.NatureArticleCode,
            request.ConfigurationColonnesJson, null, doc.Version,
            isCorrectionMineure: true);

        AppliquerStrategieType(doc, request.ConfigurationColonnesJson);
        MergerSections(doc, request.Sections);

        await _unitOfWork.DocumentEnteteRepository.UpdateAsync(doc);
        await _unitOfWork.CommitAsync();

        return true;
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
        string? statut               = null)
    {
        var result = await _unitOfWork.DocumentEnteteRepository.GetByFiltersAsync(
            typeDocumentCode, natureComposantCode, operationCode, posteCode, familleProduitCode, statut);

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
    private async Task<(Guid? formulaireId, string? codeRef, int version, string statut)> ResolverFormulaireAsync(
        string typeCode,
        string? formCodeRef,
        string? posteCode,
        string? familleCode,
        string? natureCode,
        string? configColonnesJson,
        IEnumerable<ColonneJsonDto>? colonneDefs,
        int versionInitiale,
        bool isCorrectionMineure = false,
        bool forceNouvelleVersion = false)
    {
        int version = 0;
        string statut = "BROUILLON";

        if (string.IsNullOrWhiteSpace(formCodeRef))
        {
            formCodeRef = typeCode switch
            {
                TypesDocument.ControlePoste   when !string.IsNullOrWhiteSpace(posteCode)   => $"FE-RC-{posteCode.Trim()}",
                TypesDocument.PlanProduitFini when !string.IsNullOrWhiteSpace(familleCode) => $"FE-PF-{familleCode.Trim()}",
                TypesDocument.PlanAssemblage  when !string.IsNullOrWhiteSpace(natureCode)  => $"FE-ASS-{natureCode.Trim()}",
                _ => null
            };
        }

        if (string.IsNullOrWhiteSpace(formCodeRef))
            return (null, null, version, statut);

        var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(formCodeRef);
        string role = form?.Role ?? "UNKNOWN";

        var colsJson = BuildColonnesJson(configColonnesJson, colonneDefs);

        var result = await _formulaireStructureService.UpdateFormulaireStructureAsync(
            role, colsJson, formCodeRef, versionInitiale, isCorrectionMineure, forceNouvelleVersion);

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
        doc.Nom = AppliquerNumeroVersionAuTexte(doc.Nom, doc.Version);
        if (!string.IsNullOrWhiteSpace(doc.Designation))
            doc.Designation = AppliquerNumeroVersionAuTexte(doc.Designation, doc.Version);
    }

    /// <summary>Remplace ou ajoute le suffixe "V{n}" dans un texte.</summary>
    private static string AppliquerNumeroVersionAuTexte(string texte, int version)
    {
        if (string.IsNullOrWhiteSpace(texte)) return texte;
        var regex = new System.Text.RegularExpressions.Regex(
            @"([ -]*)V\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return regex.IsMatch(texte)
            ? regex.Replace(texte, $"$1V{version}")
            : $"{texte.TrimEnd('-').TrimEnd()} V{version}";
    }

    /// <summary>Ajoute les colonnes extra manquantes sur toutes les lignes de l'entité.</summary>
    private async Task SyncExtraColonnesAsync(DocumentEntete entite, Guid? formulaireId)
    {
        if (!formulaireId.HasValue) return;

        var activeCols = await _unitOfWork.RefFormulaireRepository
            .GetColonnesActivesByFormulaireIdAsync(formulaireId.Value);

        if (activeCols == null || !activeCols.Any()) return;

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

    /// <summary>Applique le Smart Dictionary sur les libellés des lignes.</summary>
    private async Task AppliquerSmartDictionaryAsync(ICollection<DocumentSection> sections)
    {
        await GenericSmartDictionaryService.ExecuteSmartDictionaryPassAsync(
            sections,
            _unitOfWork.DictionnaireQualiteRepository,
            s => s.DocumentLignes,
            l => (
                l.LibelleAffiche,
                (id) => { l.TypeCaracteristiqueId = id; },
                l.MoyenTexteLibre,
                (id) => { l.MoyenControleId = id; },
                l.InstrumentCode
            ),
            null);
    }

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

    /// <summary>Applique les propriétés personnalisées selon la stratégie du type de document.</summary>
    private void AppliquerStrategieType(DocumentEntete doc, string? configColonnesJson)
    {
        var strategy = _strategies.FirstOrDefault(s => s.DocumentTypeCode == doc.TypeDocumentCode);
        strategy?.ApplyCustomProperties(doc, configColonnesJson);
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
            }
        );
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
                lig.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                {
                    Id            = Guid.NewGuid(),
                    LigneId       = lig.Id,
                    CleColonne    = inc.CleColonne,
                    ValeurColonne = inc.ValeurColonne,
                    OrdreAffiche  = inc.OrdreAffiche
                });
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
