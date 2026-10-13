using SopalTrace.Application.DTOs.QualityPlans.Fabrication;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanFabricationMapper
{
    public static PlanFabricationEnteteDto ToDto(DocumentEntete plan, string? configJson = null)
    {
        if (plan == null) return null!;

        return new PlanFabricationEnteteDto
        {
            Id = plan.Id,
            RowVersion = VersionsDocument.EnTexte(plan.RowVersion),
            Nom = plan.Nom,
            CodeArticleSageVersionne = plan.CodeArticleSageVersionne,
            Designation = plan.Designation,
            Version = plan.Version,
            Statut = plan.Statut ?? "BROUILLON",
            OperationCode = plan.OperationCode,
            FormulaireId = plan.FormulaireId,
            FormulaireCodeReference = plan.Formulaire?.CodeReference,
            FormulaireVersion = plan.Formulaire?.Version,
            LegendeMoyens = plan.LegendeMoyens,
            Remarques = plan.Remarques,
            ConfigurationColonnesJson = configJson,
            CreePar = plan.CreePar ?? "",
            CreeLe = plan.CreeLe,
            ModeleSourceId = plan.DocumentSourceId,
            Sections = plan.DocumentSections?.Select(ToSectionDto).ToList() ?? new List<PlanFabricationSectionDto>()
        };
    }

    public static PlanFabricationSectionDto ToSectionDto(DocumentSection s)
    {
        return new PlanFabricationSectionDto
        {
            Id = s.Id,
            LibelleSection = s.LibelleSection,
            OrdreAffiche = s.OrdreAffiche,
            TypeSectionId = s.TypeSectionId,
            PeriodiciteId = s.PeriodiciteId,
            Lignes = s.DocumentLignes?.Select(ToLigneDto).ToList() ?? new List<PlanFabricationLigneDto>()
        };
    }

    public static PlanFabricationLigneDto ToLigneDto(DocumentLigne l)
    {
        return new PlanFabricationLigneDto
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
            ImageBase64 = l.EnDataUrl(),
            ExtraColonnes = l.DocumentLigneExtraColonnes?.Select(c => new PlanFabricationLigneExtraColonneDto
            {
                Id = c.Id,
                CleColonne = c.CleColonne,
                ValeurColonne = c.ValeurColonne,
                OrdreAffiche = c.OrdreAffiche
            }).ToList() ?? new List<PlanFabricationLigneExtraColonneDto>()
        };
    }

    public static PlanFabricationEnteteDto ToSummaryDto(DocumentEntete p)
    {
        return new PlanFabricationEnteteDto
        {
            Id = p.Id,
            RowVersion = VersionsDocument.EnTexte(p.RowVersion),
            Nom = p.Nom,
            CodeArticleSageVersionne = p.CodeArticleSageVersionne,
            Designation = p.Designation ?? p.Remarques,
            Version = p.Version,
            Statut = p.Statut,
            OperationCode = p.OperationCode,
            FormulaireId = p.FormulaireId,
            FormulaireCodeReference = p.Formulaire?.CodeReference,
            FormulaireVersion = p.Formulaire?.Version,
            CreePar = p.CreePar ?? "",
            CreeLe = p.CreeLe,
        };
    }

    public static DocumentSection ToSectionEntity(
        CreatePlanFabricationSectionDto s,
        DocumentEntete plan,
        Guid? periodiciteId,
        List<RefFormulaireColonneDef>? activeCols)
    {
        var sectionId = Guid.NewGuid();
        var planSec = new DocumentSection
        {
            Id = sectionId,
            EnteteId = plan.Id,
            Entete = plan,
            LibelleSection = s.LibelleSection ?? "",
            OrdreAffiche = s.OrdreAffiche,
            TypeSectionId = s.TypeSectionId,
            PeriodiciteId = periodiciteId,
            DocumentLignes = new List<DocumentLigne>()
        };

        if (s.Lignes != null)
        {
            foreach (var l in s.Lignes)
            {
                planSec.DocumentLignes.Add(ToLigneEntity(l, plan, planSec, activeCols));
            }
        }

        return planSec;
    }

    public static DocumentLigne ToLigneEntity(
        CreatePlanFabricationLigneDto l,
        DocumentEntete plan,
        DocumentSection section,
        List<RefFormulaireColonneDef>? activeCols)
    {
        var ligneId = Guid.NewGuid();
        var planLigne = new DocumentLigne
        {
            Id = ligneId,
            EnteteId = plan.Id,
            Entete = plan,
            SectionId = section.Id,
            Section = section,
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
            DocumentLineImage = ImagesLigne.Creer(l.ImageBase64),
            DocumentLigneExtraColonnes = new List<DocumentLigneExtraColonne>()
        };

        SopalTrace.Application.Utilities.LineCleanupHelper.CleanupPlanFabLine(planLigne);

        if (activeCols != null)
        {
            foreach (var colDef in activeCols)
            {
                string? val = l.ExtraColonnes?.FirstOrDefault(c => c.CleColonne == colDef.CleColonne)?.ValeurColonne;
                planLigne.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                {
                    Id = Guid.NewGuid(),
                    LigneId = ligneId,
                    Ligne = planLigne,
                    CleColonne = colDef.CleColonne,
                    ValeurColonne = val,
                    OrdreAffiche = 0
                });
            }
        }

        return planLigne;
    }

    public static CreatePlanFabricationRequestDto ToCreatePlanRequest(DocumentEntete existingPlan, NouvelleVersionPlanFabricationRequestDto request)
    {
        return new CreatePlanFabricationRequestDto
        {
            Nom = !string.IsNullOrWhiteSpace(request.Nom) ? request.Nom : existingPlan.CodeArticleSageVersionne ?? string.Empty,
            Designation = existingPlan.Designation,
            OperationCode = existingPlan.OperationCode,
            VersionInitiale = request.VersionInitiale,
            Statut = request.Statut,
            CodeArticleSageVersionne = request.CodeArticleSageVersionne,
            LegendeMoyens = request.LegendeMoyens ?? existingPlan.LegendeMoyens,
            RefFormulaireCodeReference = request.RefFormulaireCodeReference ?? existingPlan.Formulaire?.CodeReference,
            ModeleSourceId = request.ModeleSourceId ?? existingPlan.DocumentSourceId,
            ConfigurationColonnesJson = request.ConfigurationColonnesJson,
            Sections = request.Sections != null && request.Sections.Any()
                ? request.Sections
                : existingPlan.DocumentSections?.Select(ToCreateSectionDto).ToList() ?? new List<CreatePlanFabricationSectionDto>()
        };
    }

    public static CreatePlanFabricationRequestDto ToCreatePlanRequest(DocumentEntete archivePlan, int newVersion)
    {
        return new CreatePlanFabricationRequestDto
        {
            Nom = archivePlan.CodeArticleSageVersionne ?? string.Empty,
            Designation = archivePlan.Designation,
            OperationCode = archivePlan.OperationCode,
            VersionInitiale = newVersion,
            LegendeMoyens = archivePlan.LegendeMoyens,
            RefFormulaireCodeReference = archivePlan.Formulaire?.CodeReference,
            ModeleSourceId = archivePlan.DocumentSourceId,
            Sections = archivePlan.DocumentSections?.Select(ToCreateSectionDto).ToList() ?? new List<CreatePlanFabricationSectionDto>()
        };
    }

    public static CreatePlanFabricationSectionDto ToCreateSectionDto(DocumentSection s)
    {
        return new CreatePlanFabricationSectionDto
        {
            LibelleSection = s.LibelleSection,
            OrdreAffiche = s.OrdreAffiche,
            TypeSectionId = s.TypeSectionId,
            PeriodiciteId = s.PeriodiciteId,
            Lignes = s.DocumentLignes?.Select(ToCreateLigneDto).ToList() ?? new List<CreatePlanFabricationLigneDto>()
        };
    }

    public static CreatePlanFabricationLigneDto ToCreateLigneDto(DocumentLigne l)
    {
        return new CreatePlanFabricationLigneDto
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
            ImageBase64 = l.EnDataUrl(),
            ExtraColonnes = l.DocumentLigneExtraColonnes?.Select(c => new CreatePlanFabricationExtraColonneDto
            {
                CleColonne = c.CleColonne,
                ValeurColonne = c.ValeurColonne
            }).ToList() ?? new List<CreatePlanFabricationExtraColonneDto>()
        };
    }

    public static DocumentSection CreateSection(CreatePlanFabricationSectionDto dto, Guid planId)
    {
        return new DocumentSection
        {
            Id = Guid.NewGuid(),
            EnteteId = planId,
            LibelleSection = dto.LibelleSection ?? "",
            OrdreAffiche = dto.OrdreAffiche,
            TypeSectionId = dto.TypeSectionId,
            PeriodiciteId = dto.PeriodiciteId,
            DocumentLignes = new List<DocumentLigne>()
        };
    }

    public static void UpdateSection(DocumentSection sec, CreatePlanFabricationSectionDto dto)
    {
        sec.OrdreAffiche = dto.OrdreAffiche;
        sec.LibelleSection = dto.LibelleSection ?? "";
        sec.TypeSectionId = dto.TypeSectionId;
        sec.PeriodiciteId = dto.PeriodiciteId;
    }

    public static DocumentLigne CreateLigne(CreatePlanFabricationLigneDto dto, Guid planId, Guid sectionId)
    {
        var planLigne = new DocumentLigne
        {
            Id = Guid.NewGuid(),
            EnteteId = planId,
            SectionId = sectionId,
            OrdreAffiche = dto.OrdreAffiche,
            CaracteristiqueId = dto.CaracteristiqueId,
            LibelleAffiche = dto.LibelleAffiche,
            TypeCaracteristiqueId = dto.TypeCaracteristiqueId,
            TypeControleId = dto.TypeControleId,
            MoyenControleId = dto.MoyenControleId,
            MoyenTexteLibre = string.IsNullOrWhiteSpace(dto.MoyenTexteLibre) ? null : dto.MoyenTexteLibre,
            InstrumentCode = dto.InstrumentCode,
            PeriodiciteId = dto.PeriodiciteId,
            LimiteSpecTexte = dto.LimiteSpecTexte,
            EstCritique = dto.EstCritique,
            Instruction = dto.Instruction,
            Observations = dto.Observations,
            DocumentLineImage = ImagesLigne.Creer(dto.ImageBase64),
            DocumentLigneExtraColonnes = new List<DocumentLigneExtraColonne>()
        };

        if (dto.ExtraColonnes != null)
        {
            foreach (var ec in dto.ExtraColonnes)
            {
                planLigne.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                {
                    Id = Guid.NewGuid(),
                    LigneId = planLigne.Id,
                    CleColonne = ec.CleColonne,
                    ValeurColonne = ec.ValeurColonne,
                    OrdreAffiche = 0
                });
            }
        }

        SopalTrace.Application.Utilities.LineCleanupHelper.CleanupPlanFabLine(planLigne);
        return planLigne;
    }

    public static void UpdateLigne(DocumentLigne lig, CreatePlanFabricationLigneDto dto)
    {
        lig.OrdreAffiche = dto.OrdreAffiche;
        lig.CaracteristiqueId = dto.CaracteristiqueId;
        lig.LibelleAffiche = dto.LibelleAffiche;
        lig.TypeCaracteristiqueId = dto.TypeCaracteristiqueId;
        lig.TypeControleId = dto.TypeControleId;
        lig.MoyenControleId = dto.MoyenControleId;
        lig.MoyenTexteLibre = string.IsNullOrWhiteSpace(dto.MoyenTexteLibre) ? null : dto.MoyenTexteLibre;
        lig.InstrumentCode = dto.InstrumentCode;
        lig.PeriodiciteId = dto.PeriodiciteId;
        lig.LimiteSpecTexte = dto.LimiteSpecTexte;
        lig.EstCritique = dto.EstCritique;
        lig.Instruction = dto.Instruction;
        lig.Observations = dto.Observations;
        lig.DefinirImage(dto.ImageBase64);

        SopalTrace.Application.Utilities.LineCleanupHelper.CleanupPlanFabLine(lig);
    }

    public static void MergerExtraColonnes(DocumentLigne lig, List<CreatePlanFabricationExtraColonneDto> incoming, SopalTrace.Application.Interfaces.IUnitOfWork unitOfWork)
    {
        var existing = lig.DocumentLigneExtraColonnes.ToList();

        foreach (var ec in existing.Where(e => !incoming.Any(i => i.CleColonne == e.CleColonne)))
        {
            unitOfWork.PlanFabricationEnteteRepository.RemoveExtraColonne(ec);
            lig.DocumentLigneExtraColonnes.Remove(ec);
        }

        foreach (var inc in incoming)
        {
            var found = existing.FirstOrDefault(e => e.CleColonne == inc.CleColonne);
            if (found != null)
            {
                found.ValeurColonne = inc.ValeurColonne;
                // Assuming OrdreAffiche can be updated if present in DTO, otherwise keep it or reset
            }
            else
            {
                var nouvelle = new DocumentLigneExtraColonne
                {
                    Id = Guid.NewGuid(),
                    LigneId = lig.Id,
                    Ligne = lig,
                    CleColonne = inc.CleColonne,
                    ValeurColonne = inc.ValeurColonne,
                    OrdreAffiche = 0
                };
                lig.DocumentLigneExtraColonnes.Add(nouvelle);
                unitOfWork.Ajouter(nouvelle);
            }
        }
    }
}
