using SopalTrace.Application.DTOs.QualityPlans.Fabrication;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanFabricationMapper
{
    public static PlanFabricationEnteteDto ToDto(PlanFabricationEntete plan, string? configJson = null)
    {
        if (plan == null) return null!;

        return new PlanFabricationEnteteDto
        {
            Id = plan.Id,
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
            ModeleSourceId = plan.ModeleSourceId,
            Sections = plan.PlanFabricationSections?.Select(ToSectionDto).ToList() ?? new List<PlanFabricationSectionDto>()
        };
    }

    public static PlanFabricationSectionDto ToSectionDto(PlanFabricationSection s)
    {
        return new PlanFabricationSectionDto
        {
            Id = s.Id,
            LibelleSection = s.LibelleSection,
            OrdreAffiche = s.OrdreAffiche,
            TypeSectionId = s.TypeSectionId,
            PeriodiciteId = s.PeriodiciteId,
            RegleEchantillonnageId = s.RegleEchantillonnageId,
            Lignes = s.PlanFabricationLignes?.Select(ToLigneDto).ToList() ?? new List<PlanFabricationLigneDto>()
        };
    }

    public static PlanFabricationLigneDto ToLigneDto(PlanFabricationLigne l)
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
            ImageBase64 = l.ImageBase64,
            ExtraColonnes = l.PlanFabricationLigneExtraColonnes?.Select(c => new PlanFabricationLigneExtraColonneDto
            {
                Id = c.Id,
                CleColonne = c.CleColonne,
                ValeurColonne = c.ValeurColonne,
                OrdreAffiche = c.OrdreAffiche
            }).ToList() ?? new List<PlanFabricationLigneExtraColonneDto>()
        };
    }

    public static PlanFabricationEnteteDto ToSummaryDto(PlanFabricationEntete p)
    {
        return new PlanFabricationEnteteDto
        {
            Id = p.Id,
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

    public static PlanFabricationSection ToSectionEntity(
        CreatePlanFabricationSectionDto s,
        PlanFabricationEntete plan,
        Guid? periodiciteId,
        List<RefFormulaireColonneDef>? activeCols)
    {
        var sectionId = Guid.NewGuid();
        var planSec = new PlanFabricationSection
        {
            Id = sectionId,
            PlanEnteteId = plan.Id,
            PlanEntete = plan,
            LibelleSection = s.LibelleSection ?? "",
            OrdreAffiche = s.OrdreAffiche,
            TypeSectionId = s.TypeSectionId,
            PeriodiciteId = periodiciteId,
            RegleEchantillonnageId = s.RegleEchantillonnageId,
            PlanFabricationLignes = new List<PlanFabricationLigne>()
        };

        if (s.Lignes != null)
        {
            foreach (var l in s.Lignes)
            {
                planSec.PlanFabricationLignes.Add(ToLigneEntity(l, plan, planSec, activeCols));
            }
        }

        return planSec;
    }

    public static PlanFabricationLigne ToLigneEntity(
        CreatePlanFabricationLigneDto l,
        PlanFabricationEntete plan,
        PlanFabricationSection section,
        List<RefFormulaireColonneDef>? activeCols)
    {
        var ligneId = Guid.NewGuid();
        var planLigne = new PlanFabricationLigne
        {
            Id = ligneId,
            PlanEnteteId = plan.Id,
            PlanEntete = plan,
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
                    Ligne = planLigne,
                    CleColonne = colDef.CleColonne,
                    ValeurColonne = val,
                    OrdreAffiche = 0
                });
            }
        }

        return planLigne;
    }

    public static CreatePlanFabricationRequestDto ToCreatePlanRequest(PlanFabricationEntete existingPlan, NouvelleVersionPlanFabricationRequestDto request)
    {
        return new CreatePlanFabricationRequestDto
        {
            Nom = !string.IsNullOrWhiteSpace(request.Nom) ? request.Nom : existingPlan.CodeArticleSageVersionne,
            Designation = existingPlan.Designation,
            OperationCode = existingPlan.OperationCode,
            VersionInitiale = request.VersionInitiale,
            Statut = request.Statut,
            CodeArticleSageVersionne = request.CodeArticleSageVersionne,
            LegendeMoyens = request.LegendeMoyens ?? existingPlan.LegendeMoyens,
            RefFormulaireCodeReference = request.RefFormulaireCodeReference ?? existingPlan.Formulaire?.CodeReference,
            ModeleSourceId = request.ModeleSourceId ?? existingPlan.ModeleSourceId,
            ConfigurationColonnesJson = request.ConfigurationColonnesJson,
            Sections = request.Sections != null && request.Sections.Any()
                ? request.Sections
                : existingPlan.PlanFabricationSections?.Select(ToCreateSectionDto).ToList() ?? new List<CreatePlanFabricationSectionDto>()
        };
    }

    public static CreatePlanFabricationRequestDto ToCreatePlanRequest(PlanFabricationEntete archivePlan, int newVersion)
    {
        return new CreatePlanFabricationRequestDto
        {
            Nom = archivePlan.CodeArticleSageVersionne ?? string.Empty,
            Designation = archivePlan.Designation,
            OperationCode = archivePlan.OperationCode,
            VersionInitiale = newVersion,
            LegendeMoyens = archivePlan.LegendeMoyens,
            RefFormulaireCodeReference = archivePlan.Formulaire?.CodeReference,
            ModeleSourceId = archivePlan.ModeleSourceId,
            Sections = archivePlan.PlanFabricationSections?.Select(ToCreateSectionDto).ToList() ?? new List<CreatePlanFabricationSectionDto>()
        };
    }

    public static CreatePlanFabricationSectionDto ToCreateSectionDto(PlanFabricationSection s)
    {
        return new CreatePlanFabricationSectionDto
        {
            LibelleSection = s.LibelleSection,
            OrdreAffiche = s.OrdreAffiche,
            TypeSectionId = s.TypeSectionId,
            PeriodiciteId = s.PeriodiciteId,
            RegleEchantillonnageId = s.RegleEchantillonnageId,
            Lignes = s.PlanFabricationLignes?.Select(ToCreateLigneDto).ToList() ?? new List<CreatePlanFabricationLigneDto>()
        };
    }

    public static CreatePlanFabricationLigneDto ToCreateLigneDto(PlanFabricationLigne l)
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
            ImageBase64 = l.ImageBase64,
            ExtraColonnes = l.PlanFabricationLigneExtraColonnes?.Select(c => new CreatePlanFabricationExtraColonneDto
            {
                CleColonne = c.CleColonne,
                ValeurColonne = c.ValeurColonne
            }).ToList() ?? new List<CreatePlanFabricationExtraColonneDto>()
        };
    }
}
