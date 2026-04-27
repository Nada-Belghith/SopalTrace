using SopalTrace.Application.DTOs.QualityPlans.PlansNC;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanNcMapper
{
    public static PlanNcResponseDto MapperEntiteVersDto(PlanNcEntete plan)
    {
        return new PlanNcResponseDto
        {
            Id = plan.Id,
            PosteCode = plan.PosteCode,
            Nom = plan.Nom,
            Version = plan.Version ?? 0,
            Statut = plan.Statut ?? "",
            CreePar = plan.CreePar,
            CreeLe = plan.CreeLe ?? DateTime.MinValue,
            Lignes = plan.PlanNcLignes.Select(l => new LigneNcResponseDto
            {
                Id = l.Id,
                OrdreAffiche = l.OrdreAffiche,
                MachineCode = l.MachineCode,
                RisqueDefautId = l.RisqueDefautId,
                LibelleDefaut = l.RisqueDefaut?.LibelleDefaut ?? "Inconnu"
            }).ToList()
        };
    }

    public static PlanNcLigne ConstruireNouvelleLigne(Guid planId, LigneNcEditDto dto)
    {
        return new PlanNcLigne
        {
            Id = Guid.NewGuid(),
            PlanNcenteteId = planId,
            OrdreAffiche = dto.OrdreAffiche,
            MachineCode = dto.MachineCode,
            RisqueDefautId = dto.RisqueDefautId
        };
    }

    public static void MettreAJourLigne(PlanNcLigne ligne, LigneNcEditDto dto)
    {
        ligne.OrdreAffiche = dto.OrdreAffiche;
        ligne.MachineCode = dto.MachineCode;
        ligne.RisqueDefautId = dto.RisqueDefautId;
    }

    public static PlanNcEntete DupliquerEntitePlan(PlanNcEntete source, string modifiePar, string motif)
    {
        var planId = Guid.NewGuid();
        return new PlanNcEntete
        {
            Id = planId,
            PosteCode = source.PosteCode,
            Nom = source.Nom,
            Version = source.Version + 1,
            Statut = "BROUILLON",
            CreePar = modifiePar,
            CreeLe = DateTime.UtcNow,
            PlanNcLignes = source.PlanNcLignes.Select(l => new PlanNcLigne
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
