#nullable disable
using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.PlansNC;

// --- REQUÊTES ---
public record CreatePlanNcRequestDto
{
    public string TypeRobinetCode { get; init; }
    public string OperationCode { get; init; }
    public string PosteCode { get; init; }
    public Guid? FormulaireId { get; init; }
    public string Nom { get; init; }
    public string CommentaireVersion { get; init; }
}

public record ColonneNcEditDto
{
    public Guid? Id { get; init; }
    public int OrdreAffiche { get; init; }
    public string MachineCode { get; init; }
    public string LibelleDefaut { get; init; }
}

public record NouvelleVersionNcRequestDto
{
    public Guid AncienId { get; init; }
    public string ModifiePar { get; init; }
    public string MotifModification { get; init; }
}

// --- RÉPONSES ---
public record PlanNcResponseDto
{
    public Guid Id { get; init; }
    public string TypeRobinetCode { get; init; }
    public string OperationCode { get; init; }
    public string PosteCode { get; init; }
    public Guid? FormulaireId { get; init; }
    public string Nom { get; init; }
    public int Version { get; init; }
    public string Statut { get; init; }
    public string CreePar { get; init; }
    public DateTime CreeLe { get; init; }
    public string ModifiePar { get; init; }
    public DateTime? ModifieLe { get; init; }
    public string CommentaireVersion { get; init; }
    public List<ColonneNcResponseDto> Colonnes { get; init; } = new();
}

public record ColonneNcResponseDto
{
    public Guid Id { get; init; }
    public int OrdreAffiche { get; init; }
    public string MachineCode { get; init; }
    public string LibelleDefaut { get; init; }
}
