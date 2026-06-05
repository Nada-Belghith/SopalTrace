#nullable disable
using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.PlansNC;

// --- REQUÊTES ---
public record CreatePlanNcRequestDto
{
    public string PosteCode { get; init; }
    public string Nom { get; init; }
    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }
    public string ConfigurationColonnesJson { get; init; }
    public Guid? FormulaireId { get; init; }
    /// <summary>
    /// CodeReference du formulaire sélectionné (ex: FE-RC-PAS71_SOUPAPE).
    /// Passé directement du frontend pour éviter un GetFormulaireById qui causerait
    /// un conflit de concurrence EF Core avec UpdateFormulaireStructureAsync.
    /// </summary>
    public string FormulaireCodeReference { get; init; }
    public int? VersionInitiale { get; init; }

    public List<LigneNcEditDto> Lignes { get; init; } = new();
    public string CommentaireVersion { get; init; }
}

public record SavePlanNcDto
{
    public string PosteCode { get; init; }
    public string Nom { get; init; }
    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }
    public string ConfigurationColonnesJson { get; init; }
    public Guid? FormulaireId { get; init; }




    
    public List<LigneNcEditDto> Lignes { get; init; } = new();
}

public record LigneNcEditDto
{
    public Guid? Id { get; init; }
    public int OrdreAffiche { get; init; }
    public string MachineCode { get; init; }
    public Guid? RisqueDefautId { get; init; }
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
    public string PosteCode { get; init; }
    public string Nom { get; init; }
    public int Version { get; init; }
    public string Statut { get; init; }
    public string CreePar { get; init; }
    public DateTime CreeLe { get; init; }
    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }
    public string ConfigurationColonnesJson { get; init; }
    public Guid? FormulaireId { get; init; }




    
    public List<LigneNcResponseDto> Lignes { get; init; } = new();
}

public record LigneNcResponseDto
{
    public Guid Id { get; init; }
    public int OrdreAffiche { get; init; }
    public string MachineCode { get; init; }
    public Guid RisqueDefautId { get; init; }
    public string LibelleDefaut { get; init; }
}
