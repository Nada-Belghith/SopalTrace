#nullable disable
using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;

// ==========================================
// REQUÊTES (Création & Versioning)
// ==========================================
public record CreatePlanEchanRequestDto
{
    public string NiveauControle { get; init; }
    public string TypePlan { get; init; }
    public string ModeControle { get; init; }
    
    // ✅ Seule la valeur directe est conservée (NqaId est définitivement supprimé)
    public int? NqaId { get; init; }
    public double? ValeurNqa { get; init; }
    
    public string CommentaireVersion { get; init; }
    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }
    public List<PlanEchanRegleDto> Regles { get; init; } = new();
}

public record PlanEchanRegleDto
{
    public Guid? Id { get; init; }
    public int? TailleMinLot { get; init; }
    public int? TailleMaxLot { get; init; }
    public string LettreCode { get; init; }
    
    // Champs remplis plus tard par l'opérateur (vaudront 0 par défaut à la création par l'admin)
    public int EffectifEchantillonA { get; init; }
    public int NbPostesB { get; init; }
    public int? EffectifParPosteAb { get; init; }
    
    // Champs remplis par l'admin
    public int CritereAcceptationAc { get; init; }
    public int CritereRejetRe { get; init; }
}

public record NouvelleVersionEchanRequestDto
{
    public Guid AncienId { get; init; }
    public string ModifiePar { get; init; }
    public string MotifModification { get; init; }
    
    // ✅ Toutes les données (entête + règles) sont dans ce DTO
    public UpdatePlanEchanRequestDto Donnees { get; init; }
}

public record RestaurerEchanRequestDto
{
    public Guid ArchiveId { get; init; }
    public string ModifiePar { get; init; }
    public string MotifRestauration { get; init; }
}

public record UpdatePlanEchanRequestDto
{
    public string NiveauControle { get; init; }
    public string TypePlan { get; init; }
    public string ModeControle { get; init; }
    
    // ✅ Suppression de NqaId ici aussi
    public int? NqaId { get; init; }
    public double? ValeurNqa { get; init; }
    
    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }
    public string ModifiePar { get; init; }
    public List<PlanEchanRegleDto> Regles { get; init; } = new();
}

// ==========================================
// RÉPONSES (Lecture GET)
// ==========================================
public record PlanEchanResponseDto
{
    public Guid Id { get; init; }
    public string NiveauControle { get; init; }
    public string TypePlan { get; init; }
    public string ModeControle { get; init; }
    
    // ✅ Suppression de NqaId ici aussi
    public int NqaId { get; init; }
    public double ValeurNqa { get; init; }
    
    public int Version { get; init; }
    public string Statut { get; init; }
    public string CreePar { get; init; }
    public DateTime CreeLe { get; init; }
    public string ModifiePar { get; init; }
    public DateTime? ModifieLe { get; init; }
    public string CommentaireVersion { get; init; }
    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }
    public List<PlanEchanRegleDto> Regles { get; init; } = new();
}