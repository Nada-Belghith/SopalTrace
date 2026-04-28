#nullable disable

using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;

// ====================================================================
// DTOs DE CRÉATION ET GESTION DE PLAN
// ====================================================================

public record CreatePlanRequestDto
{
    public Guid? ModeleSourceId { get; init; } // Ajoutez le ? ici
    public string CodeArticleSage { get; init; }
    public string Designation { get; init; }
    public string Nom { get; init; }
    public string OperationCode { get; init; } // << AJOUTÉ
    public string MachineDefautCode { get; set; }
    public string CommentaireVersion { get; init; }
    public string LegendeMoyens { get; init; } 
    public string Remarques { get; init; }
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
    public string LegendeMoyens { get; init; } // <-- Ajout
    public string Remarques { get; init; }
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

    public Guid? ModeleSectionId { get; init; }

    public Guid? TypeSectionId { get; init; }
    public string LibelleSection { get; init; }
    public string FrequenceLibelle { get; init; } 
    public Guid? PeriodiciteId { get; init; }
   
    public int OrdreAffiche { get; init; }
    public List<LigneEditDto> Lignes { get; init; } = new();
}

public record LigneEditDto
{
    public Guid? Id { get; set; }

    public Guid? ModeleLigneSourceId { get; init; }

    public int OrdreAffiche { get; init; }
    public Guid? TypeCaracteristiqueId { get; init; }
    public string LibelleAffiche { get; init; }
    public Guid? TypeControleId { get; init; }
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
    public string OperationCode { get; init; }
    public string CodeArticleSage { get; init; }
    public string Nom { get; init; }
    public string Designation { get; init; }
    public int Version { get; init; }
    public string Statut { get; init; }
    public DateTime? DateApplication { get; init; }
    public string MachineDefautCode { get; init; }
    public string LegendeMoyens { get; init; } // <-- Ajout
    public string Remarques { get; init; }
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
    public Guid? TypeCaracteristiqueId { get; init; }
    public string LibelleAffiche { get; init; }
    public string NomCategorie { get; init; }
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

public record RestaurerPlanRequestDto(
    Guid PlanArchiveId, 
    string RestaurePar, 
    string MotifRestoration
);
public record UpdateValeursPlanRequestDto
{
    public List<SectionEditDto> Sections { get; init; } = new();
    public string LegendeMoyens { get; init; }
    public string Remarques { get; init; }
    public string Nom { get; init; }
    public string ModifiePar { get; init; }
    public bool Finaliser { get; init; } = true;
}

