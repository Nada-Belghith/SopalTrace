using SopalTrace.Application.DTOs.QualityPlans.Fabrication;
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
    private readonly IFrequencyParserService _frequencyParserService;

    public PlanFabricationService(
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

    public async Task<PlanFabricationEnteteDto?> GetPlanByIdAsync(Guid id)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (plan == null) return null;

        return new PlanFabricationEnteteDto
        {
            Id = plan.Id,
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
            Sections = plan?.PlanFabricationSections?.Select(s => new PlanFabricationSectionDto
            {
                Id = s.Id,
                LibelleSection = s.LibelleSection,
                OrdreAffiche = s.OrdreAffiche,
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,
                RegleEchantillonnageId = s.RegleEchantillonnageId,
                Lignes = s.PlanFabricationLignes?.Select(l => new PlanFabricationLigneDto
                {
                    Id = l.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    CaracteristiqueId = l.CaracteristiqueId,
                    LibelleAffiche = l.LibelleAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
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
                    ExtraColonnes = l.PlanFabricationLigneExtraColonnes?.Select(c => new PlanFabricationLigneExtraColonneDto
                    {
                        Id = c.Id,
                        CleColonne = c.CleColonne,
                        ValeurColonne = c.ValeurColonne,
                        OrdreAffiche = c.OrdreAffiche
                    }).ToList() ?? new List<PlanFabricationLigneExtraColonneDto>()
                }).ToList() ?? new List<PlanFabricationLigneDto>()
            }).ToList() ?? new List<PlanFabricationSectionDto>()
        };
    }

    public async Task<IReadOnlyList<PlanFabricationEnteteDto>> GetPlansByFiltersAsync(string? natureComposantCode = null, string? operationCode = null, string? familleProduitCode = null, string? statut = null, string? codeArticleSageVersionne = null)
    {
        var plans = await _unitOfWork.PlanFabricationEnteteRepository.GetByFiltersAsync(operationCode);
        
        if (!string.IsNullOrWhiteSpace(statut))
        {
            plans = plans.Where(p => p.Statut == statut);
        }

        if (!string.IsNullOrWhiteSpace(codeArticleSageVersionne))
        {
            plans = plans.Where(p => p.CodeArticleSageVersionne == codeArticleSageVersionne);
        }

        return plans.Select(p => new PlanFabricationEnteteDto
        {
            Id = p.Id,
            Nom = p.CodeArticleSageVersionne,
            Designation = p.Designation ?? p.Remarques,
            Version = p.Version,
            Statut = p.Statut,
            OperationCode = p.OperationCode,
            FormulaireId = p.FormulaireId,
            FormulaireCodeReference = p.Formulaire?.CodeReference,
            CreePar = p.CreePar ?? "",
            CreeLe = p.CreeLe,
        }).ToList();
    }

    public async Task<Guid> CreerPlanAsync(CreatePlanFabricationRequestDto request)
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
                if (request.Statut != "BROUILLON")
                {
                    var activeDocs = existingDocs.Where(d => d.CodeArticleSageVersionne == codeArticleSageVersionne && d.Statut == "ACTIF").ToList();
                    foreach (var act in activeDocs)
                    {
                        act.Statut = "ARCHIVE";
                    }
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

        var iterCount = existingDocs.Count(d => d.CodeArticleSageVersionne == codeArticleSageVersionne);
        var baseNom = System.Text.RegularExpressions.Regex.Replace(request.Nom ?? "", @"\.\d+$", "");

        var plan = new PlanFabricationEntete
        {
            Id = Guid.NewGuid(),
            CodeArticleSageVersionne = codeArticleSageVersionne,
            Nom = $"{baseNom}.{iterCount}",
            Designation = request.Designation,
            Version = finalVersion,
            Statut = !string.IsNullOrWhiteSpace(request.Statut) ? request.Statut : ((existingDoc != null && existingDoc.Statut == "BROUILLON" && !forceArchive) ? "BROUILLON" : "ACTIF"),
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
                            PlanEntete = plan,
                            LibelleSection = s.LibelleSection ?? "",
                            OrdreAffiche = s.OrdreAffiche,
                            TypeSectionId = s.TypeSectionId,
                            PeriodiciteId = s.PeriodiciteId ?? (await _frequencyParserService.ResolveOrCreatePeriodiciteFromTextAsync(s.LibelleSection ?? string.Empty)),
                            RegleEchantillonnageId = s.RegleEchantillonnageId
                        };
                        
                        if (s.Lignes != null)
                        {
                            foreach (var l in s.Lignes)
                            {
                                var planLigne = new PlanFabricationLigne
                                {
                                    Id = Guid.NewGuid(),
                                    PlanEnteteId = plan.Id,
                                    PlanEntete = plan,
                                    SectionId = planSec.Id,
                                    Section = planSec,
                                    OrdreAffiche = l.OrdreAffiche,
                                    CaracteristiqueId = l.CaracteristiqueId,
                                    LibelleAffiche = l.LibelleAffiche,
                                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                                    TypeControleId = l.TypeControleId,
                                    MoyenControleId = l.MoyenControleId,
                                    MoyenTexteLibre = string.IsNullOrWhiteSpace(l.MoyenTexteLibre) ? null : l.MoyenTexteLibre,
                                    InstrumentCode = l.InstrumentCode,
                                    PeriodiciteId = l.PeriodiciteId,
                                    LimiteSpecTexte = l.LimiteSpecTexte,
                                    EstCritique = l.EstCritique,
                                    Instruction = l.Instruction,
                                    Observations = l.Observations,
                                    ImageBase64 = l.ImageBase64
                                };

                                SopalTrace.Application.Utilities.LineCleanupHelper.CleanupPlanFabLine(planLigne);

                                if (activeCols != null)
                                {
                                    foreach (var colDef in activeCols)
                                    {
                                        string? val = l.ExtraColonnes?.FirstOrDefault(c => c.CleColonne == colDef.CleColonne)?.ValeurColonne;
                                        planLigne.PlanFabricationLigneExtraColonnes.Add(new PlanFabricationLigneExtraColonne
                                        {
                                            Id = Guid.NewGuid(),
                                            LigneId = planLigne.Id,
                                            Ligne = planLigne,
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

    public async Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionPlanFabricationRequestDto request)
    {
        var existingPlan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(request.AncienId, includeRelations: true);
        if (existingPlan == null) throw new Exception("Plan introuvable");
        var createReq = new CreatePlanFabricationRequestDto
        {
            Nom = existingPlan.CodeArticleSageVersionne,
            Designation = existingPlan.Designation,
            OperationCode = existingPlan.OperationCode,
            VersionInitiale = request.VersionInitiale,
            LegendeMoyens = request.LegendeMoyens ?? existingPlan.LegendeMoyens,
            RefFormulaireCodeReference = request.RefFormulaireCodeReference ?? existingPlan.Formulaire?.CodeReference,
            ModeleSourceId = request.ModeleSourceId ?? existingPlan.ModeleSourceId,
            ConfigurationColonnesJson = request.ConfigurationColonnesJson,
            Sections = request.Sections != null && request.Sections.Any()
                ? request.Sections
                : existingPlan.PlanFabricationSections.Select(s => new CreatePlanFabricationSectionDto
                {
                    LibelleSection = s.LibelleSection,
                    OrdreAffiche = s.OrdreAffiche,
                    TypeSectionId = s.TypeSectionId,
                    PeriodiciteId = s.PeriodiciteId,
                    RegleEchantillonnageId = s.RegleEchantillonnageId,
                    Lignes = s.PlanFabricationLignes.Select(l => new CreatePlanFabricationLigneDto
                    {
                        OrdreAffiche = l.OrdreAffiche,
                        CaracteristiqueId = l.CaracteristiqueId,
                        LibelleAffiche = l.LibelleAffiche,
                        TypeCaracteristiqueId = l.TypeCaracteristiqueId,
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
                        ExtraColonnes = l.PlanFabricationLigneExtraColonnes.Select(c => new CreatePlanFabricationExtraColonneDto { CleColonne = c.CleColonne, ValeurColonne = c.ValeurColonne }).ToList()
                    }).ToList()
                }).ToList()
        };

        return await CreerPlanAsync(createReq);
    }

    public async Task<bool> MettreAJourPlanAsync(Guid id, UpdatePlanFabricationRequestDto request)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (plan == null) return false;

        // Mettre à jour les champs de l'entête
        plan.Remarques = request.Remarques;
        plan.LegendeMoyens = request.LegendeMoyens;
        plan.OperationCode = request.OperationCode ?? plan.OperationCode;
        plan.ModifiePar = _currentUserService.UserInfo ?? "";
        plan.ModifieLe = DateTime.UtcNow;

        // La version reste toujours celle du formulaire PRC associé
        var formStruct = await _formulaireStructureService.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION");
        List<SopalTrace.Domain.Entities.RefFormulaireColonneDef>? activeCols = null;
        if (formStruct != null)
        {
            plan.FormulaireId = formStruct.Id;
            plan.Version = formStruct.Version;
            var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formStruct.Id);
            if (form != null)
                activeCols = (await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByCodeReferenceAsync(form.CodeReference))?.ToList();
        }

        // Supprimer les anciennes sections et lignes explicitement
        if (plan.PlanFabricationSections != null && plan.PlanFabricationSections.Any())
        {
            foreach (var section in plan.PlanFabricationSections.ToList())
            {
                if (section.PlanFabricationLignes != null)
                {
                    foreach (var ligne in section.PlanFabricationLignes.ToList())
                    {
                        if (ligne.PlanFabricationLigneExtraColonnes != null)
                        {
                            foreach (var ext in ligne.PlanFabricationLigneExtraColonnes.ToList())
                            {
                                _unitOfWork.PlanFabricationEnteteRepository.RemoveExtraColonne(ext);
                            }
                            ligne.PlanFabricationLigneExtraColonnes.Clear();
                        }
                        _unitOfWork.PlanFabricationEnteteRepository.RemoveLigne(ligne);
                    }
                    section.PlanFabricationLignes.Clear();
                }
                _unitOfWork.PlanFabricationEnteteRepository.RemoveSection(section);
            }

            plan.PlanFabricationSections.Clear();
            await _unitOfWork.FlushDeletesAsync();
        }

        if (plan.PlanFabricationSections == null)
        {
            plan.PlanFabricationSections = new List<PlanFabricationSection>();
        }

        if (request.Sections != null)
        {
            foreach (var s in request.Sections)
            {
                var sectionId = Guid.NewGuid();
                var planSec = new PlanFabricationSection
                {
                    Id = sectionId,
                    PlanEnteteId = plan.Id,
                    LibelleSection = s.LibelleSection ?? "",
                    OrdreAffiche = s.OrdreAffiche,
                    TypeSectionId = s.TypeSectionId,
                    PeriodiciteId = s.PeriodiciteId ?? (await _frequencyParserService.ResolveOrCreatePeriodiciteFromTextAsync(s.LibelleSection ?? string.Empty)),
                    RegleEchantillonnageId = s.RegleEchantillonnageId,
                    PlanFabricationLignes = new List<PlanFabricationLigne>()
                };

                if (s.Lignes != null)
                {
                    foreach (var l in s.Lignes)
                    {
                        var ligneId = Guid.NewGuid();
                        var planLigne = new PlanFabricationLigne
                        {
                            Id = ligneId,
                            PlanEnteteId = plan.Id,
                            SectionId = sectionId,
                            OrdreAffiche = l.OrdreAffiche,
                            CaracteristiqueId = l.CaracteristiqueId,
                            LibelleAffiche = l.LibelleAffiche,
                            TypeCaracteristiqueId = l.TypeCaracteristiqueId,
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
                            PlanFabricationLigneExtraColonnes = new List<PlanFabricationLigneExtraColonne>()
                        };

                        SopalTrace.Application.Utilities.LineCleanupHelper.CleanupPlanFabLine(planLigne);

                        if (activeCols != null)
                        {
                            foreach (var colDef in activeCols)
                            {
                                string? val = l.ExtraColonnes?.FirstOrDefault(c => c.CleColonne == colDef.CleColonne)?.ValeurColonne;
                                planLigne.PlanFabricationLigneExtraColonnes.Add(new PlanFabricationLigneExtraColonne
                                {
                                    Id = Guid.NewGuid(),
                                    LigneId = ligneId,
                                    CleColonne = colDef.CleColonne,
                                    ValeurColonne = val,
                                    OrdreAffiche = 0
                                });
                            }
                        }
                        planSec.PlanFabricationLignes.Add(planLigne);
                    }
                }

                // Utiliser AddSection pour que EF Core reconnaisse les nouvelles sections comme "Added"
                // (après un Clear(), le tracking est perdu)
                _unitOfWork.PlanFabricationEnteteRepository.AddSection(planSec);
            }
        }

        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<Guid> RestaurerPlanArchiveAsync(RestaurerDocumentRequestDto request)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(request.DocumentArchiveId, includeRelations: true);
        if (plan == null) throw new Exception("Plan introuvable");

        var maxVersion = await _unitOfWork.PlanFabricationEnteteRepository.GetLatestVersionAsync(plan.CodeArticleSageVersionne, plan.OperationCode);
        
        var createReq = new CreatePlanFabricationRequestDto
        {
            Nom = plan.CodeArticleSageVersionne,
            Designation = plan.Designation,
            OperationCode = plan.OperationCode,
            VersionInitiale = maxVersion + 1,
            LegendeMoyens = plan.LegendeMoyens,
            RefFormulaireCodeReference = plan.Formulaire?.CodeReference,
            ModeleSourceId = plan.ModeleSourceId,
            Sections = plan.PlanFabricationSections.Select(s => new CreatePlanFabricationSectionDto
            {
                LibelleSection = s.LibelleSection,
                OrdreAffiche = s.OrdreAffiche,
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,
                RegleEchantillonnageId = s.RegleEchantillonnageId,
                Lignes = s.PlanFabricationLignes.Select(l => new CreatePlanFabricationLigneDto
                {
                    OrdreAffiche = l.OrdreAffiche,
                    CaracteristiqueId = l.CaracteristiqueId,
                    LibelleAffiche = l.LibelleAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
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
                    ExtraColonnes = l.PlanFabricationLigneExtraColonnes.Select(c => new CreatePlanFabricationExtraColonneDto { CleColonne = c.CleColonne, ValeurColonne = c.ValeurColonne }).ToList()
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

    public async Task ArchiverPlansByFormulaireAsync(Guid formulaireId)
    {
        var plansFabrication = await _unitOfWork.PlanFabricationEnteteRepository.GetByFormulaireIdAsync(formulaireId);
        foreach (var plan in plansFabrication.Where(p => p.Statut == "ACTIF"))
        {
            plan.Statut = "ARCHIVE";
        }
        await _unitOfWork.CommitAsync();
    }
}
