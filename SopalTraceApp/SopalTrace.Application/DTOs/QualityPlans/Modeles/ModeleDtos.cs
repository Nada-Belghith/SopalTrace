#nullable disable
using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Modeles;

// --- CRÉATION ---
public record CreateModeleRequestDto
{
    public required string Code { get; init; }
    public required string Libelle { get; init; }
    public required string TypeRobinetCode { get; init; }
    public required string NatureComposantCode { get; init; }
    public required string OperationCode { get; init; }
    public string? Notes { get; init; }
    public List<SectionModeleEditDto> Sections { get; init; } = new();
}

// --- ÉDITION ---
public record SectionModeleEditDto
{
    public Guid? Id { get; set; }
    public required int OrdreAffiche { get; init; }
    public required string LibelleSection { get; init; }
    public string? FrequenceLibelle { get; init; }
    public List<LigneModeleEditDto> Lignes { get; init; } = new();
}

public record LigneModeleEditDto
{
    public Guid? Id { get; set; }
    public required int OrdreAffiche { get; init; }
    public required Guid TypeCaracteristiqueId { get; init; }
    public string? LibelleAffiche { get; init; }
    public required Guid TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public Guid? GroupeInstrumentId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public string? Instruction { get; init; }
    public required bool EstCritique { get; init; }
}

// --- ACTIONS MÉTIER ---
public record ChangeModeleStatusRequestDto
{
    public required string NouveauStatut { get; init; }
    public string? Motif { get; init; }
}

public record NouvelleVersionModeleRequestDto
{
    public required Guid AncienId { get; init; }
    public required string CreePar { get; init; }
    public string? MotifModification { get; init; }
}

// --- RÉPONSES ---
public record ModeleResponseDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Libelle { get; init; }
    public required string TypeRobinetCode { get; init; }
    public required string NatureComposantCode { get; init; }
    public required string OperationCode { get; init; }
    public required int Version { get; init; }
    public required string Statut { get; init; }
    public string? Notes { get; init; }
    public required string CreePar { get; init; }
    public required DateTime CreeLe { get; init; }
    public string? ArchivePar { get; init; }
    public DateTime? ArchiveLe { get; init; }
    public List<ModeleSectionResponseDto> Sections { get; init; } = new();
}

public record ModeleSectionResponseDto
{
    public required Guid Id { get; init; }
    public required int OrdreAffiche { get; init; }
    public required string LibelleSection { get; init; }
    public string? FrequenceLibelle { get; init; }
    public List<ModeleLigneResponseDto> Lignes { get; init; } = new();
}

public record ModeleLigneResponseDto
{
    public required Guid Id { get; init; }
    public required int OrdreAffiche { get; init; }
    public required Guid TypeCaracteristiqueId { get; init; }
    public string? LibelleAffiche { get; init; }
    public required Guid TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public Guid? GroupeInstrumentId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public string? Instruction { get; init; }
    public required bool EstCritique { get; init; }
}