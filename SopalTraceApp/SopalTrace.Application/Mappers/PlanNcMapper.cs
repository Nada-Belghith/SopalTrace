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
            TypeRobinetCode = plan.TypeRobinetCode,
            OperationCode = plan.OperationCode,
            PosteCode = plan.PosteCode,
            FormulaireId = plan.FormulaireId,
            Nom = plan.Nom,
            Version = plan.Version,
            Statut = plan.Statut,
            CreePar = plan.CreePar,
            CreeLe = plan.CreeLe,
            ModifiePar = plan.ModifiePar,
            ModifieLe = plan.ModifieLe,
            CommentaireVersion = plan.CommentaireVersion,
            Colonnes = plan.PlanNcColonnes.Select(c => new ColonneNcResponseDto
            {
                Id = c.Id,
                OrdreAffiche = c.OrdreAffiche,
                MachineCode = c.MachineCode,
                LibelleDefaut = c.LibelleDefaut
            }).ToList()
        };
    }

    public static PlanNcColonne ConstruireNouvelleColonne(Guid planId, ColonneNcEditDto dto)
    {
        return new PlanNcColonne
        {
            Id = Guid.NewGuid(),
            PlanNcenteteId = planId,
            OrdreAffiche = dto.OrdreAffiche,
            MachineCode = dto.MachineCode,
            LibelleDefaut = dto.LibelleDefaut
        };
    }

    public static void MettreAJourColonne(PlanNcColonne colonne, ColonneNcEditDto dto)
    {
        colonne.OrdreAffiche = dto.OrdreAffiche;
        colonne.MachineCode = dto.MachineCode;
        colonne.LibelleDefaut = dto.LibelleDefaut;
    }

    public static PlanNcEntete DupliquerEntitePlan(PlanNcEntete source, string modifiePar, string motif)
    {
        var planId = Guid.NewGuid();
        return new PlanNcEntete
        {
            Id = planId,
            TypeRobinetCode = source.TypeRobinetCode,
            OperationCode = source.OperationCode,
            PosteCode = source.PosteCode,
            FormulaireId = source.FormulaireId,
            Nom = source.Nom,
            Version = source.Version + 1,
            Statut = "BROUILLON",
            CreePar = modifiePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = motif,
            PlanNcColonnes = source.PlanNcColonnes.Select(c => new PlanNcColonne
            {
                Id = Guid.NewGuid(),
                PlanNcenteteId = planId,
                OrdreAffiche = c.OrdreAffiche,
                MachineCode = c.MachineCode,
                LibelleDefaut = c.LibelleDefaut
            }).ToList()
        };
    }
}
