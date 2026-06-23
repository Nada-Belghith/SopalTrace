using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
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

    public async Task<DocumentEnteteDto?> GetModeleByIdAsync(Guid id)
    {
        var modele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (modele == null) return null;

        // Basic mapping for view
        return new DocumentEnteteDto
        {
            Id = modele.Id,
            TypeDocumentCode = "MODELE_FAB",
            Nom = modele.Libelle,
            Designation = modele.Notes,
            Version = modele.Version,
            Statut = modele.Statut,
            OperationCode = modele.OperationCode,
            FormulaireId = modele.FormulaireId,
            FormulaireCodeReference = modele?.Formulaire?.CodeReference,
            LegendeMoyens = modele?.LegendeMoyens,
            Remarques = modele?.Notes,
            CreePar = modele?.CreePar ?? "",
            CreeLe = modele?.CreeLe ?? DateTime.UtcNow,
            NatureArticleCode = modele?.NatureArticleCode,
            FamilleProduitFiniCode = modele?.FamilleProduitFiniCode,
            Sections = modele?.ModeleFabricationSections?.Select(s => new DocumentSectionDto
            {
                Id = s.Id,
                LibelleSection = s.LibelleSection,
                Lignes = s.ModeleFabricationLignes?.Select(l => new DocumentLigneDto
                {
                    Id = l.Id,
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
                    // Handle extra columns mapping dynamically if needed
                    ExtraColonnes = l.ModeleFabricationLigneExtraColonnes?.Select(c => new DocumentExtraColonneDto {
                        CleColonne = c.CleColonne,
                        ValeurColonne = c.ValeurColonne,
                        OrdreAffiche = c.OrdreAffiche
                    }).ToList() ?? new List<DocumentExtraColonneDto>()
                }).ToList() ?? new List<DocumentLigneDto>()
            }).ToList() ?? new List<DocumentSectionDto>()
        };
    }

    public async Task<Guid> CreerModeleAsync(CreateDocumentRequestDto request)
    {
        var user = _currentUserService.UserInfo ?? "";
        var existingDocs = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFiltersAsync(request.NatureArticleCode, request.OperationCode, request.FamilleProduitFiniCode);
        
        var existingDoc = existingDocs.Where(d => d.Code == request.Nom)
                                      .OrderByDescending(d => d.Version)
                                      .FirstOrDefault();

        bool forceArchive = request.VersionInitiale.HasValue && request.VersionInitiale.Value != (existingDoc?.Version ?? -1);
        int finalVersion = request.VersionInitiale ?? 0;

        if (existingDoc != null)
        {
            if (existingDoc.Statut == "ACTIF")
            {
                existingDoc.Statut = "ARCHIVE";
                await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(existingDoc);
            }
            var maxVersion = existingDoc.Version;
            finalVersion = (request.VersionInitiale.HasValue && request.VersionInitiale.Value > maxVersion) ? request.VersionInitiale.Value : (maxVersion + 1);
        }

        Guid? formulaireId = null;
        var formStruct = await _formulaireStructureService.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION");
        if (formStruct != null)
        {
            formulaireId = formStruct.Id;
            var colsJson = request.ConfigurationColonnesJson ?? (request.ColonneDefs != null && request.ColonneDefs.Any()
                ? System.Text.Json.JsonSerializer.Serialize(request.ColonneDefs, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                : null);

            if (colsJson != null)
            {
                await _formulaireStructureService.UpdateFormulaireStructureAsync(
                    "EN_COURS_DE_FABRICATION", 
                    colsJson, 
                    formStruct.CodeReference, 
                    request.VersionInitiale
                );
            }
        }

        var modele = new ModeleFabricationEntete
        {
            Id = Guid.NewGuid(),
            Code = request.Nom,
            Libelle = request.Nom,
            Notes = request.Designation,
            Version = finalVersion,
            Statut = "ACTIF", // Les modèles n'ont pas de statut brouillon
            OperationCode = request.OperationCode ?? "",
            FormulaireId = formulaireId,
            LegendeMoyens = request.LegendeMoyens,
            CreePar = user,
            CreeLe = DateTime.UtcNow,
            NatureArticleCode = request.NatureArticleCode ?? "",
            FamilleProduitFiniCode = request.FamilleProduitFiniCode
        };

        if (formulaireId.HasValue)
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId.Value);
            if (form != null)
            {
                modele.Version = form.Version; // La version hérite de la version PRC
                
                var activeCols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByCodeReferenceAsync(form.CodeReference);
                if (request.Sections != null)
                {
                    foreach (var s in request.Sections)
                    {
                        var modSec = new ModeleFabricationSection
                        {
                            Id = Guid.NewGuid(),
                            ModeleEnteteId = modele.Id,
                            LibelleSection = s.LibelleSection ?? ""
                        };
                        
                        if (s.Lignes != null)
                        {
                            foreach (var l in s.Lignes)
                            {
                                var modLigne = new ModeleFabricationLigne
                                {
                                    Id = Guid.NewGuid(),
                                    SectionId = modSec.Id,
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
                                    ImageBase64 = l.ImageBase64
                                };

                                if (activeCols != null)
                                {
                                    foreach (var colDef in activeCols)
                                    {
                                        string? val = l.ExtraColonnes?.FirstOrDefault(c => c.CleColonne == colDef.CleColonne)?.ValeurColonne;
                                        modLigne.ModeleFabricationLigneExtraColonnes.Add(new ModeleFabricationLigneExtraColonne
                                        {
                                            Id = Guid.NewGuid(),
                                            LigneId = modLigne.Id,
                                            CleColonne = colDef.CleColonne,
                                            ValeurColonne = val,
                                            OrdreAffiche = 0
                                        });
                                    }
                                }
                                modSec.ModeleFabricationLignes.Add(modLigne);
                            }
                        }
                        modele.ModeleFabricationSections.Add(modSec);
                    }
                }
            }
        }

        await _unitOfWork.ModeleFabricationEnteteRepository.AddAsync(modele);
        await _unitOfWork.CommitAsync();

        return modele.Id;
    }

    public async Task<Guid> CreerNouvelleVersionModeleAsync(NouvelleVersionDocumentRequestDto request)
    {
        var existingModele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(request.AncienId, includeRelations: true);
        if (existingModele == null) throw new Exception("Modèle introuvable");

        var createReq = new CreateDocumentRequestDto
        {
            TypeDocumentCode = "MODELE_FAB",
            Nom = existingModele.Code,
            Designation = existingModele.Notes,
            NatureArticleCode = existingModele.NatureArticleCode,
            FamilleProduitFiniCode = existingModele.FamilleProduitFiniCode,
            OperationCode = existingModele.OperationCode,
            VersionInitiale = request.VersionInitiale,
            LegendeMoyens = existingModele.LegendeMoyens,
            RefFormulaireCodeReference = existingModele.Formulaire?.CodeReference,
            Sections = existingModele.ModeleFabricationSections.Select(s => new CreateDocumentSectionDto
            {
                LibelleSection = s.LibelleSection,
                Lignes = s.ModeleFabricationLignes.Select(l => new CreateDocumentLigneDto
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
                    ExtraColonnes = l.ModeleFabricationLigneExtraColonnes.Select(c => new CreateDocumentExtraColonneDto { CleColonne = c.CleColonne, ValeurColonne = c.ValeurColonne }).ToList()
                }).ToList()
            }).ToList()
        };

        return await CreerModeleAsync(createReq);
    }

    public async Task<bool> MettreAJourModeleAsync(Guid id, UpdateDocumentRequestDto request)
    {
        var modele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (modele == null) return false;

        modele.Notes = request.Remarques;
        modele.LegendeMoyens = request.LegendeMoyens;
        modele.OperationCode = request.OperationCode ?? modele.OperationCode;

        await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(modele);
        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<Guid> RestaurerModeleArchiveAsync(RestaurerDocumentRequestDto request)
    {
        var modele = await _unitOfWork.ModeleFabricationEnteteRepository.GetByIdAsync(request.DocumentArchiveId, includeRelations: true);
        if (modele == null) throw new Exception("Modèle introuvable");

        var maxVersion = await _unitOfWork.ModeleFabricationEnteteRepository.GetLatestVersionAsync(modele.Code, modele.OperationCode, modele.NatureArticleCode, modele.FamilleProduitFiniCode);
        
        var createReq = new CreateDocumentRequestDto
        {
            TypeDocumentCode = "MODELE_FAB",
            Nom = modele.Code,
            Designation = modele.Notes,
            NatureArticleCode = modele.NatureArticleCode,
            FamilleProduitFiniCode = modele.FamilleProduitFiniCode,
            OperationCode = modele.OperationCode,
            VersionInitiale = maxVersion + 1,
            LegendeMoyens = modele.LegendeMoyens,
            RefFormulaireCodeReference = modele.Formulaire?.CodeReference,
            Sections = modele.ModeleFabricationSections.Select(s => new CreateDocumentSectionDto
            {
                LibelleSection = s.LibelleSection,
                Lignes = s.ModeleFabricationLignes.Select(l => new CreateDocumentLigneDto
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
                    ExtraColonnes = l.ModeleFabricationLigneExtraColonnes.Select(c => new CreateDocumentExtraColonneDto { CleColonne = c.CleColonne, ValeurColonne = c.ValeurColonne }).ToList()
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
}
