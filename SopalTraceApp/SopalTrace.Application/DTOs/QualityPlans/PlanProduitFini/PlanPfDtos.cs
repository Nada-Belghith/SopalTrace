#nullable disable

using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.PlanProduitFini;

// ====================================================================
// DTOs DE CRÉATION ET GESTION DE PLAN PF
// ====================================================================

public record CreatePlanPfRequestDto
{
    public string TypeRobinetCode { get; init; }
    public string Designation { get; init; }
    public string CommentaireVersion { get; init; }
    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }
    public List<SectionPfEditDto> Sections { get; init; } = new();
}

public record NouvelleVersionPfRequestDto
{
    public Guid AncienId { get; init; }
    public string TypeRobinetCode { get; init; }
    public string Designation { get; init; }
    public string ModifiePar { get; init; }
    public string MotifModification { get; init; }
    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }
    public List<SectionPfEditDto> Sections { get; init; } = new();
}

public record RestaurerPfRequestDto
{
    public Guid PlanArchiveId { get; init; }
    public string RestaurePar { get; init; }
    public string MotifRestoration { get; init; }
}

public record UpdatePlanPfRequestDto
{
    public List<SectionPfEditDto> Sections { get; init; } = new();
    public string CommentaireVersion { get; init; }
    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }
}

// ====================================================================
// DTOs DE LA LIBERTÉ TOTALE (ÉDITION)
// ====================================================================

public record SectionPfEditDto
{
    public Guid? Id { get; set; }
    public Guid? TypeSectionId { get; init; }
    public string LibelleSection { get; init; }
    public string Notes { get; init; }
    public int OrdreAffiche { get; init; }
    public List<LignePfEditDto> Lignes { get; init; } = new();
}

public record LignePfEditDto
{
    public Guid? Id { get; set; }
    public int OrdreAffiche { get; init; }
    public Guid? TypeCaracteristiqueId { get; init; }
    public string LibelleAffiche { get; init; }
    public Guid? TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public string InstrumentCode { get; init; }
    public string MoyenTexteLibre { get; init; }
    public double? ValeurNominale { get; init; } // Optionnel pour la logique métier
    public double? ToleranceSuperieure { get; init; }
    public double? ToleranceInferieure { get; init; }
    public string LimiteSpecTexte { get; init; }
    public Guid? DefauthequeId { get; init; }
    public string Instruction { get; init; }
    public string Observations { get; init; }
}

// ====================================================================
// DTOs DE LECTURE (GET)
// ====================================================================

public record PlanPfEnteteDto
{
    public Guid Id { get; set; }
    public string TypeRobinetCode { get; set; }
    public string Designation { get; set; }
    public int Version { get; set; }
    public string Statut { get; set; }
    public DateOnly? DateApplication { get; set; }
    public string CreePar { get; set; }
    public DateTime CreeLe { get; set; }
    public string ModifiePar { get; set; }
    public DateTime? ModifieLe { get; set; }
    public string CommentaireVersion { get; set; }
    public string Remarques { get; set; }
    public string LegendeMoyens { get; set; }

    public List<PlanPfSectionDto> Sections { get; set; } = new();
}

public record PlanPfSectionDto
{
    public Guid Id { get; set; }
    public Guid PlanEnteteId { get; set; }
    public Guid? TypeSectionId { get; set; }
    public string TypeSectionLibelle { get; set; } 
    public string LibelleSection { get; set; }
    public string Notes { get; set; }
    public int OrdreAffiche { get; set; }

    public List<PlanPfLigneDto> Lignes { get; set; } = new();
}

public record PlanPfLigneDto
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public int OrdreAffiche { get; set; }
    public Guid? TypeCaracteristiqueId { get; set; }
    public string TypeCaracteristiqueLibelle { get; set; }
    public string LibelleAffiche { get; set; }
    public Guid? TypeControleId { get; set; }
    public string TypeControleLibelle { get; set; }
    public Guid? MoyenControleId { get; set; }
    public string MoyenControleLibelle { get; set; }
    public string InstrumentCode { get; set; }
    public string MoyenTexteLibre { get; set; }
    public double? ToleranceSuperieure { get; set; }
    public double? ToleranceInferieure { get; set; }
    public string LimiteSpecTexte { get; set; }
    public Guid? DefauthequeId { get; set; }
    public string DefauthequeLibelle { get; set; }
    public string Instruction { get; set; }
    public string Observations { get; set; }
}

