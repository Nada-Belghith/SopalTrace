#nullable enable
using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;

// --- CRÉATION ---
public record CreatePlanAssDto
{
    public required string OperationCode { get; init; }
    public string? TypeRobinetCode { get; init; }
    public required string NatureComposantCode { get; init; }
    public string? PosteCode { get; init; }
    public string? FamilleCode { get; init; }
    public required string Code { get; init; }
    public required bool EstModele { get; init; }
    public string? CodeArticleSage { get; init; }
    public required string Nom { get; init; }
    public string? CreePar { get; init; } = null;
    public string? LegendeMoyens { get; init; } = null;
    public string? Remarques { get; init; } = null;
    public int? VersionInitiale { get; init; } = null;
    public List<SectionAssEditDto> Sections { get; init; } = new();
}

public record CreatePlanAssRequestDto : CreatePlanAssDto;

// --- ÉDITION (ARBRE COMPLET) ---
public record SectionAssEditDto
{
    public Guid? Id { get; set; }
    public required int OrdreAffiche { get; init; }
    public Guid? TypeSectionId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public required string LibelleSection { get; init; }
    public string? NormeReference { get; init; } = null;
    public int? NqaId { get; init; }
    public string? Notes { get; init; } = null;
    public Guid? RegleEchantillonnageId { get; init; }
    public string? RegleEchantillonnageLibelle { get; init; }
    public string? FrequenceLibelle { get; init; }
    public string? ModeFreq { get; init; }
    public int? FreqNum { get; init; }
    public string? TypeVariable { get; init; }
    public int? FreqHours { get; init; }
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
    public string? InstrumentCode { get; init; } = null;
    public string? LimiteSpecTexte { get; init; } = null;
    public string? Instruction { get; init; } = null;
    public string? Observations { get; init; } = null;
    public string? MoyenTexteLibre { get; init; } = null;
    public bool EstCritique { get; init; } = false;
    public string? ColonnesSupplementaires { get; init; } = null;
    public string? ImageBase64 { get; init; } = null;
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
    public string? LegendeMoyens { get; init; }
    public string? Remarques { get; init; }
    public int? VersionInitiale { get; init; }
}

// --- LECTURE ---
public record PlanAssResponseDto
{
    public required Guid Id { get; init; }
    public required string OperationCode { get; init; }
    public string? TypeRobinetCode { get; init; }
    public required string NatureComposantCode { get; init; }
    public string? PosteCode { get; init; }
    public string? FamilleCode { get; init; }
    public required string Code { get; init; }
    public required bool EstModele { get; init; }
    public string? CodeArticleSage { get; init; }
    public string? Designation { get; init; }
    public required string Nom { get; init; }
    public required int Version { get; init; }
    public required string Statut { get; init; }
    public string? LegendeMoyens { get; init; }
    public string? Remarques { get; init; }
    public required string CreePar { get; init; }
    public required DateTime CreeLe { get; init; }
    public string? ModifiePar { get; init; }
    public DateTime? ModifieLe { get; init; }
    public string? ConfigurationColonnesJson { get; init; }
    public List<SectionAssResponseDto> Sections { get; init; } = new();
}

public record SectionAssResponseDto
{
    public required Guid Id { get; init; }
    public Guid? TypeSectionId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public required string LibelleSection { get; init; }
    public required int OrdreAffiche { get; init; }
    public string? NormeReference { get; init; }
    public int? NqaId { get; init; }
    public string? Notes { get; init; }
    public Guid? RegleEchantillonnageId { get; init; }
    public string? RegleEchantillonnageLibelle { get; init; }
    public string? FrequenceLibelle { get; init; }
    public string? ModeFreq { get; init; }
    public int? FreqNum { get; init; }
    public string? TypeVariable { get; init; }
    public int? FreqHours { get; init; }
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
    public string? LimiteSpecTexte { get; init; }
    public string? Observations { get; init; }
    public string? Instruction { get; init; } = null;
    public string? MoyenTexteLibre { get; init; } = null;
    public bool EstCritique { get; init; } = false;
    public string? ColonnesSupplementaires { get; init; }
    public string? ImageBase64 { get; init; }
}
