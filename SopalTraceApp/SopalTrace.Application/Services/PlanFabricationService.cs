using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SopalTrace.Application.Services;

public class PlanFabricationService : IPlanFabricationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFormulaireStructureService _formulaireStructureService;

    public PlanFabricationService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFormulaireStructureService formulaireStructureService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _formulaireStructureService = formulaireStructureService;
    }

    public async Task<DocumentEnteteDto?> GetPlanByIdAsync(Guid id)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (plan == null) return null;

        return new DocumentEnteteDto
        {
            Id = plan.Id,
            TypeDocumentCode = "PLAN_FAB",
            Nom = plan.Nom,
            Designation = plan.Designation,
            Version = plan.Version,
            Statut = plan.Statut,
            OperationCode = plan.OperationCode,
            FormulaireId = plan.FormulaireId,
            FormulaireCodeReference = plan?.Formulaire?.CodeReference,
            LegendeMoyens = plan?.LegendeMoyens,
            Remarques = plan?.Remarques,
            CreePar = plan?.CreePar ?? "",
            CreeLe = plan?.CreeLe ?? DateTime.UtcNow,
            ModeleSourceId = plan?.ModeleSourceId,
            Sections = plan?.PlanFabricationSections?.Select(s => new DocumentSectionDto
            {
                Id = s.Id,
                LibelleSection = s.LibelleSection,
                Lignes = s.PlanFabricationLignes?.Select(l => new DocumentLigneDto
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
                    ExtraColonnes = l.PlanFabricationLigneExtraColonnes?.Select(c => new DocumentExtraColonneDto {
                        CleColonne = c.CleColonne,
                        ValeurColonne = c.ValeurColonne,
                        OrdreAffiche = c.OrdreAffiche
                    }).ToList() ?? new List<DocumentExtraColonneDto>()
                }).ToList() ?? new List<DocumentLigneDto>()
            }).ToList() ?? new List<DocumentSectionDto>()
        };
    }

    public async Task<Guid> CreerPlanAsync(CreateDocumentRequestDto request)
    {
        var user = _currentUserService.UserInfo ?? "";
        var existingDocs = await _unitOfWork.PlanFabricationEnteteRepository.GetByFiltersAsync(request.OperationCode);
        
        var codeArticleSageVersionne = request.Nom;
        var existingDoc = existingDocs.Where(d => d.CodeArticleSageVersionne == codeArticleSageVersionne)
                                      .OrderByDescending(d => d.Version)
                                      .FirstOrDefault();

        bool forceArchive = request.VersionInitiale.HasValue && request.VersionInitiale.Value != (existingDoc?.Version ?? -1);
        int finalVersion = request.VersionInitiale ?? 0;

        if (existingDoc != null)
        {
            if (existingDoc.Statut == "BROUILLON" && !forceArchive)
            {
                var fullDraft = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(existingDoc.Id, includeRelations: true);
                if (fullDraft != null)
                {
                    await _unitOfWork.PlanFabricationEnteteRepository.DeleteAsync(fullDraft);
                }
                finalVersion = existingDoc.Version;
            }
            else
            {
                if (existingDoc.Statut == "ACTIF")
                {
                    existingDoc.Statut = "ARCHIVE";
                    await _unitOfWork.PlanFabricationEnteteRepository.UpdateAsync(existingDoc);
                }
                var maxVersion = existingDoc.Version;
                finalVersion = (request.VersionInitiale.HasValue && request.VersionInitiale.Value > maxVersion) ? request.VersionInitiale.Value : (maxVersion + 1);
            }
        }

        Guid? formulaireId = null;
        var formStruct = await _formulaireStructureService.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION");
        if (formStruct != null)
        {
            formulaireId = formStruct.Id;
        }

        var plan = new PlanFabricationEntete
        {
            Id = Guid.NewGuid(),
            CodeArticleSageVersionne = codeArticleSageVersionne,
            Nom = codeArticleSageVersionne,
            Designation = request.Designation,
            Version = finalVersion,
            Statut = (existingDoc != null && existingDoc.Statut == "BROUILLON" && !forceArchive) ? "BROUILLON" : "ACTIF",
            OperationCode = request.OperationCode,
            FormulaireId = formulaireId,
            LegendeMoyens = request.LegendeMoyens,
            CreePar = user,
            CreeLe = DateTime.UtcNow,
            ModeleSourceId = request.ModeleSourceId
        };

        if (formulaireId.HasValue)
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId.Value);
            if (form != null)
            {
                plan.Version = form.Version; // Hérite de la version du formulaire
                
                var activeCols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByCodeReferenceAsync(form.CodeReference);
                if (request.Sections != null)
                {
                    foreach (var s in request.Sections)
                    {
                        var planSec = new PlanFabricationSection
                        {
                            Id = Guid.NewGuid(),
                            PlanEnteteId = plan.Id,
                            LibelleSection = s.LibelleSection ?? ""
                        };
                        
                        if (s.Lignes != null)
                        {
                            foreach (var l in s.Lignes)
                            {
                                var planLigne = new PlanFabricationLigne
                                {
                                    Id = Guid.NewGuid(),
                                    SectionId = planSec.Id,
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
                                        planLigne.PlanFabricationLigneExtraColonnes.Add(new PlanFabricationLigneExtraColonne
                                        {
                                            Id = Guid.NewGuid(),
                                            LigneId = planLigne.Id,
                                            CleColonne = colDef.CleColonne,
                                            ValeurColonne = val,
                                            OrdreAffiche = 0
                                        });
                                    }
                                }
                                planSec.PlanFabricationLignes.Add(planLigne);
                            }
                        }
                        plan.PlanFabricationSections.Add(planSec);
                    }
                }
            }
        }

        await _unitOfWork.PlanFabricationEnteteRepository.AddAsync(plan);
        await _unitOfWork.CommitAsync();

        return plan.Id;
    }

    public async Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionDocumentRequestDto request)
    {
        var existingPlan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(request.AncienId, includeRelations: true);
        if (existingPlan == null) throw new Exception("Plan introuvable");

        var createReq = new CreateDocumentRequestDto
        {
            TypeDocumentCode = "PLAN_FAB",
            Nom = existingPlan.CodeArticleSageVersionne,
            Designation = existingPlan.Designation,
            OperationCode = existingPlan.OperationCode,
            VersionInitiale = request.VersionInitiale,
            LegendeMoyens = existingPlan.LegendeMoyens,
            RefFormulaireCodeReference = existingPlan.Formulaire?.CodeReference,
            ModeleSourceId = existingPlan.ModeleSourceId,
            Sections = existingPlan.PlanFabricationSections.Select(s => new CreateDocumentSectionDto
            {
                LibelleSection = s.LibelleSection,
                Lignes = s.PlanFabricationLignes.Select(l => new CreateDocumentLigneDto
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
                    ExtraColonnes = l.PlanFabricationLigneExtraColonnes.Select(c => new CreateDocumentExtraColonneDto { CleColonne = c.CleColonne, ValeurColonne = c.ValeurColonne }).ToList()
                }).ToList()
            }).ToList()
        };

        return await CreerPlanAsync(createReq);
    }

    public async Task<bool> MettreAJourPlanAsync(Guid id, UpdateDocumentRequestDto request)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (plan == null) return false;

        plan.Remarques = request.Remarques;
        plan.LegendeMoyens = request.LegendeMoyens;
        plan.OperationCode = request.OperationCode ?? plan.OperationCode;

        await _unitOfWork.PlanFabricationEnteteRepository.UpdateAsync(plan);
        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<Guid> RestaurerPlanArchiveAsync(RestaurerDocumentRequestDto request)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(request.DocumentArchiveId, includeRelations: true);
        if (plan == null) throw new Exception("Plan introuvable");

        var maxVersion = await _unitOfWork.PlanFabricationEnteteRepository.GetLatestVersionAsync(plan.CodeArticleSageVersionne, plan.OperationCode);
        
        var createReq = new CreateDocumentRequestDto
        {
            TypeDocumentCode = "PLAN_FAB",
            Nom = plan.CodeArticleSageVersionne,
            Designation = plan.Designation,
            OperationCode = plan.OperationCode,
            VersionInitiale = maxVersion + 1,
            LegendeMoyens = plan.LegendeMoyens,
            RefFormulaireCodeReference = plan.Formulaire?.CodeReference,
            ModeleSourceId = plan.ModeleSourceId,
            Sections = plan.PlanFabricationSections.Select(s => new CreateDocumentSectionDto
            {
                LibelleSection = s.LibelleSection,
                Lignes = s.PlanFabricationLignes.Select(l => new CreateDocumentLigneDto
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
                    ExtraColonnes = l.PlanFabricationLigneExtraColonnes.Select(c => new CreateDocumentExtraColonneDto { CleColonne = c.CleColonne, ValeurColonne = c.ValeurColonne }).ToList()
                }).ToList()
            }).ToList()
        };

        return await CreerPlanAsync(createReq);
    }

    public async Task<bool> SupprimerPlanAsync(Guid id)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (plan == null) return false;

        await _unitOfWork.PlanFabricationEnteteRepository.DeleteAsync(plan);
        await _unitOfWork.CommitAsync();
        return true;
    }
}
