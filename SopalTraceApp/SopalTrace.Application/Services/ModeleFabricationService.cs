using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SopalTrace.Application.Services;

public class ModeleFabricationService : IModeleFabricationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFormulaireStructureService _formulaireStructureService;

    public ModeleFabricationService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFormulaireStructureService formulaireStructureService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _formulaireStructureService = formulaireStructureService;
    }

    public async Task<ModeleResponseDto?> GetModeleByIdAsync(Guid id)
    {
        var modele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (modele == null) return null;

        return ModeleFabricationMapper.ToDto(modele);
    }

    public async Task<IReadOnlyList<ModeleResponseDto>> GetModelesByFiltersAsync(string? natureComposantCode = null, string? operationCode = null, string? familleProduitCode = null, string? statut = null)
    {
        var modeles = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFiltersAsync(natureComposantCode, operationCode, familleProduitCode, statut);
        return modeles.Select(m => ModeleFabricationMapper.ToDto(m)).ToList();
    }

    public async Task<Guid> CreerModeleAsync(CreateModeleRequestDto request)
    {
        var user = _currentUserService.UserInfo ?? "";
        var existingDocs = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFiltersAsync(request.NatureComposantCode, request.OperationCode, request.FamilleProduitCode);
        
        var existingDoc = existingDocs.Where(d => d.Code == request.Code)
                                      .OrderByDescending(d => d.Version)
                                      .FirstOrDefault();

        var formStruct = await _formulaireStructureService.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION");
        if (formStruct == null) throw new Exception("Formulaire PRC introuvable.");

        if (existingDoc != null)
        {
            var activeDocs = existingDocs.Where(d => d.Code == request.Code && d.Statut == "ACTIF").ToList();
            foreach (var act in activeDocs)
            {
                act.Statut = "ARCHIVE";
                await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(act);
            }
        }

        var modele = ModeleFabricationMapper.ToEntity(request, user, formStruct.Id);
        modele.Version = formStruct.Version; // Force version to match form
        
        // Incrémentation du suffixe sur le Libellé (Nom) : commence à .0
        var iterCount = existingDocs.Count(d => d.Code == request.Code);
        var baseLibelle = System.Text.RegularExpressions.Regex.Replace(modele.Libelle ?? "", @"\.\d+$", "");
        modele.Libelle = $"{baseLibelle}.{iterCount}";

        var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formStruct.Id);
        List<SopalTrace.Domain.Entities.RefFormulaireColonneDef>? activeCols = null;
        if (form != null)
        {
            activeCols = (await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByCodeReferenceAsync(form.CodeReference))?.ToList();
        }

        if (activeCols != null)
        {
            foreach (var sec in modele.ModeleFabricationSections)
            {
                foreach (var lig in sec.ModeleFabricationLignes)
                {
                    // If Mapper already added ExtraColonnes from JSON, we don't need to add them again, or maybe we just ensure all active columns exist
                    var existingKeys = lig.ModeleFabricationLigneExtraColonnes.Select(c => c.CleColonne).ToList();
                    foreach (var colDef in activeCols)
                    {
                        if (!existingKeys.Contains(colDef.CleColonne))
                        {
                            lig.ModeleFabricationLigneExtraColonnes.Add(new ModeleFabricationLigneExtraColonne
                            {
                                Id = Guid.NewGuid(),
                                LigneId = lig.Id,
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

        await _unitOfWork.ModeleFabricationEnteteRepository.AddAsync(modele);
        await _unitOfWork.CommitAsync();

        return modele.Id;
    }

    public async Task<Guid> CreerNouvelleVersionModeleAsync(NouvelleVersionModeleRequestDto request)
    {
        var existingModele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(request.AncienId, includeRelations: true);
        if (existingModele == null) throw new Exception("Modèle introuvable");

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
                : existingModele.ModeleFabricationSections.Select(s => new SectionModeleEditDto
                {
                    LibelleSection = s.LibelleSection,
                    OrdreAffiche = s.OrdreAffiche,
                    Lignes = s.ModeleFabricationLignes.Select(l => new LigneModeleEditDto
                    {
                        OrdreAffiche = l.OrdreAffiche,
                        TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                        LibelleAffiche = l.LibelleAffiche,
                        TypeControleId = l.TypeControleId,
                        MoyenControleId = l.MoyenControleId,
                        MoyenTexteLibre = string.IsNullOrWhiteSpace(l.MoyenTexteLibre) ? null : l.MoyenTexteLibre,
                        InstrumentCode = l.InstrumentCode,
                        PeriodiciteId = l.PeriodiciteId,
                        LimiteSpecTexte = l.LimiteSpecTexte,
                        EstCritique = l.EstCritique,
                        Instruction = l.Instruction,
                        Observations = l.Observations,
                        ImageBase64 = l.ImageBase64,
                        ColonnesSupplementaires = l.ModeleFabricationLigneExtraColonnes != null && l.ModeleFabricationLigneExtraColonnes.Any()
                            ? System.Text.Json.JsonSerializer.Serialize(l.ModeleFabricationLigneExtraColonnes
                                .GroupBy(c => c.CleColonne).ToDictionary(g => g.Key, g => g.First().ValeurColonne))
                            : null
                    }).ToList()
                }).ToList()
        };

        return await CreerModeleAsync(createReq);
    }

    public async Task<bool> MettreAJourModeleAsync(Guid id, CreateModeleRequestDto request)
    {
        var modele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (modele == null) return false;

        var formStruct = await _formulaireStructureService.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION");
        if (formStruct == null) return false;

        var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formStruct.Id);
        List<SopalTrace.Domain.Entities.RefFormulaireColonneDef>? activeCols = null;
        if (form != null)
        {
            activeCols = (await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByCodeReferenceAsync(form.CodeReference))?.ToList();
        }

        var newEntityData = ModeleFabricationMapper.ToEntity(request, _currentUserService.UserInfo ?? "", formStruct.Id);

        if (formStruct.Version > modele.Version)
        {
            // LE PRC A CHANGE DE VERSION : ON ARCHIVE L'ANCIEN ET ON CREE UNE NOUVELLE VERSION DU MODELE
            modele.Statut = "ARCHIVE";
            await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(modele);

            var newModele = newEntityData;
            newModele.Version = formStruct.Version; // PREND TOUJOURS LA VERSION DU FORM
            
            if (activeCols != null)
            {
                foreach (var sec in newModele.ModeleFabricationSections)
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
            await _unitOfWork.ModeleFabricationEnteteRepository.AddAsync(newModele);
        }
        else
        {
            // LA VERSION N'A PAS CHANGE : MISE A JOUR IN-PLACE DU MODELE
            modele.Notes = request.Notes;
            modele.LegendeMoyens = request.LegendeMoyens;
            modele.OperationCode = request.OperationCode ?? modele.OperationCode;

            if (modele.ModeleFabricationSections != null)
            {
                foreach (var section in modele.ModeleFabricationSections.ToList())
                    _unitOfWork.ModeleFabricationEnteteRepository.RemoveSection(section);
                modele.ModeleFabricationSections.Clear();
            }
            else
            {
                modele.ModeleFabricationSections = new List<ModeleFabricationSection>();
            }
            
            foreach (var s in newEntityData.ModeleFabricationSections)
            {
                s.ModeleEnteteId = modele.Id; // relink
                modele.ModeleFabricationSections.Add(s);
            }

            if (activeCols != null)
            {
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
            await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(modele);
        }

        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<Guid> RestaurerModeleArchiveAsync(RestaurerModeleRequestDto request)
    {
        var modele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(request.ModeleArchiveId, includeRelations: true);
        if (modele == null) throw new Exception("Modèle introuvable");

        var maxVersion = await _unitOfWork.ModeleFabricationEnteteRepository.GetLatestVersionAsync(modele.Code, modele.OperationCode, modele.NatureArticleCode, modele.FamilleProduitFiniCode);
        
        var createReq = new CreateModeleRequestDto
        {
            Code = modele.Code,
            Libelle = modele.Libelle,
            TypeRobinetCode = "",
            NatureComposantCode = modele.NatureArticleCode ?? "",
            FamilleProduitCode = modele.FamilleProduitFiniCode,
            OperationCode = modele.OperationCode ?? "",
            VersionInitiale = maxVersion + 1,
            LegendeMoyens = modele.LegendeMoyens,
            Notes = modele.Notes,
            RefFormulaireCodeReference = modele.Formulaire?.CodeReference,
            Sections = modele.ModeleFabricationSections.Select(s => new SectionModeleEditDto
            {
                LibelleSection = s.LibelleSection,
                OrdreAffiche = s.OrdreAffiche,
                Lignes = s.ModeleFabricationLignes.Select(l => new LigneModeleEditDto
                {
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                    LibelleAffiche = l.LibelleAffiche,
                    TypeControleId = l.TypeControleId,
                    MoyenControleId = l.MoyenControleId,
                    MoyenTexteLibre = string.IsNullOrWhiteSpace(l.MoyenTexteLibre) ? null : l.MoyenTexteLibre,
                    InstrumentCode = l.InstrumentCode,
                    PeriodiciteId = l.PeriodiciteId,
                    LimiteSpecTexte = l.LimiteSpecTexte,
                    EstCritique = l.EstCritique,
                    Instruction = l.Instruction,
                    Observations = l.Observations,
                    ImageBase64 = l.ImageBase64,
                    ColonnesSupplementaires = l.ModeleFabricationLigneExtraColonnes != null && l.ModeleFabricationLigneExtraColonnes.Any()
                        ? System.Text.Json.JsonSerializer.Serialize(l.ModeleFabricationLigneExtraColonnes.ToDictionary(c => c.CleColonne, c => c.ValeurColonne))
                        : null
                }).ToList()
            }).ToList()
        };

        return await CreerModeleAsync(createReq);
    }

    public async Task<bool> SupprimerModeleAsync(Guid id)
    {
        var modele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (modele == null) return false;

        await _unitOfWork.ModeleFabricationEnteteRepository.DeleteAsync(modele);
        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task ArchiverModelesByFormulaireAsync(Guid formulaireId)
    {
        var modelesFabrication = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFormulaireIdAsync(formulaireId);
        foreach (var modele in modelesFabrication.Where(m => m.Statut == "ACTIF"))
        {
            modele.Statut = "ARCHIVE";
            await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(modele);
        }
    }
}
