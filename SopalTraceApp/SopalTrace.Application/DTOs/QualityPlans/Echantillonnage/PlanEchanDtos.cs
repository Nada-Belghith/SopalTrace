#nullable disable
using System;

namespace SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;

// ==========================================
// REQUÊTES (Création & Versioning)
// ==========================================
public record CreatePlanEchanRequestDto
{
    public string CodeReference { get; init; }
    public string CodeArticleSage { get; init; }
    public string MachineCode { get; init; }
    public Guid? FormulaireId { get; init; }
    public string NiveauControle { get; init; }
    public string TypePlan { get; init; }
    public string ModeControle { get; init; }
    public int NqaId { get; init; }
    public string CommentaireVersion { get; init; }
}

public record NouvelleVersionEchanRequestDto
{
    public Guid AncienId { get; init; }
    public string ModifiePar { get; init; }
    public string MotifModification { get; init; }
}

// NOUVEAU DTO : Pour mettre à jour la V2 (Brouillon) et l'activer
public record UpdatePlanEchanRequestDto
{
    public string NiveauControle { get; init; }
    public string TypePlan { get; init; }
    public string ModeControle { get; init; }
    public int NqaId { get; init; }
    public Guid? FormulaireId { get; init; }
}

// ==========================================
// RÉPONSES (Lecture GET)
// ==========================================
public record PlanEchanResponseDto
{
    public Guid Id { get; init; }
    public string CodeReference { get; init; }
    public string CodeArticleSage { get; init; }
    public string MachineCode { get; init; }
    public Guid? FormulaireId { get; init; }
    public string CodeFormulaire { get; init; }
    public string NiveauControle { get; init; }
    public string TypePlan { get; init; }
    public string ModeControle { get; init; }
    public int NqaId { get; init; }
    public int Version { get; init; }
    public string Statut { get; init; }
    public string CreePar { get; init; }
    public DateTime CreeLe { get; init; }
    public string ModifiePar { get; init; }
    public DateTime? ModifieLe { get; init; }
    public string CommentaireVersion { get; init; }
}