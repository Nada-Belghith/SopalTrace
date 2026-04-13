#nullable disable
using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;

namespace SopalTrace.Application.Mappers;

public static class PlanEchanMapper
{
    public static PlanEchanResponseDto MapperEntiteVersDto(PlanEchantillonnageEntete entete)
    {
        return new PlanEchanResponseDto
        {
            Id = entete.Id,
            CodeReference = entete.CodeReference,
            CodeArticleSage = entete.CodeArticleSage,
            MachineCode = entete.MachineCode,
            FormulaireId = entete.FormulaireId,
            CodeFormulaire = entete.Formulaire?.CodeReference,
            NiveauControle = entete.NiveauControle,
            TypePlan = entete.TypePlan,
            ModeControle = entete.ModeControle,
            NqaId = entete.NqaId,
            Version = entete.Version,
            Statut = entete.Statut,
            CreePar = entete.CreePar,
            CreeLe = entete.CreeLe,
            ModifiePar = entete.CreePar,
            ModifieLe = entete.CreeLe,
            CommentaireVersion = entete.CommentaireVersion
        };
    }

    public static PlanEchantillonnageEntete ConstruireNouveauPlan(CreatePlanEchanRequestDto dto, string creePar)
    {
        return new PlanEchantillonnageEntete
        {
            Id = Guid.NewGuid(),
            CodeReference = dto.CodeReference,
            CodeArticleSage = string.IsNullOrWhiteSpace(dto.CodeArticleSage) ? null : dto.CodeArticleSage,
            MachineCode = string.IsNullOrWhiteSpace(dto.MachineCode) ? null : dto.MachineCode,
            FormulaireId = dto.FormulaireId,
            NiveauControle = dto.NiveauControle,
            TypePlan = dto.TypePlan,
            ModeControle = dto.ModeControle,
            NqaId = dto.NqaId,
            Version = 1,
            Statut = StatutsPlan.Actif, // La V1 est directement active
            CreePar = creePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = dto.CommentaireVersion ?? "Création initiale"
        };
    }

    public static PlanEchantillonnageEntete DupliquerEntitePlan(PlanEchantillonnageEntete source, string modifiePar, string motif)
    {
        return new PlanEchantillonnageEntete
        {
            Id = Guid.NewGuid(),
            CodeReference = source.CodeReference,
            CodeArticleSage = source.CodeArticleSage,
            MachineCode = source.MachineCode,
            FormulaireId = source.FormulaireId,
            NiveauControle = source.NiveauControle, // Sera modifiable via le PUT
            TypePlan = source.TypePlan,
            ModeControle = source.ModeControle,
            NqaId = source.NqaId,
            Version = source.Version + 1,
            Statut = StatutsPlan.Brouillon, // CORRECTION : La V2 naît en BROUILLON
            CreePar = modifiePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = motif
        };
    }
}