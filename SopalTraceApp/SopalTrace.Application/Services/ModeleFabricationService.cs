using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Entities;
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

    private async Task FallbackToActiveFormColumnsIfMissingAsync(ModeleResponseDto dto, ModeleFabricationEntete modele)
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
        var user = _currentUserService.UserInfo ?? "";
        var formStruct = await GetFormStructOrThrowAsync();

        await ArchiveActiveModelsForCodeAsync(request.NatureComposantCode, request.OperationCode, request.FamilleProduitCode, request.Code);
        await EnsureFrequenciesResolvedAsync(request.Sections);

        var modele = ModeleFabricationMapper.ToEntity(request, user, formStruct.Id);
        modele.Version = formStruct.Version; 
        
        modele.Libelle = await ResolveModelSuffixAsync(modele.Libelle ?? "", request.NatureComposantCode, request.OperationCode, request.FamilleProduitCode, request.Code, formStruct.Version);

        await SyncExtraColumnsAsync(modele, formStruct.Id);

        await _unitOfWork.ModeleFabricationEnteteRepository.AddAsync(modele);
        await _unitOfWork.CommitAsync();

        return modele.Id;
    }

    public async Task<Guid> CreateNewVersionAsync(NouvelleVersionModeleRequestDto request)
    {
        var existingModele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(request.AncienId, includeRelations: true);
        if (existingModele == null) throw new Exception("Ancien modèle introuvable.");

        var createReq = new CreateModeleRequestDto
        {
            Code = !string.IsNullOrWhiteSpace(request.Code) ? request.Code : existingModele.Code,
            Libelle = !string.IsNullOrWhiteSpace(request.Libelle) ? request.Libelle : existingModele.Libelle,
            TypeRobinetCode = request.TypeRobinetCode ?? "",
            NatureComposantCode = !string.IsNullOrWhiteSpace(request.NatureComposantCode) ? request.NatureComposantCode : (existingModele.NatureArticleCode ?? ""),
            FamilleProduitCode = request.FamilleProduitCode ?? existingModele.FamilleProduitFiniCode,
            OperationCode = !string.IsNullOrWhiteSpace(request.OperationCode) ? request.OperationCode : (existingModele.OperationCode ?? ""),
            VersionInitiale = request.VersionInitiale,
            LegendeMoyens = request.LegendeMoyens ?? existingModele.LegendeMoyens,
            Notes = request.Notes ?? existingModele.Notes,
            RefFormulaireCodeReference = request.RefFormulaireCodeReference ?? existingModele.Formulaire?.CodeReference,
            ConfigurationColonnesJson = request.ConfigurationColonnesJson,
            Sections = request.Sections != null && request.Sections.Any() 
                ? request.Sections 
                : ModeleFabricationMapper.BuildSectionsFromExistingModel(existingModele)
        };

        return await CreateModelAsync(createReq);
    }

    public async Task<bool> UpdateModelAsync(Guid id, CreateModeleRequestDto request)
    {
        var modele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (modele == null) return false;

        // RÈGLE MÉTIER STRICTE : On n'écrase jamais un modèle ACTIF.
        if (modele.Statut == "ACTIF")
        {
            throw new InvalidOperationException("Écrasement interdit : Un modèle ACTIF ne peut pas être mis à jour sur place. Une nouvelle version doit être créée.");
        }

        var formStruct = await GetFormStructOrThrowAsync();
        await EnsureFrequenciesResolvedAsync(request.Sections);

        var newEntityData = ModeleFabricationMapper.ToEntity(request, _currentUserService.UserInfo ?? "", formStruct.Id);

        if (formStruct.Version > modele.Version)
        {
            await HandleVersionUpgradeAsync(modele, newEntityData, formStruct);
        }
        else
        {
            await HandleInPlaceUpdateAsync(modele, newEntityData, request, formStruct);
        }

        await _unitOfWork.CommitAsync();
        return true;
    }

    private async Task HandleVersionUpgradeAsync(ModeleFabricationEntete existingModele, ModeleFabricationEntete newEntityData, FormulaireStructureDto formStruct)
    {
        existingModele.Statut = "ARCHIVE";
        await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(existingModele);

        newEntityData.Version = formStruct.Version; 
        newEntityData.Libelle = await ResolveModelSuffixAsync(newEntityData.Libelle ?? "", existingModele.NatureArticleCode, existingModele.OperationCode, existingModele.FamilleProduitFiniCode, existingModele.Code, formStruct.Version);
        await SyncExtraColumnsAsync(newEntityData, formStruct.Id);
        
        await _unitOfWork.ModeleFabricationEnteteRepository.AddAsync(newEntityData);
    }

    private async Task HandleInPlaceUpdateAsync(ModeleFabricationEntete existingModele, ModeleFabricationEntete newEntityData, CreateModeleRequestDto request, FormulaireStructureDto formStruct)
    {
        UpdateModelInPlace(existingModele, newEntityData, request);
        await SyncExtraColumnsAsync(existingModele, formStruct.Id);
        
        await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(existingModele);
    }

    public async Task ArchiveModelsByFormulaireAsync(Guid formulaireId)
    {
        var modelesFabrication = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFormulaireIdAsync(formulaireId);
        foreach (var modele in modelesFabrication.Where(m => m.Statut == "ACTIF"))
        {
            modele.Statut = "ARCHIVE";
            await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(modele);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRIVATE METHODS
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task<FormulaireStructureDto> GetFormStructOrThrowAsync()
    {
        var formStruct = await _formulaireStructureService.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION");
        if (formStruct == null) throw new Exception("Formulaire PRC introuvable.");
        return formStruct;
    }

    private async Task ArchiveActiveModelsForCodeAsync(string? natureCode, string? operationCode, string? familleCode, string code)
    {
        var existingDocs = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFiltersAsync(natureCode, operationCode, familleCode);
        var activeDocs = existingDocs.Where(d => d.Code == code && d.Statut == "ACTIF").ToList();
        
        foreach (var act in activeDocs)
        {
            act.Statut = "ARCHIVE";
            await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(act);
        }
    }

    private async Task<string> ResolveModelSuffixAsync(string libelle, string? natureCode, string? operationCode, string? familleCode, string code, int formulaireVersion)
    {
        var existingDocs = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFiltersAsync(natureCode, operationCode, familleCode);
        var iterCount = existingDocs.Count(d => d.Code == code && ((d.Formulaire != null && d.Formulaire.Version == formulaireVersion) || d.Version == formulaireVersion));

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

    private async Task SyncExtraColumnsAsync(ModeleFabricationEntete modele, Guid formulaireId)
    {
        var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId);
        if (form == null) return;

        var activeCols = (await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(form.Id))?.ToList();
        if (activeCols == null || !activeCols.Any()) return;

        foreach (var sec in modele.ModeleFabricationSections)
        {
            foreach (var lig in sec.ModeleFabricationLignes)
            {
                var existingKeys = lig.ModeleFabricationLigneExtraColonnes.Select(c => c.CleColonne).ToList();
                foreach (var colDef in activeCols)
                {
                    if (!existingKeys.Contains(colDef.CleColonne))
                    {
                        lig.ModeleFabricationLigneExtraColonnes.Add(new ModeleFabricationLigneExtraColonne
                        {
                            Id = Guid.NewGuid(),
                            LigneId = lig.Id,
                            Ligne = lig,
                            CleColonne = colDef.CleColonne,
                            ValeurColonne = null,
                            OrdreAffiche = lig.ModeleFabricationLigneExtraColonnes.Count + 1
                        });
                        existingKeys.Add(colDef.CleColonne);
                    }
                }
            }
        }
    }

    private void UpdateModelInPlace(ModeleFabricationEntete modele, ModeleFabricationEntete newEntityData, CreateModeleRequestDto request)
    {
        modele.Notes = request.Notes;
        modele.LegendeMoyens = request.LegendeMoyens;
        modele.OperationCode = request.OperationCode ?? modele.OperationCode;

        if (modele.ModeleFabricationSections != null)
        {
            foreach (var section in modele.ModeleFabricationSections.ToList())
                _unitOfWork.ModeleFabricationEnteteRepository.RemoveSection(section);
            modele.ModeleFabricationSections.Clear();
            _unitOfWork.FlushDeletesAsync().Wait();
        }
        else
        {
            modele.ModeleFabricationSections = new List<ModeleFabricationSection>();
        }
        
        foreach (var s in newEntityData.ModeleFabricationSections)
        {
            s.ModeleEnteteId = modele.Id; 
            modele.ModeleFabricationSections.Add(s);
        }
    }

}
