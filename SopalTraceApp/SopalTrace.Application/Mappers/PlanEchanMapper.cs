using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;
using SopalTrace.Domain.Entities;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanEchanMapper
{
    public static PlanEchanResponseDto? ToResponseDto(this DocumentEchantillonnageEntete? entity)
    {
        if (entity == null) return null;

        return new PlanEchanResponseDto
        {
            Id = entity.Id,
            NiveauControle = entity.NiveauControle,
            TypePlan = entity.TypePlan,
            ModeControle = entity.ModeControle,
            NqaId = entity.NqaId,
            ValeurNqa = 0, // Should be filled by service if needed
            Version = entity.Version,
            Statut = entity.Statut,
            CreePar = entity.CreePar,
            CreeLe = entity.CreeLe,
            ModifiePar = entity.ModifiePar,
            ModifieLe = entity.ModifieLe,
            CommentaireVersion = entity.CommentaireVersion,
            Remarques = entity.Remarques,
            LegendeMoyens = entity.LegendeMoyens,
            CritereAcceptationAc = entity.CritereAcceptationAc,
            CritereRejetRe = entity.CritereRejetRe,
            Regles = entity.DocumentEchantillonnageRegles?.Select(r => new PlanEchanRegleDto
            {
                Id = r.Id,
                TailleMinLot = r.TailleMinLot,
                TailleMaxLot = r.TailleMaxLot,
                LettreCode = r.LettreCode,
                EffectifEchantillonA = r.EffectifEchantillonA,
                NbPostesB = r.NbPostesB,
                EffectifParPosteAb = r.EffectifParPosteAb
            }).ToList() ?? new()
        };
    }

    public static DocumentEchantillonnageEntete? ToEntity(this CreatePlanEchanRequestDto? dto)
    {
        if (dto == null) return null;

        return new DocumentEchantillonnageEntete
        {
            NiveauControle = dto.NiveauControle,
            TypePlan = dto.TypePlan,
            ModeControle = dto.ModeControle,
            NqaId = dto.NqaId ?? 0,
            CommentaireVersion = dto.CommentaireVersion,
            Remarques = dto.Remarques,
            LegendeMoyens = dto.LegendeMoyens,
            CritereAcceptationAc = dto.CritereAcceptationAc,
            CritereRejetRe = dto.CritereRejetRe,
            DocumentEchantillonnageRegles = dto.Regles?.Select(r => new DocumentEchantillonnageRegle
            {
                TailleMinLot = r.TailleMinLot,
                TailleMaxLot = r.TailleMaxLot,
                LettreCode = r.LettreCode,
                EffectifEchantillonA = r.EffectifEchantillonA,
                NbPostesB = r.NbPostesB,
                EffectifParPosteAb = r.EffectifParPosteAb
            }).ToList() ?? new()
        };
    }

    public static void UpdateEntity(this DocumentEchantillonnageEntete entity, UpdatePlanEchanRequestDto dto)
    {
        if (entity == null || dto == null) return;

        entity.NiveauControle = dto.NiveauControle;
        entity.TypePlan = dto.TypePlan;
        entity.ModeControle = dto.ModeControle;
        entity.NqaId = dto.NqaId ?? 0;
        entity.Remarques = dto.Remarques;
        entity.LegendeMoyens = dto.LegendeMoyens;
        entity.CritereAcceptationAc = dto.CritereAcceptationAc;
        entity.CritereRejetRe = dto.CritereRejetRe;
        entity.ModifiePar = dto.ModifiePar;

        // Note: For rules, it is better to handle them in the service layer 
        // to manage added/updated/deleted properly, or completely clear and recreate.
        // For simplicity, we clear and recreate.
        entity.DocumentEchantillonnageRegles.Clear();

        if (dto.Regles != null)
        {
            foreach (var r in dto.Regles)
            {
                entity.DocumentEchantillonnageRegles.Add(new DocumentEchantillonnageRegle
                {
                    FicheEnteteId = entity.Id,
                    TailleMinLot = r.TailleMinLot,
                    TailleMaxLot = r.TailleMaxLot,
                    LettreCode = r.LettreCode,
                    EffectifEchantillonA = r.EffectifEchantillonA,
                    NbPostesB = r.NbPostesB,
                    EffectifParPosteAb = r.EffectifParPosteAb
                });
            }
        }
    }
}
