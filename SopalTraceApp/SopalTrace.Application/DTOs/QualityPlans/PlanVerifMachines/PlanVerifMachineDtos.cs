#nullable enable
using System;
using System.Collections.Generic;
using SopalTrace.Application.Helpers;

namespace SopalTrace.Application.DTOs.QualityPlans.PlanVerifMachines;

public class CreatePlanVerifMachineRequestDto
{
    public string Nom { get; set; } = string.Empty;
    public string MachineCode { get; set; } = string.Empty;
    public int? VersionInitiale { get; set; }
    public bool AfficheConformite { get; set; }
    public bool AfficheMoyenDetectionRisques { get; set; }
    public bool AfficheFamilles { get; set; }
    public bool AfficheFuiteEtalon { get; set; }
    public string? Remarques { get; set; }
    public string? LegendeMoyens { get; set; }
    
    public string? RefFormulaireCodeReference { get; set; }
    public List<ColonneJsonDto>? ColonneDefs { get; set; }

    public List<CreatePlanVerifMachineFamilleDto> Familles { get; set; } = new();
    public List<CreatePlanVerifMachineLigneDto> LignesConformite { get; set; } = new();
    public List<CreatePlanVerifMachineLigneDto> LignesRisques { get; set; } = new();
}

public class UpdatePlanVerifMachineRequestDto : CreatePlanVerifMachineRequestDto
{
}

public class NouvelleVersionPlanVerifMachineRequestDto : CreatePlanVerifMachineRequestDto
{
    public Guid AncienId { get; set; }
}

public class RestaurerPlanVerifMachineRequestDto
{
    public Guid AncienId { get; set; }
    public string ModifiePar { get; set; } = string.Empty;
    public string MotifModification { get; set; } = string.Empty;
}

public class CreatePlanVerifMachineFamilleDto
{
    public Guid Id { get; set; }
    public Guid RefFamilleCorpsId { get; set; }
    public int OrdreAffiche { get; set; }
}

public class CreatePlanVerifMachineLigneDto
{
    public int OrdreAffiche { get; set; }
    public string TypeLigne { get; set; } = string.Empty;
    public string LibelleRisque { get; set; } = string.Empty;
    public string? LibelleMethode { get; set; }
    
    public List<CreatePlanVerifMachineEcheanceDto> Echeances { get; set; } = new();
    public List<CreatePlanVerifMachineExtraColonneDto> ExtraColonnes { get; set; } = new();
}

public class CreatePlanVerifMachineExtraColonneDto
{
    public string CleColonne { get; set; } = string.Empty;
    public string? ValeurColonne { get; set; }
    public int OrdreAffiche { get; set; }
}

public class CreatePlanVerifMachineEcheanceDto
{
    public int OrdreAffiche { get; set; }
    public Guid PeriodiciteId { get; set; }
    public Guid? RefMoyenDetectionId { get; set; }
    
    public List<CreatePlanVerifMachineMatricePieceDto> MatricePieces { get; set; } = new();
}

public class CreatePlanVerifMachineMatricePieceDto
{
    public Guid? FamilleId { get; set; }
    public string RoleVerif { get; set; } = string.Empty;
    public Guid? PieceRefId { get; set; }
}

public class PlanVerifMachineEnteteDto
{
    public Guid Id { get; set; }
    public string MachineCode { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public int? Version { get; set; }
    public string? Statut { get; set; }
    public string CreePar { get; set; } = string.Empty;
    public DateTime? CreeLe { get; set; }
    public Guid? FormulaireId { get; set; }
    public string? Remarques { get; set; }
    public string? LegendeMoyens { get; set; }
    
    public bool AfficheConformite { get; set; }
    public bool AfficheMoyenDetectionRisques { get; set; }
    public bool AfficheFamilles { get; set; }
    public bool AfficheFuiteEtalon { get; set; }
    public string? ConfigurationColonnesJson { get; set; }

    public List<PlanVerifMachineFamilleDto> Familles { get; set; } = new();
    public List<PlanVerifMachineLigneDto> Lignes { get; set; } = new();
}

public class PlanVerifMachineFamilleDto
{
    public Guid Id { get; set; }
    public Guid RefFamilleCorpsId { get; set; }
    public int OrdreAffiche { get; set; }
}

public class PlanVerifMachineLigneDto
{
    public Guid Id { get; set; }
    public int OrdreAffiche { get; set; }
    public string TypeLigne { get; set; } = string.Empty;
    public string LibelleRisque { get; set; } = string.Empty;
    public string? LibelleMethode { get; set; }
    
    public List<PlanVerifMachineEcheanceDto> Echeances { get; set; } = new();
    public List<PlanVerifMachineExtraColonneDto> ExtraColonnes { get; set; } = new();
}

public class PlanVerifMachineExtraColonneDto
{
    public Guid Id { get; set; }
    public string CleColonne { get; set; } = string.Empty;
    public string? ValeurColonne { get; set; }
    public int OrdreAffiche { get; set; }
}

public class PlanVerifMachineEcheanceDto
{
    public Guid Id { get; set; }
    public int OrdreAffiche { get; set; }
    public Guid PeriodiciteId { get; set; }
    public Guid? RefMoyenDetectionId { get; set; }
    
    public List<PlanVerifMachineMatricePieceDto> MatricePieces { get; set; } = new();
}

public class PlanVerifMachineMatricePieceDto
{
    public Guid Id { get; set; }
    public Guid? FamilleId { get; set; }
    public string RoleVerif { get; set; } = string.Empty;
    public Guid? PieceRefId { get; set; }
}
