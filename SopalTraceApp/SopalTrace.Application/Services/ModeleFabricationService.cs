using SopalTrace.Domain.Constants;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using System.Collections.Generic;

namespace SopalTrace.Application.Services;

public class ModeleFabricationService : IModeleFabricationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFormulaireStructureService _formulaireStructureService;
    private readonly IFrequencyParserService _frequencyParserService;

    public ModeleFabricationService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFormulaireStructureService formulaireStructureService,
        IFrequencyParserService frequencyParserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _formulaireStructureService = formulaireStructureService;
        _frequencyParserService = frequencyParserService;
    }

    public async Task<ModeleResponseDto?> GetModelByIdAsync(Guid id)
    {
        var modele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (modele == null) return null;

        var dto = ModeleFabricationMapper.ToDto(modele);
        await FallbackToActiveFormColumnsIfMissingAsync(dto, modele);
        
        return dto;
    }

    private async Task FallbackToActiveFormColumnsIfMissingAsync(ModeleResponseDto dto, DocumentEntete modele)
    {
        // Ne pas écraser la structure si elle a déjà été figée (ex: Modèles archivés)
        if (!string.IsNullOrWhiteSpace(dto.ConfigurationColonnesJson))
            return;

        if (modele.Formulaire == null || string.IsNullOrWhiteSpace(modele.Formulaire.CodeReference) || !modele.FormulaireId.HasValue)
            return;

        var activeCols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(modele.FormulaireId.Value);
        if (activeCols != null)
        {
            dto.ConfigurationColonnesJson = ColonneJsonMapper.Serialize(activeCols);
        }
    }

    public async Task<IReadOnlyList<ModeleResponseDto>> GetModelsByFiltersAsync(string? natureComposantCode = null, string? operationCode = null, string? familleProduitCode = null, string? statut = null)
    {
        var modeles = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFiltersAsync(natureComposantCode, operationCode, familleProduitCode, statut);
        return modeles.Select(m => ModeleFabricationMapper.ToDto(m)).ToList();
    }

    public async Task<Guid> CreateModelAsync(CreateModeleRequestDto request)
    {
        var user = _currentUserService.MatriculeRequis;
        var formStruct = await GetFormStructOrThrowAsync();

        await ArchiveActiveModelsForCodeAsync(request.NatureComposantCode, request.OperationCode, request.FamilleProduitCode, request.Code);
        await EnsureFrequenciesResolvedAsync(request.Sections);

        var modele = ModeleFabricationMapper.ToEntity(request, user, formStruct.Id);
        modele.Version = formStruct.Version; 
        
        modele.Designation = await ResolveModelSuffixAsync(modele.Designation ?? "", request.NatureComposantCode, request.OperationCode, request.FamilleProduitCode, request.Code, formStruct.Version);

        await SyncExtraColumnsAsync(modele, formStruct.Id);
        await ReferencesLignesQualite.CompleterAsync(modele.DocumentSections, _unitOfWork.DictionnaireQualiteRepository);

        await _unitOfWork.ModeleFabricationEnteteRepository.AddAsync(modele);
        await _unitOfWork.CommitAsync();

        return modele.Id;
    }

    public async Task<Guid> CreateNewVersionAsync(NouvelleVersionModeleRequestDto request)
    {
        var existingModele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(request.AncienId, includeRelations: true);
        if (existingModele == null) throw new RessourceIntrouvableException("Ancien modèle introuvable.");
        _unitOfWork.VerifierVersionDocument(existingModele, request.RowVersion, obligatoire: false);

        var createReq = new CreateModeleRequestDto
        {
            Code = !string.IsNullOrWhiteSpace(request.Code) ? request.Code : existingModele.Nom,
            Libelle = !string.IsNullOrWhiteSpace(request.Libelle) ? request.Libelle : (existingModele.Designation ?? ""),
            TypeRobinetCode = request.TypeRobinetCode ?? "",
            NatureComposantCode = !string.IsNullOrWhiteSpace(request.NatureComposantCode) ? request.NatureComposantCode : (existingModele.NatureArticleCode ?? ""),
            FamilleProduitCode = request.FamilleProduitCode ?? existingModele.FamilleProduitFiniCode,
            OperationCode = !string.IsNullOrWhiteSpace(request.OperationCode) ? request.OperationCode : (existingModele.OperationCode ?? ""),
            VersionInitiale = request.VersionInitiale,
            LegendeMoyens = request.LegendeMoyens ?? existingModele.LegendeMoyens,
            Notes = request.Notes ?? existingModele.Remarques,
            RefFormulaireCodeReference = request.RefFormulaireCodeReference ?? existingModele.Formulaire?.CodeReference,
            ConfigurationColonnesJson = request.ConfigurationColonnesJson,
            Sections = request.Sections != null && request.Sections.Any() 
                ? request.Sections 
                : ModeleFabricationMapper.BuildSectionsFromExistingModel(existingModele)
        };

        return await CreateModelAsync(createReq);
    }

    public async Task<string?> UpdateModelAsync(Guid id, CreateModeleRequestDto request)
    {
        var modele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (modele == null) return null;
        _unitOfWork.VerifierVersionDocument(modele, request.RowVersion, obligatoire: true);

        // RÈGLE MÉTIER STRICTE : On n'écrase jamais un modèle ACTIF.
        if (modele.Statut == DocumentStatuts.Actif)
        {
            throw new RegleMetierException("Écrasement interdit : Un modèle ACTIF ne peut pas être mis à jour sur place. Une nouvelle version doit être créée.");
        }

        var formStruct = await GetFormStructOrThrowAsync();
        await EnsureFrequenciesResolvedAsync(request.Sections);

        var newEntityData = ModeleFabricationMapper.ToEntity(request, _currentUserService.MatriculeRequis, formStruct.Id);

        if (formStruct.Version > modele.Version)
        {
            await HandleVersionUpgradeAsync(modele, newEntityData, formStruct);
        }
        else
        {
            await HandleInPlaceUpdateAsync(modele, newEntityData, request, formStruct);
            await ReferencesLignesQualite.CompleterAsync(modele.DocumentSections, _unitOfWork.DictionnaireQualiteRepository);
        }

        await _unitOfWork.CommitAsync();
        return VersionsDocument.EnTexte(modele.RowVersion) ?? string.Empty;
    }

    private async Task HandleVersionUpgradeAsync(DocumentEntete existingModele, DocumentEntete newEntityData, FormulaireStructureDto formStruct)
    {
        existingModele.Statut = DocumentStatuts.Archive;
        await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(existingModele);

        newEntityData.Version = formStruct.Version; 
        newEntityData.Designation = await ResolveModelSuffixAsync(newEntityData.Designation ?? "", existingModele.NatureArticleCode, existingModele.OperationCode, existingModele.FamilleProduitFiniCode, existingModele.Nom, formStruct.Version);
        await SyncExtraColumnsAsync(newEntityData, formStruct.Id);
        await ReferencesLignesQualite.CompleterAsync(newEntityData.DocumentSections, _unitOfWork.DictionnaireQualiteRepository);

        await _unitOfWork.ModeleFabricationEnteteRepository.AddAsync(newEntityData);
    }

    private async Task HandleInPlaceUpdateAsync(DocumentEntete existingModele, DocumentEntete newEntityData, CreateModeleRequestDto request, FormulaireStructureDto formStruct)
    {
        UpdateModelInPlace(existingModele, request);
        await SyncExtraColumnsAsync(existingModele, formStruct.Id);
        
        await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(existingModele);
    }

    public async Task ArchiveModelsByFormulaireAsync(Guid formulaireId)
    {
        var modelesFabrication = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFormulaireIdAsync(formulaireId);
        foreach (var modele in modelesFabrication.Where(m => m.Statut == DocumentStatuts.Actif))
        {
            modele.Statut = DocumentStatuts.Archive;
            await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(modele);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRIVATE METHODS
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task<FormulaireStructureDto> GetFormStructOrThrowAsync()
    {
        var formStruct = await _formulaireStructureService.GetFormulaireByRoleAsync(RolesFormulaire.EnCoursDeFabrication);
        if (formStruct == null) throw new RegleMetierException("Aucun formulaire actif pour les plans en cours de fabrication (PRC).");
        return formStruct;
    }

    private async Task ArchiveActiveModelsForCodeAsync(string? natureCode, string? operationCode, string? familleCode, string code)
    {
        var existingDocs = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFiltersAsync(natureCode, operationCode, familleCode);
        var activeDocs = existingDocs.Where(d => d.Nom == code && d.Statut == DocumentStatuts.Actif).ToList();
        
        foreach (var act in activeDocs)
        {
            act.Statut = DocumentStatuts.Archive;
            await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(act);
        }
    }

    private async Task<string> ResolveModelSuffixAsync(string libelle, string? natureCode, string? operationCode, string? familleCode, string code, int formulaireVersion)
    {
        var existingDocs = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFiltersAsync(natureCode, operationCode, familleCode);
        var iterCount = existingDocs.Count(d => d.Nom == code && ((d.Formulaire != null && d.Formulaire.Version == formulaireVersion) || d.Version == formulaireVersion));

        var baseLibelle = System.Text.RegularExpressions.Regex.Replace(libelle, @"\s+V\d+(\.\d+)?$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (string.IsNullOrWhiteSpace(baseLibelle))
        {
            baseLibelle = $"Modèle {code}";
        }

        return $"{baseLibelle} V{formulaireVersion}.{iterCount}";
    }

    private async Task EnsureFrequenciesResolvedAsync(List<SectionModeleEditDto>? sections)
    {
        if (sections == null) return;

        foreach (var s in sections)
        {
            if (!s.PeriodiciteId.HasValue && !string.IsNullOrEmpty(s.LibelleSection))
            {
                s.PeriodiciteId = await _frequencyParserService.ResolveOrCreatePeriodiciteFromTextAsync(s.LibelleSection);
            }
        }
    }

    private async Task SyncExtraColumnsAsync(DocumentEntete modele, Guid formulaireId)
    {
        var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId);
        if (form == null) return;

        var activeCols = (await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(form.Id))?.ToList();
        if (activeCols == null || !activeCols.Any()) return;

        foreach (var sec in modele.DocumentSections)
        {
            foreach (var lig in sec.DocumentLignes)
            {
                var existingKeys = lig.DocumentLigneExtraColonnes.Select(c => c.CleColonne).ToList();
                foreach (var colDef in activeCols)
                {
                    if (!existingKeys.Contains(colDef.CleColonne))
                    {
                        lig.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                        {
                            Id = Guid.NewGuid(),
                            LigneId = lig.Id,
                            Ligne = lig,
                            CleColonne = colDef.CleColonne,
                            ValeurColonne = null,
                            OrdreAffiche = lig.DocumentLigneExtraColonnes.Count + 1
                        });
                        existingKeys.Add(colDef.CleColonne);
                    }
                }
            }
        }
    }

    private void UpdateModelInPlace(DocumentEntete modele, CreateModeleRequestDto request)
    {
        modele.Remarques = request.Notes;
        modele.LegendeMoyens = request.LegendeMoyens;
        modele.OperationCode = request.OperationCode ?? modele.OperationCode;

        if (request.Sections != null)
        {
            if (modele.DocumentSections == null)
            {
                modele.DocumentSections = new List<DocumentSection>();
            }

            SopalTrace.Application.Utilities.SectionUpdateHelper.UpdateSections(
                modele.DocumentSections,
                request.Sections,
                sec => _unitOfWork.ModeleFabricationEnteteRepository.RemoveSection(sec),
                lig => _unitOfWork.ModeleFabricationEnteteRepository.RemoveLigne(lig),
                dto => dto.Id,
                dto => dto.Id,
                sec => sec.Id,
                lig => lig.Id,
                dto => ModeleFabricationMapper.CreateSection(dto, modele.Id),
                (sec, dto) => ModeleFabricationMapper.UpdateSection(sec, dto),
                sec => sec.DocumentLignes,
                dto => dto.Lignes,
                (dto, sec) => ModeleFabricationMapper.CreateLigne(dto, modele.Id, sec.Id),
                (lig, dto) => 
                {
                    ModeleFabricationMapper.UpdateLigne(lig, dto);
                    ModeleFabricationMapper.MergerExtraColonnes(lig, dto.ExtraColonnes ?? new(), _unitOfWork);
                },
            ajouter: _unitOfWork.Ajouter);
        }
    }

}
