using SopalTrace.Application.DTOs.QualityPlans.ControlePoste;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class ControlePosteMapper
{
    public static ControlePosteResponseDto MapperEntiteVersDto(PlanControlePosteEntete plan)
    {
        return new ControlePosteResponseDto
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
            
            Lignes = plan.PlanControlePosteLignes.Select(l => new LigneNcResponseDto
            {
                Id = l.Id,
                OrdreAffiche = l.OrdreAffiche,
                MachineCode = l.MachineCode,
                RisqueDefautId = l.RisqueDefautId,
                LibelleDefaut = l.RisqueDefaut?.LibelleDefaut ?? "Inconnu"
            }).ToList()
        };
    }

    public static PlanControlePosteLigne ConstruireNouvelleLigne(Guid planId, LigneNcEditDto dto, Guid resolvedRisqueDefautId)
    {
        return new PlanControlePosteLigne
        {
            Id = Guid.NewGuid(),
            ControlePosteEnteteId = planId,
            OrdreAffiche = dto.OrdreAffiche,
            MachineCode = dto.MachineCode,
            RisqueDefautId = resolvedRisqueDefautId
        };
    }

    public static void MettreAJourLigne(PlanControlePosteLigne ligne, LigneNcEditDto dto, Guid resolvedRisqueDefautId)
    {
        ligne.OrdreAffiche = dto.OrdreAffiche;
        ligne.MachineCode = dto.MachineCode;
        ligne.RisqueDefautId = resolvedRisqueDefautId;
    }

    public static PlanControlePosteEntete DupliquerEntitePlan(PlanControlePosteEntete source, string modifiePar, string motif)
    {
        var planId = Guid.NewGuid();
        return new PlanControlePosteEntete
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
            PlanControlePosteLignes = source.PlanControlePosteLignes.Select(l => new PlanControlePosteLigne
            {
                Id = Guid.NewGuid(),
                ControlePosteEnteteId = planId,
                OrdreAffiche = l.OrdreAffiche,
                MachineCode = l.MachineCode,
                RisqueDefautId = l.RisqueDefautId
            }).ToList()
        };
    }
}
