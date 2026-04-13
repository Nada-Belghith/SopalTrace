#nullable disable

using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;

// ====================================================================
// DTOs DE CRÉATION ET GESTION DE PLAN
// ====================================================================

public record CreatePlanRequestDto
{
    public Guid ModeleSourceId { get; init; }
    public string CodeArticleSage { get; init; }
    public string Designation { get; init; }
    public string Nom { get; init; }
    public string MachineDefautCode { get; set; }
    public string CommentaireVersion { get; init; }
}

public record ChangePlanStatusRequestDto
{
    public string NouveauStatut { get; init; }
    public string Motif { get; init; }
}

public record ClonePlanRequestDto
{
    public Guid PlanExistantId { get; init; }
    public string NouveauCodeArticleSage { get; init; }
    public string NouvelleDesignation { get; init; }
    public string CreePar { get; init; }
}

public record NouvelleVersionRequestDto
{
    public Guid AncienId { get; init; }
    public string ModifiePar { get; init; }
    public string MotifModification { get; init; }
    public List<SectionEditDto> SectionsModifiees { get; init; } = new();
}

public record UpdatePlanLineRequestDto
{
    public Guid LigneId { get; set; }
    public double? ValeurNominale { get; set; }
    public double? ToleranceSuperieure { get; set; }
    public double? ToleranceInferieure { get; set; }
    public string Unite { get; set; }
    public string LimiteSpecTexte { get; set; }
    public string Observations { get; set; }
    public string Instruction { get; set; }
    public string InstrumentCode { get; set; }
    public Guid? PeriodiciteId { get; set; }
}

// ====================================================================
// DTOs DE LA LIBERTÉ TOTALE (ÉDITION)
// ====================================================================

public record SectionEditDto
{
    public Guid? Id { get; set; }

    // AJOUT ICI : Permet au mapper de faire le lien avec le modèle source
    public Guid? ModeleSectionId { get; init; }

    public Guid TypeSectionId { get; init; }
    public string LibelleSection { get; init; }
    public int OrdreAffiche { get; init; }
    public List<LigneEditDto> Lignes { get; init; } = new();
}

public record LigneEditDto
{
    public Guid? Id { get; set; }

    // AJOUT ICI : Permet au mapper de faire le lien avec la ligne source
    public Guid? ModeleLigneSourceId { get; init; }

    public int OrdreAffiche { get; init; }
    public Guid TypeCaracteristiqueId { get; init; }
    public string LibelleAffiche { get; init; }
    public Guid TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public Guid? GroupeInstrumentId { get; init; }
    public string InstrumentCode { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public double? ValeurNominale { get; init; }
    public double? ToleranceSuperieure { get; init; }
    public double? ToleranceInferieure { get; init; }
    public string Unite { get; init; }
    public string LimiteSpecTexte { get; init; }
    public string Observations { get; init; }
    public string Instruction { get; init; }
    public bool EstCritique { get; init; } = false;
}

// ====================================================================
// DTOs DE RÉPONSE (AFFICHAGE)
// ====================================================================

public record PlanResponseDto
{
    public Guid Id { get; init; }
    public Guid ModeleSourceId { get; init; }
    public string CodeArticleSage { get; init; }
    public string Nom { get; init; }
    public string Designation { get; init; }
    public int Version { get; init; }
    public string Statut { get; init; }
    public DateTime? DateApplication { get; init; }
    public string MachineDefautCode { get; init; }
    public string CreePar { get; init; }
    public DateTime CreeLe { get; init; }
    public string ModifiePar { get; init; }
    public DateTime? ModifieLe { get; init; }
    public string CommentaireVersion { get; init; }
    public List<PlanSectionResponseDto> Sections { get; init; } = new();
}

public record PlanSectionResponseDto
{
    public Guid Id { get; init; }
    public Guid? ModeleSectionId { get; init; }
    public int OrdreAffiche { get; init; }
    public string LibelleSection { get; init; }
    public string FrequenceLibelle { get; init; }
    public List<PlanLigneResponseDto> Lignes { get; init; } = new();
}

public record PlanLigneResponseDto
{
    public Guid Id { get; init; }
    public Guid? ModeleLigneSourceId { get; init; }
    public int OrdreAffiche { get; init; }
    public Guid TypeCaracteristiqueId { get; init; }
    public string LibelleAffiche { get; init; }
    public Guid TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public Guid? GroupeInstrumentId { get; init; }
    public string InstrumentCode { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public double? ValeurNominale { get; set; }
    public double? ToleranceSuperieure { get; set; }
    public double? ToleranceInferieure { get; set; }
    public string Unite { get; set; }
    public string LimiteSpecTexte { get; set; }
    public string Observations { get; set; }
    public string Instruction { get; set; }
    public bool EstCritique { get; init; }
}