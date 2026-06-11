using System;

namespace SopalTrace.Application.DTOs.QualityPlans.Hub;

public record HubModeleDto(
    Guid Id,
    string Category,
    string Libelle,
    string Nature,
    string Type,
    string Operation,
    string Poste,
    int Version,
    string Statut,
    string Description,
    string? CodeReferenceFormulaire = null,
    int? FormulaireVersion = null
);

public record HubPlanDto(
    Guid Id,
    string Category,
    string Libelle,
    string Nature,
    string Type,
    string Operation,
    string Poste,
    int Version,
    string Statut,
    string Description,
    string? CodeArticleSage,
    string? Designation = null,
    string? CodeReferenceFormulaire = null,
    int? FormulaireVersion = null
);