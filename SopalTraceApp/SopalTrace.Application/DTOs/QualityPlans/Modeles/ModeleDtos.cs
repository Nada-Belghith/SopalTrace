#nullable disable
using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Modeles;

// ==========================================================
// REQUÊTES (Création / Modification)
// ==========================================================

public record CreateModeleRequestDto
{
    public string Code { get; init; }
    public string Libelle { get; init; }
    public string TypeRobinetCode { get; init; }
    public string NatureComposantCode { get; init; }
    public string OperationCode { get; init; }
    public string Notes { get; init; }
    public List<ModeleCreateSectionDto> Sections { get; init; } = new();
}

public record ModeleCreateSectionDto
{
    public int OrdreAffiche { get; init; }
    public string LibelleSection { get; init; }
    public string FrequenceLibelle { get; init; }
    public List<ModeleCreateLigneDto> Lignes { get; init; } = new();
}

public record ModeleCreateLigneDto
{
    public int OrdreAffiche { get; init; }
    public Guid? OutilSourceId { get; init; }
    public Guid TypeCaracteristiqueId { get; init; }
    public string LibelleAffiche { get; init; }
    public Guid TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public Guid? GroupeInstrumentId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public string Instruction { get; init; }
    public bool EstCritique { get; init; } = false;
}

public record UpdateModeleRequestDto
{
    public string Libelle { get; init; }
    public string Notes { get; init; }
    public string Statut { get; init; }
}

// ==========================================================
// RÉPONSES (Affichage)
// ==========================================================

public record ModeleResponseDto
{
    public Guid Id { get; init; }
    public string Code { get; init; }
    public string Libelle { get; init; }
    public string TypeRobinetCode { get; init; }
    public string NatureComposantCode { get; init; }
    public string OperationCode { get; init; }
    public int Version { get; init; }
    public string Statut { get; init; }
    public string Notes { get; init; }
    public string CreePar { get; init; }
    public DateTime CreeLe { get; init; }
    public DateTime? ArchiveLe { get; init; }
    public string ArchivePar { get; init; }
    public List<ModeleSectionResponseDto> Sections { get; init; } = new();
}

public record ModeleSectionResponseDto
{
    public Guid Id { get; init; }
    public int OrdreAffiche { get; init; }
    public string LibelleSection { get; init; }
    public string FrequenceLibelle { get; init; }
    public List<ModeleLigneResponseDto> Lignes { get; init; } = new();
}

public record ModeleLigneResponseDto
{
    public Guid Id { get; init; }
    public int OrdreAffiche { get; init; }
    public Guid? OutilSourceId { get; init; }
    public Guid TypeCaracteristiqueId { get; init; }
    public string LibelleAffiche { get; init; }
    public Guid TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public Guid? GroupeInstrumentId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public string Instruction { get; init; }
    public bool EstCritique { get; init; }
}