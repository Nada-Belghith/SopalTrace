using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Assemblage;

// --- CRÉATION ---
public record CreatePlanAssRequestDto
{
    public string OperationCode { get; init; }
    public string TypeRobinetCode { get; init; }
    public bool EstModele { get; init; }
    public string? CodeArticleSage { get; init; }
    public string Nom { get; init; }
    public string? CommentaireVersion { get; init; }
}

// --- ÉDITION (ARBRE COMPLET) ---
public record SectionAssEditDto
{
    public Guid? Id { get; set; }
    public int OrdreAffiche { get; init; }
    public Guid TypeSectionId { get; init; }
    public Guid? PeriodiciteId { get; init; } // V5.2 : BIEN SUR LA SECTION
    public string LibelleSection { get; init; }
    public string? NormeReference { get; init; }
    public int? NqaId { get; init; }
    public string? Notes { get; init; }
    public List<LigneAssEditDto> Lignes { get; init; } = new();
}

public record LigneAssEditDto
{
    public Guid? Id { get; set; }
    public int OrdreAffiche { get; init; }
    public Guid TypeCaracteristiqueId { get; init; }
    public string LibelleAffiche { get; init; } // NOT NULL
    public Guid TypeControleId { get; init; }
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
    public bool EstCritique { get; init; }
}

// --- ACTIONS MÉTIER ---
public record ChangePlanAssStatusRequestDto
{
    public string NouveauStatut { get; init; }
    public string? Motif { get; init; }
}

public record CloneExceptionAssRequestDto
{
    public Guid PlanMaitreId { get; init; }
    public string NouveauCodeArticleSage { get; init; }
    public string CreePar { get; init; }
}

public record NouvelleVersionAssRequestDto
{
    public Guid AncienId { get; init; }
    public string ModifiePar { get; init; }
    public string MotifModification { get; init; }
}

// --- LECTURE ---
public record PlanAssResponseDto
{
    public Guid Id { get; init; }
    public string OperationCode { get; init; }
    public string TypeRobinetCode { get; init; }
    public bool EstModele { get; init; }
    public string? CodeArticleSage { get; init; }
    public string? Designation { get; init; }
    public string Nom { get; init; }
    public int Version { get; init; }
    public string Statut { get; init; }
    public DateTime? DateApplication { get; init; }
    public string CreePar { get; init; }
    public DateTime CreeLe { get; init; }
    public string? ModifiePar { get; init; }
    public DateTime? ModifieLe { get; init; }
    public string? CommentaireVersion { get; init; }
    public List<SectionAssResponseDto> Sections { get; init; } = new();
}

public record SectionAssResponseDto
{
    public Guid Id { get; init; }
    public Guid TypeSectionId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public string LibelleSection { get; init; }
    public int OrdreAffiche { get; init; }
    public string? NormeReference { get; init; }
    public int? NqaId { get; init; }
    public string? Notes { get; init; }
    public List<LigneAssResponseDto> Lignes { get; init; } = new();
}

public record LigneAssResponseDto
{
    public Guid Id { get; init; }
    public int OrdreAffiche { get; init; }
    public Guid TypeCaracteristiqueId { get; init; }
    public string LibelleAffiche { get; init; }
    public Guid TypeControleId { get; init; }
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
    public bool EstCritique { get; init; }
}