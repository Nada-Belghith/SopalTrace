using SopalTrace.Application.DTOs.QualityPlans.PlansNC;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanNcMapper
{
    public static PlanNcResponseDto MapperEntiteVersDto(PlanNonConformiteEntete plan)
    {
        return new PlanNcResponseDto
        {
            Id = plan.Id,
            PosteCode = plan.PosteCode,
            Nom = plan.Nom,
            Version = plan.Version,
            Statut = plan.Statut,
            CreePar = plan.CreePar,
            CreeLe = plan.CreeLe,
            Remarques = plan.Remarques,
            LegendeMoyens = plan.LegendeMoyens,




            ConfigurationColonnesJson = plan.ConfigurationColonnesJson,
            FormulaireId = plan.FormulaireId,
            
            Lignes = plan.PlanNonConformiteLignes.Select(l => new LigneNcResponseDto
            {
                Id = l.Id,
                OrdreAffiche = l.OrdreAffiche,
                MachineCode = l.MachineCode,
                RisqueDefautId = l.RisqueDefautId,
                LibelleDefaut = l.RisqueDefaut?.LibelleDefaut ?? "Inconnu"
            }).ToList()
        };
    }

    public static PlanNonConformiteLigne ConstruireNouvelleLigne(Guid planId, LigneNcEditDto dto, Guid resolvedRisqueDefautId)
    {
        return new PlanNonConformiteLigne
        {
            Id = Guid.NewGuid(),
            PlanNcenteteId = planId,
            OrdreAffiche = dto.OrdreAffiche,
            MachineCode = dto.MachineCode,
            RisqueDefautId = resolvedRisqueDefautId
        };
    }

    public static void MettreAJourLigne(PlanNonConformiteLigne ligne, LigneNcEditDto dto, Guid resolvedRisqueDefautId)
    {
        ligne.OrdreAffiche = dto.OrdreAffiche;
        ligne.MachineCode = dto.MachineCode;
        ligne.RisqueDefautId = resolvedRisqueDefautId;
    }

    public static PlanNonConformiteEntete DupliquerEntitePlan(PlanNonConformiteEntete source, string modifiePar, string motif)
    {
        var planId = Guid.NewGuid();
        return new PlanNonConformiteEntete
        {
            Id = planId,
            PosteCode = source.PosteCode,
            Nom = source.Nom,
            Version = source.Version + 1,
            Statut = "BROUILLON",
            CreePar = modifiePar,
            CreeLe = DateTime.UtcNow,
            Remarques = source.Remarques,
            LegendeMoyens = source.LegendeMoyens,
            ConfigurationColonnesJson = source.ConfigurationColonnesJson,
            FormulaireId = source.FormulaireId,
            PlanNonConformiteLignes = source.PlanNonConformiteLignes.Select(l => new PlanNonConformiteLigne
            {
                Id = Guid.NewGuid(),
                PlanNcenteteId = planId,
                OrdreAffiche = l.OrdreAffiche,
                MachineCode = l.MachineCode,
                RisqueDefautId = l.RisqueDefautId
            }).ToList()
        };
    }
}
