#nullable disable
using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanEchanMapper
{
    public static PlanEchanResponseDto MapperEntiteVersDto(PlanEchantillonnageEntete entete)
    {
        return new PlanEchanResponseDto
        {
            Id = entete.Id,
            NiveauControle = entete.NiveauControle,
            TypePlan = entete.TypePlan,
            ModeControle = entete.ModeControle,
            
            // ✅ On récupère la valeur depuis la table NQA liée
            NqaId = entete.NqaId,
            ValeurNqa = entete.Nqa != null ? entete.Nqa.ValeurNqa : 0.0,
            
            Version = entete.Version,
            Statut = entete.Statut,
            CreePar = entete.CreePar,
            CreeLe = entete.CreeLe,
            ModifiePar = entete.ModifiePar,
            ModifieLe = entete.ModifieLe,
            CommentaireVersion = entete.CommentaireVersion,
            Remarques = entete.Remarques,
            LegendeMoyens = entete.LegendeMoyens,
            Regles = entete.PlanEchantillonnageRegles.Select(r => new PlanEchanRegleDto
            {
                Id = r.Id,
                TailleMinLot = r.TailleMinLot,
                TailleMaxLot = r.TailleMaxLot,
                LettreCode = r.LettreCode,
                EffectifEchantillonA = r.EffectifEchantillonA,
                NbPostesB = r.NbPostesB,
                EffectifParPosteAb = r.EffectifParPosteAb,
                CritereAcceptationAc = r.CritereAcceptationAc,
                CritereRejetRe = r.CritereRejetRe
            }).ToList()
        };
    }

    // ✅ On passe nqaIdDb en paramètre !
    public static PlanEchantillonnageEntete ConstruireNouveauPlan(CreatePlanEchanRequestDto dto, int nqaIdDb, string creePar)
    {
        var plan = new PlanEchantillonnageEntete
        {
            Id = Guid.NewGuid(),
            NiveauControle = dto.NiveauControle,
            TypePlan = dto.TypePlan,
            ModeControle = dto.ModeControle,
            
            // ✅ On assigne l'ID trouvé par le service
            NqaId = nqaIdDb, 
            
            Version = 1,
            Statut = StatutsPlan.Actif, 
            CreePar = creePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = dto.CommentaireVersion ?? "Création initiale",
            Remarques = dto.Remarques,
            LegendeMoyens = dto.LegendeMoyens
        };

        if (dto.Regles != null)
        {
            plan.PlanEchantillonnageRegles = dto.Regles.Select(r => new PlanEchantillonnageRegle
            {
                Id = Guid.NewGuid(),
                FicheEnteteId = plan.Id,
                TailleMinLot = r.TailleMinLot,
                TailleMaxLot = r.TailleMaxLot,
                LettreCode = r.LettreCode,
                EffectifEchantillonA = r.EffectifEchantillonA,
                NbPostesB = r.NbPostesB,
                EffectifParPosteAb = r.EffectifParPosteAb,
                CritereAcceptationAc = r.CritereAcceptationAc,
                CritereRejetRe = r.CritereRejetRe
            }).ToList();
        }

        return plan;
    }

    public static PlanEchantillonnageEntete DupliquerEntitePlan(PlanEchantillonnageEntete source, string modifiePar, string motif)
    {
        var nouveau = new PlanEchantillonnageEntete
        {
            Id = Guid.NewGuid(),
            NiveauControle = source.NiveauControle,
            TypePlan = source.TypePlan,
            ModeControle = source.ModeControle,
            NqaId = source.NqaId, // ✅ On garde le même ID NQA
            Version = source.Version + 1,
            Statut = StatutsPlan.Actif, // ✅ CORRECTION : Le nouveau plan naît directement ACTIF
            CreePar = modifiePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = motif,
            Remarques = source.Remarques,
            LegendeMoyens = source.LegendeMoyens
        };

        nouveau.PlanEchantillonnageRegles = source.PlanEchantillonnageRegles.Select(r => new PlanEchantillonnageRegle
        {
            Id = Guid.NewGuid(),
            FicheEnteteId = nouveau.Id,
            TailleMinLot = r.TailleMinLot,
            TailleMaxLot = r.TailleMaxLot,
            LettreCode = r.LettreCode,
            EffectifEchantillonA = r.EffectifEchantillonA,
            NbPostesB = r.NbPostesB,
            EffectifParPosteAb = r.EffectifParPosteAb,
            CritereAcceptationAc = r.CritereAcceptationAc,
            CritereRejetRe = r.CritereRejetRe
        }).ToList();

        return nouveau;
    }
}