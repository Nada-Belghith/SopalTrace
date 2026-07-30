#nullable enable
using System;
using System.Collections.Generic;
using SopalTrace.Application.Helpers;

namespace SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;

public class CreateDocumentVerifMachineRequestDto
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

    public List<CreateDocumentVerifMachineFamilleDto> Familles { get; set; } = new();
    public List<CreateDocumentVerifMachineLigneDto> LignesConformite { get; set; } = new();
    public List<CreateDocumentVerifMachineLigneDto> LignesRisques { get; set; } = new();
}

public class UpdateDocumentVerifMachineRequestDto : CreateDocumentVerifMachineRequestDto
{
}

public class NouvelleVersionDocumentVerifMachineRequestDto : CreateDocumentVerifMachineRequestDto
{
    public Guid AncienId { get; set; }
}

public class RestaurerDocumentVerifMachineRequestDto
{
    public Guid AncienId { get; set; }
    public string ModifiePar { get; set; } = string.Empty;
    public string MotifModification { get; set; } = string.Empty;
}

public class CreateDocumentVerifMachineFamilleDto
{
    public Guid Id { get; set; }
    public Guid RefFamilleCorpsId { get; set; }
    public int OrdreAffiche { get; set; }
}

public class CreateDocumentVerifMachineLigneDto
{
    public int OrdreAffiche { get; set; }
    public string TypeLigne { get; set; } = string.Empty;
    public string LibelleRisque { get; set; } = string.Empty;
    public string? LibelleMethode { get; set; }
    
    public List<CreateDocumentVerifMachineEcheanceDto> Echeances { get; set; } = new();
    public List<CreateDocumentVerifMachineExtraColonneDto> ExtraColonnes { get; set; } = new();
}

public class CreateDocumentVerifMachineExtraColonneDto
{
    public string CleColonne { get; set; } = string.Empty;
    public string? ValeurColonne { get; set; }
    public int OrdreAffiche { get; set; }
}

public class CreateDocumentVerifMachineEcheanceDto
{
    public int OrdreAffiche { get; set; }
    public Guid PeriodiciteMachineId { get; set; }
    public Guid? RefMoyenDetectionId { get; set; }
    
    public List<CreateDocumentVerifMachineMatricePieceDto> MatricePieces { get; set; } = new();
}

public class CreateDocumentVerifMachineMatricePieceDto
{
    public Guid? FamilleId { get; set; }
    public string RoleVerif { get; set; } = string.Empty;
    public Guid? PieceRefId { get; set; }
}

public class DocumentVerifMachineEnteteDto
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

    public List<DocumentVerifMachineFamilleDto> Familles { get; set; } = new();
    public List<DocumentVerifMachineLigneDto> Lignes { get; set; } = new();
}

public class DocumentVerifMachineFamilleDto
{
    public Guid Id { get; set; }
    public Guid RefFamilleCorpsId { get; set; }
    public int OrdreAffiche { get; set; }
}

public class DocumentVerifMachineLigneDto
{
    public Guid Id { get; set; }
    public int OrdreAffiche { get; set; }
    public string TypeLigne { get; set; } = string.Empty;
    public string LibelleRisque { get; set; } = string.Empty;
    public string? LibelleMethode { get; set; }
    
    public List<DocumentVerifMachineEcheanceDto> Echeances { get; set; } = new();
    public List<DocumentVerifMachineExtraColonneDto> ExtraColonnes { get; set; } = new();
}

public class DocumentVerifMachineExtraColonneDto
{
    public Guid Id { get; set; }
    public string CleColonne { get; set; } = string.Empty;
    public string? ValeurColonne { get; set; }
    public int OrdreAffiche { get; set; }
}

public class DocumentVerifMachineEcheanceDto
{
    public Guid Id { get; set; }
    public int OrdreAffiche { get; set; }
    public Guid PeriodiciteMachineId { get; set; }
    public Guid? RefMoyenDetectionId { get; set; }
    
    public List<DocumentVerifMachineMatricePieceDto> MatricePieces { get; set; } = new();
}

public class DocumentVerifMachineMatricePieceDto
{
    public Guid Id { get; set; }
    public Guid? FamilleId { get; set; }
    public string RoleVerif { get; set; } = string.Empty;
    public Guid? PieceRefId { get; set; }
}

public class NouvelleVersionVerifMachineRequestDto
{
    public Guid AncienId { get; set; }
    public string ModifiePar { get; set; } = string.Empty;
    public string MotifModification { get; set; } = string.Empty;
    public CreateDocumentVerifMachineRequestDto Donnees { get; set; } = new();
}
