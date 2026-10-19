using System;
using System.Collections.Generic;
using SopalTrace.Application.Helpers;

namespace SopalTrace.Application.DTOs.QualityPlans.Fabrication;

public class CreatePlanFabricationRequestDto
{
    public string Nom { get; set; } = string.Empty;
    public string? CodeArticleSageVersionne { get; set; }
    public string? Designation { get; set; }
    public int? VersionInitiale { get; set; }
    public string? OperationCode { get; set; }
    public string? RefFormulaireCodeReference { get; set; }
    public string? LegendeMoyens { get; set; }
    public string? Remarques { get; set; }
    public string? NatureArticleCode { get; set; }
    public string? FamilleProduitFiniCode { get; set; }
    public string? PosteCode { get; set; }

    public string? ConfigurationColonnesJson { get; set; }

    public List<ColonneJsonDto> ColonneDefs { get; set; } = new();

    public List<CreatePlanFabricationSectionDto> Sections { get; set; } = new();

    public Guid? ModeleSourceId { get; set; }
    public string? Statut { get; set; }
}

public class UpdatePlanFabricationRequestDto
{
    /// <summary>Version reçue à l'ouverture du document : un document modifié entre-temps par un autre utilisateur est refusé (409).</summary>
    public string? RowVersion { get; set; }

    public string? Nom { get; set; }
    public string? CodeArticleSageVersionne { get; set; }
    public string? LegendeMoyens { get; set; }
    public string? Remarques { get; set; }
    
    public string? ConfigurationColonnesJson { get; set; }
    public string? RefFormulaireCodeReference { get; set; }
    
    public string? OperationCode { get; set; }

    public List<CreatePlanFabricationSectionDto> Sections { get; set; } = new();
}

public class CreatePlanFabricationSectionDto
{
    public Guid? Id { get; set; }
    public int OrdreAffiche { get; set; }
    public string LibelleSection { get; set; } = string.Empty;
    public Guid? TypeSectionId { get; set; }
    public Guid? PeriodiciteId { get; set; }
    public string? Notes { get; set; }
    public int? NqaId { get; set; }

    public List<CreatePlanFabricationLigneDto> Lignes { get; set; } = new();
}

public class CreatePlanFabricationLigneDto
{
    public Guid? Id { get; set; }
    public int OrdreAffiche { get; set; }
    public Guid? CaracteristiqueId { get; set; }
    public string? LibelleAffiche { get; set; }
    public Guid? TypeCaracteristiqueId { get; set; }
    public Guid? TypeControleId { get; set; }
    public Guid? MoyenControleId { get; set; }
    public string? MoyenTexteLibre { get; set; }
    public string? InstrumentCode { get; set; }
    public Guid? PeriodiciteId { get; set; }
    public string? LimiteSpecTexte { get; set; }
    public bool EstCritique { get; set; }
    public string? Instruction { get; set; }
    public string? Observations { get; set; }
    public string? ImageBase64 { get; set; }
    public string? MachineCode { get; set; }
    public Guid? RisqueDefautId { get; set; }


    public List<CreatePlanFabricationExtraColonneDto> ExtraColonnes { get; set; } = new();
}

public class CreatePlanFabricationExtraColonneDto
{
    public string CleColonne { get; set; } = string.Empty;
    public string? ValeurColonne { get; set; }
    public int OrdreAffiche { get; set; }
}

public class NouvelleVersionPlanFabricationRequestDto : CreatePlanFabricationRequestDto
{
    /// <summary>Version reçue à l'ouverture du document : un document modifié entre-temps par un autre utilisateur est refusé (409).</summary>
    public string? RowVersion { get; set; }

    public Guid AncienId { get; set; }
}

public class PlanFabricationEnteteDto
{
    /// <summary>Version du document (RowVersion en base64), à renvoyer telle quelle lors de l'enregistrement.</summary>
    public string? RowVersion { get; set; }

    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? CodeArticleSageVersionne { get; set; }
    public string? Designation { get; set; }
    public int Version { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string? OperationCode { get; set; }
    public string? OperationLibelle { get; set; }
    public Guid? FormulaireId { get; set; }
    public string? FormulaireCodeReference { get; set; }
    public int? FormulaireVersion { get; set; }
    public string? LegendeMoyens { get; set; }
    public string? Remarques { get; set; }
    public string CreePar { get; set; } = string.Empty;
    public DateTime CreeLe { get; set; }
    public string? ModifiePar { get; set; }
    public DateTime? ModifieLe { get; set; }
    public string? NatureArticleCode { get; set; }
    public string? FamilleProduitFiniCode { get; set; }
    public Guid? ModeleSourceId { get; set; }
    public string? PosteCode { get; set; }
    public string? PosteLibelle { get; set; }

    public string? ConfigurationColonnesJson { get; set; }
    
    public List<PlanFabricationSectionDto> Sections { get; set; } = new();
}

public class PlanFabricationSectionDto
{
    public Guid Id { get; set; }
    public Guid EnteteId { get; set; }
    public int OrdreAffiche { get; set; }
    public string LibelleSection { get; set; } = string.Empty;
    public Guid? TypeSectionId { get; set; }
    public Guid? PeriodiciteId { get; set; }
    public string? Notes { get; set; }
    public int? NqaId { get; set; }

    public List<PlanFabricationLigneDto> Lignes { get; set; } = new();
}

public class PlanFabricationLigneDto
{
    public Guid Id { get; set; }
    public Guid EnteteId { get; set; }
    public Guid SectionId { get; set; }
    public int OrdreAffiche { get; set; }
    public Guid? CaracteristiqueId { get; set; }
    public string? LibelleAffiche { get; set; }
    public Guid? TypeCaracteristiqueId { get; set; }
    public string? TypeCaracteristiqueLibelle { get; set; }
    public Guid? TypeControleId { get; set; }
    public string? TypeControleLibelle { get; set; }
    public Guid? MoyenControleId { get; set; }
    public string? MoyenControleLibelle { get; set; }
    public string? MoyenTexteLibre { get; set; }
    public string? InstrumentCode { get; set; }
    public Guid? PeriodiciteId { get; set; }
    public string? PeriodiciteLibelle { get; set; }
    public string? LimiteSpecTexte { get; set; }
    public bool EstCritique { get; set; }
    public string? Instruction { get; set; }
    public string? Observations { get; set; }
    public string? ImageBase64 { get; set; }
    public string? MachineCode { get; set; }
    public Guid? RisqueDefautId { get; set; }


    public List<PlanFabricationLigneExtraColonneDto> ExtraColonnes { get; set; } = new();
}

public class PlanFabricationLigneExtraColonneDto
{
    public Guid Id { get; set; }
    public Guid LigneId { get; set; }
    public string CleColonne { get; set; } = string.Empty;
    public string? ValeurColonne { get; set; }
    public int OrdreAffiche { get; set; }
}
