#nullable enable

using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.PlanProduitFini;

// ====================================================================
// DTOs DE CRÉATION ET GESTION DE PLAN PF
// ====================================================================

public record CreatePlanPfRequestDto
{
    public string? FamilleProduitFiniCode { get; init; }
    public string? Remarques { get; init; }
    public string? LegendeMoyens { get; init; }
    public List<SectionPfEditDto> Sections { get; init; } = new();
}

public record NouvelleVersionPfRequestDto
{
    public Guid AncienId { get; init; }
    public string? FamilleProduitFiniCode { get; init; }
    public string? ModifiePar { get; init; }
    public string? MotifModification { get; init; }
    public string? Remarques { get; init; }
    public string? LegendeMoyens { get; init; }
    public List<SectionPfEditDto> Sections { get; init; } = new();
}

public record RestaurerPfRequestDto
{
    public Guid PlanArchiveId { get; init; }
    public string RestaurePar { get; init; } = string.Empty;
    public string MotifRestoration { get; init; } = string.Empty;
}

public record UpdatePlanPfRequestDto
{
    public List<SectionPfEditDto> Sections { get; init; } = new();
    public string Remarques { get; init; } = string.Empty;
    public string LegendeMoyens { get; init; } = string.Empty;
}

// ====================================================================
// DTOs DE LA LIBERTÉ TOTALE (ÉDITION)
// ====================================================================

public record SectionPfEditDto
{
    public Guid? Id { get; set; }
    public Guid? TypeSectionId { get; init; }
    public string? LibelleSection { get; init; }
    public Guid? RegleEchantillonnageId { get; init; }
    public string? RegleEchantillonnageLibelle { get; init; }
    public string? Notes { get; init; }
    public int OrdreAffiche { get; init; }
    public List<LignePfEditDto> Lignes { get; init; } = new();
}

public record LignePfEditDto
{
    public Guid? Id { get; set; }
    public int OrdreAffiche { get; init; }
    public Guid? TypeCaracteristiqueId { get; init; }
    public string? LibelleAffiche { get; init; }
    public Guid? TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public string? InstrumentCode { get; init; }
    public string? MoyenTexteLibre { get; init; }
    public string? LimiteSpecTexte { get; init; }
    public Guid? DefauthequeId { get; init; }
    public string? Instruction { get; init; }
    public string? Observations { get; init; }
    public bool EstCritique { get; init; }
}

// ====================================================================
// DTOs DE LECTURE (GET)
// ====================================================================

public record PlanPfEnteteDto
{
    public Guid Id { get; set; }
    public string? FamilleProduitFiniCode { get; set; }
    public string? FamilleProduitFiniLibelle { get; set; }
    public int Version { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string CreePar { get; set; } = string.Empty;
    public DateTime CreeLe { get; set; }
    public string ModifiePar { get; set; } = string.Empty;
    public DateTime? ModifieLe { get; set; }
    public string Remarques { get; set; } = string.Empty;
    public string LegendeMoyens { get; set; } = string.Empty;

    public List<PlanPfSectionDto> Sections { get; set; } = new();
}

public record PlanPfSectionDto
{
    public Guid Id { get; set; }
    public Guid PlanEnteteId { get; set; }
    public Guid? TypeSectionId { get; set; }
    public string? TypeSectionLibelle { get; set; } 
    public string? LibelleSection { get; set; }
    public Guid? RegleEchantillonnageId { get; set; }
    public string? RegleEchantillonnageLibelle { get; set; }
    public string? Notes { get; set; }
    public int OrdreAffiche { get; set; }

    public List<PlanPfLigneDto> Lignes { get; set; } = new();
}

public record PlanPfLigneDto
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public int OrdreAffiche { get; set; }
    public Guid? TypeCaracteristiqueId { get; set; }
    public string TypeCaracteristiqueLibelle { get; set; } = string.Empty;
    public string LibelleAffiche { get; set; } = string.Empty;
    public Guid? TypeControleId { get; set; }
    public string TypeControleLibelle { get; set; } = string.Empty;
    public Guid? MoyenControleId { get; set; }
    public string MoyenControleLibelle { get; set; } = string.Empty;
    public string InstrumentCode { get; set; } = string.Empty;
    public string MoyenTexteLibre { get; set; } = string.Empty;
    public string LimiteSpecTexte { get; set; } = string.Empty;
    public Guid? DefauthequeId { get; set; }
    public string DefauthequeLibelle { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
    public string Observations { get; set; } = string.Empty;
    public bool EstCritique { get; set; }
}
