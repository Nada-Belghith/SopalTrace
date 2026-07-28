using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

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

    public async Task<Guid> CreerDocumentAsync(CreateDocumentRequestDto request)
    {
        var user = _currentUserService.UserInfo;

        // Find existing document by trace keys
        var existingDocs = await _unitOfWork.DocumentEnteteRepository.GetByFiltersAsync(
            request.TypeDocumentCode, request.NatureArticleCode, request.OperationCode, request.PosteCode, request.FamilleProduitFiniCode);
            
        string? formCodeRef = request.RefFormulaireCodeReference;

        var strategy = _strategies.FirstOrDefault(s => s.DocumentTypeCode == request.TypeDocumentCode);

        // Auto-generate RefFormulaireCodeReference for specific plans if not provided
        if (string.IsNullOrWhiteSpace(formCodeRef) && strategy != null)
        {
            formCodeRef = strategy.GetDefaultFormulaireCodeRef(new DocumentContextInfo(
                request.PosteCode, 
                request.FamilleProduitFiniCode, 
                request.NatureArticleCode));
        }

        Guid? knownFormulaireId = null;
        if (!string.IsNullOrWhiteSpace(formCodeRef))
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(formCodeRef);
            knownFormulaireId = form?.Id;
        }

        string baseNom = RemoveVersionSuffix(request.Nom).TrimEnd('-').Trim();
        var query = existingDocs.Where(d => RemoveVersionSuffix(d.Nom).TrimEnd('-').Trim() == baseNom);

        if (!string.IsNullOrWhiteSpace(formCodeRef))
        {
            if (!knownFormulaireId.HasValue)
                query = query.Where(d => false); // The form family doesn't exist yet
            else
                query = query.Where(d => d.FormulaireId == knownFormulaireId.Value);
        }

        var existingDoc = query.OrderByDescending(d => d.Version).FirstOrDefault();

        bool forceArchive = request.VersionInitiale.HasValue && request.VersionInitiale.Value != (existingDoc?.Version ?? -1);
        int finalVersion = request.VersionInitiale ?? 0;

        if (existingDoc != null)
        {
            if (existingDoc.Statut == "BROUILLON" && !forceArchive)
            {
                // Replace the draft: delete the old one and keep its version
                var fullDraft = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(existingDoc.Id, includeRelations: true);
                if (fullDraft != null)
                {
                    await _unitOfWork.DocumentEnteteRepository.DeleteAsync(fullDraft);
                }
                finalVersion = existingDoc.Version;
            }
            else
            {
                // Archive ALL existing active documents with the same name and same formulaireId
                var activeDocs = existingDocs.Where(d => d.Nom == request.Nom && d.Statut == "ACTIF" && d.FormulaireId == knownFormulaireId).ToList();
                foreach (var act in activeDocs)
                {
                    act.Statut = "ARCHIVE";
                    await _unitOfWork.DocumentEnteteRepository.UpdateAsync(act);
                }
                
                var maxVersion = existingDoc.Version;
                finalVersion = (request.VersionInitiale.HasValue && request.VersionInitiale.Value > maxVersion) 
                    ? request.VersionInitiale.Value 
                    : (maxVersion + 1);
            }
        }



        Guid? formulaireId = null;

        // Héritage automatique pour la fabrication (Responsable DI)
        if (request.TypeDocumentCode == "MODELE_FAB" || request.TypeDocumentCode == "PLAN_FAB")
        {
            var formStruct = await _formulaireStructureService.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION");
            if (formStruct != null)
            {
                formulaireId = formStruct.Id;
                
                // Si la requête contient une mise à jour de la configuration des colonnes
                var colsJson = request.ConfigurationColonnesJson ?? (request.ColonneDefs != null && request.ColonneDefs.Any()
                    ? System.Text.Json.JsonSerializer.Serialize(request.ColonneDefs, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                    : null);

                if (colsJson != null)
                {
                    await _formulaireStructureService.UpdateFormulaireStructureAsync(
                        "EN_COURS_DE_FABRICATION", 
                        colsJson, 
                        formStruct.CodeReference, 
                        finalVersion
                    );
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(formCodeRef))
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(formCodeRef);
            
            // If the document is being saved, and it references a RefFormulaire,
            // we MUST sync the RefFormulaire version and status as well!
            string role = form?.Role ?? (strategy?.GetDefaultFormRole() ?? "UNKNOWN");
            
            var colsJson = request.ConfigurationColonnesJson ?? (request.ColonneDefs != null && request.ColonneDefs.Any()
                ? System.Text.Json.JsonSerializer.Serialize(request.ColonneDefs, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                : null);
                
            var result = await _formulaireStructureService.UpdateFormulaireStructureAsync(
                role,
                colsJson,
                formCodeRef,
                finalVersion
            );

            if (result.HasValue)
            {
                formulaireId = result.Value.Id;
            }
            else if (form != null)
            {
                formulaireId = form.Id;
            }
        }

        var entite = DocumentMapper.ToEntity(request, user, formulaireId);
        entite.Version = finalVersion;
        entite.Statut = (existingDoc != null && existingDoc.Statut == "BROUILLON" && !forceArchive) ? "BROUILLON" : "ACTIF";

        if (formulaireId.HasValue)
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId.Value);
            if (form != null)
            {
                entite.Version = form.Version;
                entite.Statut = form.Statut ?? entite.Statut;

                // Sync extra columns for all lines based on form definition
                var activeCols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(form.Id);
                if (activeCols != null && activeCols.Any())
                {
                    foreach (var section in entite.DocumentSections)
                    {
                        foreach (var ligne in section.DocumentLignes)
                        {
                            foreach (var colDef in activeCols)
                            {
                                if (!ligne.DocumentLigneExtraColonnes.Any(ec => ec.CleColonne == colDef.CleColonne))
                                {
                                    ligne.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                                    {
                                        Id = Guid.NewGuid(),
                                        LigneId = ligne.Id,
                                        CleColonne = colDef.CleColonne,
                                        ValeurColonne = null,
                                        OrdreAffiche = 0
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
        
        entite.Nom = UpdateVersionInString(entite.Nom, entite.Version);
        if (!string.IsNullOrWhiteSpace(entite.Designation))
        {
            entite.Designation = UpdateVersionInString(entite.Designation, entite.Version);
        }
        
        await GenericSmartDictionaryService.ExecuteSmartDictionaryPassAsync(
            entite.DocumentSections,
            _unitOfWork.DictionnaireQualiteRepository,
            s => s.DocumentLignes,
            l => (
                l.LibelleAffiche,
                id => l.TypeCaracteristiqueId = id,
                l.MoyenTexteLibre,
                id => l.MoyenControleId = id,
                l.InstrumentCode
            ),
            null
        );
        
        // Si le nouveau document est ACTIF, archiver les anciens documents actifs pour le même contexte et le même nom
        if (entite.Statut == "ACTIF")
        {
            var activeDocs = await _unitOfWork.DocumentEnteteRepository.GetByFiltersAsync(
                request.TypeDocumentCode, 
                request.NatureArticleCode, 
                request.OperationCode, 
                request.PosteCode, 
                request.FamilleProduitFiniCode, 
                "ACTIF"
            );
            
            var baseNomRequest = System.Text.RegularExpressions.Regex.Replace(request.Nom ?? "", @"([ -]*)V\d+$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).TrimEnd('-').Trim();
            var sameNameDocs = activeDocs.Where(d => 
                System.Text.RegularExpressions.Regex.Replace(d.Nom ?? "", @"([ -]*)V\d+$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).TrimEnd('-').Trim() == baseNomRequest
                && d.FormulaireId == formulaireId
            ).ToList();
            
            foreach (var act in sameNameDocs)
            {
                act.Statut = "ARCHIVE";
                await _unitOfWork.DocumentEnteteRepository.UpdateAsync(act);
            }
        }

        await _unitOfWork.DocumentEnteteRepository.AddAsync(entite);
        await _unitOfWork.CommitAsync();

        return entite.Id;
    }

    public async Task<DocumentEnteteDto> GetDocumentByIdAsync(Guid documentId)
    {
        var entite = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(documentId, includeRelations: true);
        if (entite == null) throw new Exception("Document introuvable.");

        List<SopalTrace.Domain.Entities.RefFormulaireColonneDef> cols = new();
        if (entite.FormulaireId.HasValue)
        {
            cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(entite.FormulaireId.Value);
        }
        else
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(entite.TypeDocumentCode);
            if (form != null)
            {
                cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(form.Id);
            }
        }

        var dto = DocumentMapper.ToDto(entite, cols);

        var strategy = _strategies.FirstOrDefault(s => s.DocumentTypeCode == entite.TypeDocumentCode);
        if (strategy != null)
        {
            await strategy.PopulateDtoAsync(entite, dto);
        }

        return dto;
    }

    public async Task<IReadOnlyList<DocumentEnteteDto>> GetDocumentsByFiltersAsync(
        string typeDocumentCode,
        string? natureComposantCode = null, 
        string? operationCode = null, 
        string? posteCode = null, 
        string? familleProduitCode = null,
        string? statut = null)
    {
        var result = await _unitOfWork.DocumentEnteteRepository.GetByFiltersAsync(
            typeDocumentCode, natureComposantCode, operationCode, posteCode, familleProduitCode, statut);

        var dtos = new List<DocumentEnteteDto>();
        foreach (var doc in result)
        {
            List<SopalTrace.Domain.Entities.RefFormulaireColonneDef> cols = new();
            if (doc.FormulaireId.HasValue)
            {
                cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(doc.FormulaireId.Value);
            }
            else
            {
                var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(doc.TypeDocumentCode);
                if (form != null)
                {
                    cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(form.Id);
                }
            }
            dtos.Add(DocumentMapper.ToDto(doc, cols));
        }

        return dtos;
    }

    public async Task<Guid> CreerNouvelleVersionDocumentAsync(NouvelleVersionDocumentRequestDto request)
    {
        var user = _currentUserService.UserInfo;

        var ancienDoc = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(request.AncienId, includeRelations: true);
        if (ancienDoc == null) throw new Exception("Ancien document introuvable.");

        // Archiver l'ancien document s'il est actif, ou le document actif de la même famille (même nom de base ET même FormulaireId)
        var allDocs = await _unitOfWork.DocumentEnteteRepository.GetByFiltersAsync(request.TypeDocumentCode, request.NatureArticleCode, request.OperationCode, request.PosteCode, request.FamilleProduitFiniCode, "ACTIF");
        var baseNomRequest = System.Text.RegularExpressions.Regex.Replace(request.Nom ?? "", @"([ -]*)V\d+$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).TrimEnd('-').Trim();
        var activeDocs = allDocs.Where(d => 
            System.Text.RegularExpressions.Regex.Replace(d.Nom ?? "", @"([ -]*)V\d+$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).TrimEnd('-').Trim() == baseNomRequest
            && d.FormulaireId == ancienDoc.FormulaireId
        ).ToList();
        
        foreach (var act in activeDocs)
        {
            act.Statut = "ARCHIVE";
            await _unitOfWork.DocumentEnteteRepository.UpdateAsync(act);
        }

        var newVersion = ancienDoc.Version + 1;

        string? formCodeRef = request.RefFormulaireCodeReference;
        if (string.IsNullOrWhiteSpace(formCodeRef) && ancienDoc.FormulaireId.HasValue)
        {
            var ancienForm = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(ancienDoc.FormulaireId.Value);
            formCodeRef = ancienForm?.CodeReference;
        }

        // Auto-generate RefFormulaireCodeReference for specific plans if still missing
        if (string.IsNullOrWhiteSpace(formCodeRef))
        {
            if (request.TypeDocumentCode == "CTRL_POSTE" && !string.IsNullOrWhiteSpace(request.PosteCode))
            {
                formCodeRef = $"FE-RC-{request.PosteCode.Trim()}";
            }
            else if (request.TypeDocumentCode == "PLAN_PF" && !string.IsNullOrWhiteSpace(request.FamilleProduitFiniCode))
            {
                formCodeRef = $"FE-PF-{request.FamilleProduitFiniCode.Trim()}";
            }
            else if (request.TypeDocumentCode == "PLAN_ASS" && !string.IsNullOrWhiteSpace(request.NatureArticleCode))
            {
                formCodeRef = $"FE-ASS-{request.NatureArticleCode.Trim()}";
            }
        }

        Guid? formulaireId = null;
        if (!string.IsNullOrWhiteSpace(formCodeRef))
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(formCodeRef);
            string role = form?.Role ?? (request.TypeDocumentCode == "CTRL_POSTE" ? "RESULTAT_CONTROLE_POSTE" : "UNKNOWN");
            
            var colsJson = !string.IsNullOrWhiteSpace(request.ConfigurationColonnesJson)
                ? request.ConfigurationColonnesJson
                : (request.ColonneDefs != null && request.ColonneDefs.Any()
                    ? System.Text.Json.JsonSerializer.Serialize(request.ColonneDefs, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                    : null);
                
            var result = await _formulaireStructureService.UpdateFormulaireStructureAsync(
                role,
                colsJson, 
                formCodeRef,
                newVersion // Forcer la création d'une nouvelle version du Formulaire pour s'aligner avec le Document
            );

            if (result.HasValue)
            {
                formulaireId = result.Value.Id;
            }
            else if (form != null)
            {
                formulaireId = form.Id;
            }
        }

        var nouveauDoc = DocumentMapper.ToEntity(request, user, formulaireId);
        nouveauDoc.Version = newVersion;
        nouveauDoc.Statut = "ACTIF";

        if (formulaireId.HasValue)
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId.Value);
            if (form != null)
            {
                nouveauDoc.Version = form.Version;
                nouveauDoc.Statut = form.Statut ?? "ACTIF";

                // Sync extra columns for all lines based on form definition
                var activeCols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(form.Id);
                if (activeCols != null && activeCols.Any())
                {
                    foreach (var section in nouveauDoc.DocumentSections)
                    {
                        foreach (var ligne in section.DocumentLignes)
                        {
                            foreach (var colDef in activeCols)
                            {
                                if (!ligne.DocumentLigneExtraColonnes.Any(ec => ec.CleColonne == colDef.CleColonne))
                                {
                                    ligne.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                                    {
                                        Id = Guid.NewGuid(),
                                        LigneId = ligne.Id,
                                        CleColonne = colDef.CleColonne,
                                        ValeurColonne = null,
                                        OrdreAffiche = 0
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        nouveauDoc.Nom = UpdateVersionInString(nouveauDoc.Nom, nouveauDoc.Version);
        if (!string.IsNullOrWhiteSpace(nouveauDoc.Designation))
        {
            nouveauDoc.Designation = UpdateVersionInString(nouveauDoc.Designation, nouveauDoc.Version);
        }

        await GenericSmartDictionaryService.ExecuteSmartDictionaryPassAsync(
            nouveauDoc.DocumentSections,
            _unitOfWork.DictionnaireQualiteRepository,
            s => s.DocumentLignes,
            l => (
                l.LibelleAffiche,
                id => l.TypeCaracteristiqueId = id,
                l.MoyenTexteLibre,
                id => l.MoyenControleId = id,
                l.InstrumentCode
            ),
            null
        );

        await _unitOfWork.DocumentEnteteRepository.AddAsync(nouveauDoc);
        await _unitOfWork.CommitAsync();

        return nouveauDoc.Id;
    }

    public async Task<Guid> RestaurerDocumentArchiveAsync(RestaurerDocumentRequestDto request)
    {
        var user = _currentUserService.UserInfo;

        var archiveDoc = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(request.DocumentArchiveId, includeRelations: true);
        if (archiveDoc == null) throw new Exception("Document archivé introuvable.");

        // Trouver le document actif actuel pour l'archiver
        var docActifActuel = await _unitOfWork.DocumentEnteteRepository.GetActifByReferenceAsync(
            archiveDoc.TypeDocumentCode, archiveDoc.Nom, archiveDoc.OperationCode, archiveDoc.PosteCode);

        if (docActifActuel != null)
        {
            docActifActuel.Statut = "ARCHIVE";
            await _unitOfWork.DocumentEnteteRepository.UpdateAsync(docActifActuel);
        }

        var newVersion = await _unitOfWork.DocumentEnteteRepository.GetLatestVersionAsync(
            archiveDoc.TypeDocumentCode, 
            archiveDoc.Nom, 
            archiveDoc.OperationCode,
            archiveDoc.PosteCode,
            archiveDoc.NatureArticleCode,
            archiveDoc.FamilleProduitFiniCode) + 1;

        // Créer un nouveau request DTO à partir de l'archive (Duplication simplifiée)
        var dto = new CreateDocumentRequestDto
        {
            TypeDocumentCode = archiveDoc.TypeDocumentCode,
            Nom = archiveDoc.Nom,
            Designation = archiveDoc.Designation,
            VersionInitiale = newVersion,
            OperationCode = archiveDoc.OperationCode,
            LegendeMoyens = archiveDoc.LegendeMoyens,
            Remarques = $"[Restauré] {request.MotifRestoration}",
            NatureArticleCode = archiveDoc.NatureArticleCode,
            FamilleProduitFiniCode = archiveDoc.FamilleProduitFiniCode,
            PosteCode = archiveDoc.PosteCode,
            Libre1 = archiveDoc.Libre1,
            Libre2 = archiveDoc.Libre2,
            Libre3 = archiveDoc.Libre3,
            Sections = archiveDoc.DocumentSections.Select(s => new CreateDocumentSectionDto
            {
                LibelleSection = s.LibelleSection,
                OrdreAffiche = s.OrdreAffiche,
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,
                RegleEchantillonnageId = s.RegleEchantillonnageId,
                Notes = s.Notes,
                NormeReference = s.NormeReference,
                NqaId = s.NqaId,
                Lignes = s.DocumentLignes.Select(l => new CreateDocumentLigneDto
                {
                    OrdreAffiche = l.OrdreAffiche,
                    CaracteristiqueId = l.CaracteristiqueId,
                    LibelleAffiche = l.LibelleAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                    TypeControleId = l.TypeControleId,
                    MoyenControleId = l.MoyenControleId,
                    MoyenTexteLibre = l.MoyenTexteLibre,
                    InstrumentCode = l.InstrumentCode,
                    PeriodiciteId = l.PeriodiciteId,
                    LimiteSpecTexte = l.LimiteSpecTexte,
                    EstCritique = l.EstCritique,
                    Instruction = l.Instruction,
                    Observations = l.Observations,
                    ImageBase64 = l.ImageBase64,
                    MachineCode = l.MachineCode,
                    EstVerifPresence = l.EstVerifPresence,
                    DefauthequeId = l.DefauthequeId,
                    RefPlanProduit = l.RefPlanProduit,
                    MachineCodeCtrlPoste = l.MachineCodeCtrlPoste,
                    RisqueDefautId = l.RisqueDefautId,
                    Libre1 = l.Libre1,
                    Libre2 = l.Libre2,
                    Libre3 = l.Libre3,
                    Libre4 = l.Libre4,
                    Libre5 = l.Libre5
                }).ToList()
            }).ToList()
        };

        Guid? formulaireId = archiveDoc.FormulaireId;
        if (!string.IsNullOrWhiteSpace(dto.RefFormulaireCodeReference))
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(dto.RefFormulaireCodeReference);
            if (form != null)
            {
                formulaireId = form.Id;
            }
        }

        var nouveauDoc = DocumentMapper.ToEntity(dto, user, formulaireId);
        
        await _unitOfWork.DocumentEnteteRepository.AddAsync(nouveauDoc);
        await _unitOfWork.CommitAsync();

        return nouveauDoc.Id;
    }

    public async Task<bool> MettreAJourDocumentAsync(Guid id, UpdateDocumentRequestDto request)
    {
        var doc = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (doc == null) return false;

        if (request.Nom != null) doc.Nom = request.Nom;
        if (request.LegendeMoyens != null) doc.LegendeMoyens = request.LegendeMoyens;
        if (request.Remarques != null) doc.Remarques = request.Remarques;
        if (request.Libre1 != null) doc.Libre1 = request.Libre1;
        
        var strategy = _strategies.FirstOrDefault(s => s.DocumentTypeCode == doc.TypeDocumentCode);

        if (strategy != null)
        {
            strategy.ApplyCustomProperties(doc, request.ConfigurationColonnesJson);
        }

        SopalTrace.Application.Utilities.SectionUpdateHelper.UpdateSections(
            doc.DocumentSections,
            request.Sections,
            sec => _unitOfWork.DocumentEnteteRepository.RemoveSection(sec),
            lig => _unitOfWork.DocumentEnteteRepository.RemoveLigne(lig),
            dto => dto.Id,
            dto => dto.Id,
            sec => sec.Id,
            lig => lig.Id,
            dto => new DocumentSection
            {
                Id = Guid.NewGuid(),
                EnteteId = doc.Id,
                OrdreAffiche = dto.OrdreAffiche,
                LibelleSection = dto.LibelleSection,
                TypeSectionId = dto.TypeSectionId,
                PeriodiciteId = dto.PeriodiciteId,
                RegleEchantillonnageId = dto.RegleEchantillonnageId,
                Notes = dto.Notes,
                NormeReference = dto.NormeReference,
                NqaId = dto.NqaId
            },
            (sec, dto) =>
            {
                sec.OrdreAffiche = dto.OrdreAffiche;
                sec.LibelleSection = dto.LibelleSection;
                sec.TypeSectionId = dto.TypeSectionId;
                sec.PeriodiciteId = dto.PeriodiciteId;
                sec.RegleEchantillonnageId = dto.RegleEchantillonnageId;
                sec.Notes = dto.Notes;
                sec.NormeReference = dto.NormeReference;
                sec.NqaId = dto.NqaId;
            },
            sec => sec.DocumentLignes,
            dto => dto.Lignes,
            (dto, sec) =>
            {
                var ligne = new DocumentLigne
                {
                    Id = Guid.NewGuid(),
                    EnteteId = doc.Id,
                    SectionId = sec.Id,
                    OrdreAffiche = dto.OrdreAffiche,
                    CaracteristiqueId = dto.CaracteristiqueId,
                    LibelleAffiche = dto.LibelleAffiche,
                    TypeCaracteristiqueId = dto.TypeCaracteristiqueId,
                    TypeControleId = dto.TypeControleId,
                    MoyenControleId = dto.MoyenControleId,
                    MoyenTexteLibre = dto.MoyenTexteLibre,
                    InstrumentCode = string.IsNullOrWhiteSpace(dto.InstrumentCode) ? null : dto.InstrumentCode,
                    PeriodiciteId = dto.PeriodiciteId,
                    LimiteSpecTexte = dto.LimiteSpecTexte,
                    EstCritique = dto.EstCritique,
                    Instruction = dto.Instruction,
                    Observations = dto.Observations,
                    ImageBase64 = dto.ImageBase64,
                    MachineCode = string.IsNullOrWhiteSpace(dto.MachineCode) ? null : dto.MachineCode,
                    EstVerifPresence = dto.EstVerifPresence,
                    DefauthequeId = dto.DefauthequeId,
                    RefPlanProduit = string.IsNullOrWhiteSpace(dto.RefPlanProduit) ? null : dto.RefPlanProduit,
                    MachineCodeCtrlPoste = string.IsNullOrWhiteSpace(dto.MachineCodeCtrlPoste) ? null : dto.MachineCodeCtrlPoste,
                    RisqueDefautId = dto.RisqueDefautId,
                    Libre1 = dto.Libre1,
                    Libre2 = dto.Libre2,
                    Libre3 = dto.Libre3,
                    Libre4 = dto.Libre4,
                    Libre5 = dto.Libre5
                };
                if (dto.ExtraColonnes != null)
                {
                    foreach (var ec in dto.ExtraColonnes)
                    {
                        ligne.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                        {
                            Id = Guid.NewGuid(),
                            LigneId = ligne.Id,
                            CleColonne = ec.CleColonne,
                            ValeurColonne = ec.ValeurColonne,
                            OrdreAffiche = ec.OrdreAffiche
                        });
                    }
                }
                return ligne;
            },
            (lig, dto) =>
            {
                lig.OrdreAffiche = dto.OrdreAffiche;
                lig.CaracteristiqueId = dto.CaracteristiqueId;
                lig.LibelleAffiche = dto.LibelleAffiche;
                lig.TypeCaracteristiqueId = dto.TypeCaracteristiqueId;
                lig.TypeControleId = dto.TypeControleId;
                lig.MoyenControleId = dto.MoyenControleId;
                lig.MoyenTexteLibre = dto.MoyenTexteLibre;
                lig.InstrumentCode = string.IsNullOrWhiteSpace(dto.InstrumentCode) ? null : dto.InstrumentCode;
                lig.PeriodiciteId = dto.PeriodiciteId;
                lig.LimiteSpecTexte = dto.LimiteSpecTexte;
                lig.EstCritique = dto.EstCritique;
                lig.Instruction = dto.Instruction;
                lig.Observations = dto.Observations;
                lig.ImageBase64 = dto.ImageBase64;
                lig.MachineCode = string.IsNullOrWhiteSpace(dto.MachineCode) ? null : dto.MachineCode;
                lig.EstVerifPresence = dto.EstVerifPresence;
                lig.DefauthequeId = dto.DefauthequeId;
                lig.RefPlanProduit = string.IsNullOrWhiteSpace(dto.RefPlanProduit) ? null : dto.RefPlanProduit;
                lig.MachineCodeCtrlPoste = string.IsNullOrWhiteSpace(dto.MachineCodeCtrlPoste) ? null : dto.MachineCodeCtrlPoste;
                lig.RisqueDefautId = dto.RisqueDefautId;
                lig.Libre1 = dto.Libre1;
                lig.Libre2 = dto.Libre2;
                lig.Libre3 = dto.Libre3;
                lig.Libre4 = dto.Libre4;
                lig.Libre5 = dto.Libre5;
                
                var existingExtraCols = lig.DocumentLigneExtraColonnes.ToList();
                var incomingExtraCols = dto.ExtraColonnes ?? new List<CreateDocumentExtraColonneDto>();

                foreach (var ec in existingExtraCols)
                {
                    if (!incomingExtraCols.Any(i => i.CleColonne == ec.CleColonne))
                    {
                        _unitOfWork.DocumentEnteteRepository.RemoveExtraColonne(ec);
                        lig.DocumentLigneExtraColonnes.Remove(ec);
                    }
                }

                foreach (var inc in incomingExtraCols)
                {
                    var existing = existingExtraCols.FirstOrDefault(e => e.CleColonne == inc.CleColonne);
                    if (existing != null)
                    {
                        existing.ValeurColonne = inc.ValeurColonne;
                        existing.OrdreAffiche = inc.OrdreAffiche;
                    }
                    else
                    {
                        lig.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                        {
                            Id = Guid.NewGuid(),
                            LigneId = lig.Id,
                            CleColonne = inc.CleColonne,
                            ValeurColonne = inc.ValeurColonne,
                            OrdreAffiche = inc.OrdreAffiche
                        });
                    }
                }
            }
        );

        await _unitOfWork.DocumentEnteteRepository.UpdateAsync(doc);
        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<bool> SupprimerDocumentAsync(Guid id)
    {
        var doc = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(id);
        if (doc != null)
        {
            await _unitOfWork.DocumentEnteteRepository.DeleteAsync(doc);
            await _unitOfWork.CommitAsync();
            return true;
        }
        return false;
    }

    private string UpdateVersionInString(string text, int version)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        
        var regex = new System.Text.RegularExpressions.Regex(@"([ -]*)V\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (regex.IsMatch(text))
        {
            return regex.Replace(text, $"$1V{version}");
        }
        
        if (text.EndsWith("-"))
        {
            return $"{text}V{version}";
        }
        {
            return $"{text} V{version}";
        }
    }

    private string RemoveVersionSuffix(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var regex = new System.Text.RegularExpressions.Regex(@"([ -]*)V\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return regex.Replace(text, string.Empty);
    }

    public async Task ArchiverDocumentsByFormulaireAsync(Guid formulaireId)
    {
        var documents = await _unitOfWork.DocumentEnteteRepository.GetByFormulaireIdAsync(formulaireId);
        foreach (var doc in documents.Where(d => d.Statut == "ACTIF"))
        {
            doc.Statut = "ARCHIVE";
            await _unitOfWork.DocumentEnteteRepository.UpdateAsync(doc);
        }
    }
}
