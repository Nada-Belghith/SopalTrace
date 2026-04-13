using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;

// --- CRÉATION ---
public record CreatePlanAssRequestDto
{
    public required string OperationCode { get; init; }
    public required string TypeRobinetCode { get; init; }
    public required bool EstModele { get; init; }
    public string? CodeArticleSage { get; init; }
    public required string Nom { get; init; }
    public string? CommentaireVersion { get; init; }
}

// --- ÉDITION (ARBRE COMPLET) ---
public record SectionAssEditDto
{
    public Guid? Id { get; set; }
    public required int OrdreAffiche { get; init; }
    public required Guid TypeSectionId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public required string LibelleSection { get; init; }
    public string? NormeReference { get; init; }
    public int? NqaId { get; init; }
    public string? Notes { get; init; }
    public List<LigneAssEditDto> Lignes { get; init; } = new();
}

public record LigneAssEditDto
{
    public Guid? Id { get; set; }
    public required int OrdreAffiche { get; init; }
    public required Guid TypeCaracteristiqueId { get; init; }
    public required string LibelleAffiche { get; init; }
    public required Guid TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public Guid? GroupeInstrumentId { get; init; }
    public string? InstrumentCode { get; init; }
    public double? ValeurNominale { get; init; }
    public double? ToleranceSuperieure { get; init; }
    public double? ToleranceInferieure { get; init; }
    public string? Unite { get; init; }
    public string? LimiteSpecTexte { get; init; }
    public string? Instruction { get; init; }
    public string? Observations { get; init; }
    public required bool EstCritique { get; init; }
}

// --- ACTIONS MÉTIER ---
public record ChangePlanAssStatusRequestDto
{
    public required string NouveauStatut { get; init; }
    public string? Motif { get; init; }
}

/// <summary>
/// DTO pour cloner une exception depuis un plan maître
/// </summary>
public record CloneExceptionAssRequestDto
{
    public required Guid PlanMaitreId { get; init; }
    public required string NouveauCodeArticleSage { get; init; }
    public required string CreePar { get; init; }
    public string? MotifClonage { get; init; }
}

/// <summary>
/// DTO pour créer une nouvelle version d'un plan
/// </summary>
public record NouvelleVersionAssRequestDto
{
    public required Guid AncienId { get; init; }
    public required string CreePar { get; init; }
    public string? MotifModification { get; init; }
}

// --- LECTURE ---
public record PlanAssResponseDto
{
    public required Guid Id { get; init; }
    public required string OperationCode { get; init; }
    public required string TypeRobinetCode { get; init; }
    public required bool EstModele { get; init; }
    public string? CodeArticleSage { get; init; }
    public string? Designation { get; init; }
    public required string Nom { get; init; }
    public required int Version { get; init; }
    public required string Statut { get; init; }
    public DateTime? DateApplication { get; init; }
    public required string CreePar { get; init; }
    public required DateTime CreeLe { get; init; }
    public string? ModifiePar { get; init; }
    public DateTime? ModifieLe { get; init; }
    public string? CommentaireVersion { get; init; }
    public List<SectionAssResponseDto> Sections { get; init; } = new();
}

public record SectionAssResponseDto
{
    public required Guid Id { get; init; }
    public required Guid TypeSectionId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public required string LibelleSection { get; init; }
    public required int OrdreAffiche { get; init; }
    public string? NormeReference { get; init; }
    public int? NqaId { get; init; }
    public string? Notes { get; init; }
    public List<LigneAssResponseDto> Lignes { get; init; } = new();
}

public record LigneAssResponseDto
{
    public required Guid Id { get; init; }
    public required int OrdreAffiche { get; init; }
    public required Guid TypeCaracteristiqueId { get; init; }
    public required string LibelleAffiche { get; init; }
    public required Guid TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public Guid? GroupeInstrumentId { get; init; }
    public string? InstrumentCode { get; init; }
    public double? ValeurNominale { get; init; }
    public double? ToleranceSuperieure { get; init; }
    public double? ToleranceInferieure { get; init; }
    public string? Unite { get; init; }
    public string? LimiteSpecTexte { get; init; }
    public string? Observations { get; init; }
    public string? Instruction { get; init; }
    public required bool EstCritique { get; init; }
}