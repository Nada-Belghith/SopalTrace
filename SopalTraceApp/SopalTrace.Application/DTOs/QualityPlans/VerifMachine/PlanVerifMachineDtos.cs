#nullable disable
using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.VerifMachine;

// ==========================================
// DTOs UNIFIÉS POUR LE FRONTEND (CRÉATION)
// ==========================================

public record CreateVerifMachineModeleDto
{
    public string Nom { get; init; }
    public string MachineCode { get; init; }

    // Flags UI
    public bool AfficheConformite { get; init; }
    public bool AfficheMoyenDetectionRisques { get; init; }
    public bool AfficheFamilles { get; init; }
    public bool AfficheFuiteEtalon { get; init; }

    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }

    public List<VmFamilleDto> Familles { get; init; } = new();
    public List<VmLigneDto> LignesConformite { get; init; } = new();
    public List<VmLigneDto> LignesRisques { get; init; } = new();
}

public record VmFamilleDto
{
    public Guid Id { get; init; }
    public Guid RefFamilleCorpsId { get; init; }
    public int OrdreAffiche { get; init; }
}

public record VmLigneDto
{
    public int OrdreAffiche { get; init; }
    public string LibelleRisque { get; init; }
    public string LibelleMethode { get; init; }
    public List<VmEcheanceDto> Echeances { get; init; } = new();
}

public record VmEcheanceDto
{
    public int OrdreAffiche { get; init; }
    public Guid PeriodiciteId { get; init; }
    public Guid? RefMoyenDetectionId { get; init; }
    public List<VmPieceRefDto> MatricePieces { get; init; } = new();
}

public record VmPieceRefDto
{
    public Guid? FamilleId { get; init; }
    public string RoleVerif { get; init; }
    public Guid? PieceRefId { get; init; }
}

// ==========================================
// RÉPONSES (LECTURE DEPUS L'API)
// ==========================================

public record PlanVerifMachineResponseDto
{
    public Guid Id { get; init; }
    public string MachineCode { get; init; }
    public string Nom { get; init; }
    public int Version { get; init; }
    public string Statut { get; init; }
    
    // Flags UI
    public bool AfficheConformite { get; init; }
    public bool AfficheMoyenDetectionRisques { get; init; }
    public bool AfficheFamilles { get; init; }
    public bool AfficheFuiteEtalon { get; init; }

    public string Remarques { get; init; }
    public string LegendeMoyens { get; init; }

    public string CreePar { get; init; }
    public DateTime CreeLe { get; init; }
    public string ModifiePar { get; init; }
    public DateTime? ModifieLe { get; init; }

    public List<VmFamilleDto> Familles { get; init; } = new();
    public List<VmLigneResponseDto> LignesConformite { get; init; } = new();
    public List<VmLigneResponseDto> LignesRisques { get; init; } = new();
}

public record VmLigneResponseDto
{
    public Guid Id { get; init; }
    public int OrdreAffiche { get; init; }
    public string TypeLigne { get; init; } // "CONFORMITE" ou "RISQUE"
    public string LibelleRisque { get; init; }
    public string LibelleMethode { get; init; }
    public List<VmEcheanceResponseDto> Echeances { get; init; } = new();
}

public record VmEcheanceResponseDto
{
    public Guid Id { get; init; }
    public Guid PeriodiciteId { get; init; }
    public int OrdreAffiche { get; init; }
    public Guid? RefMoyenDetectionId { get; init; } // ✅ 100% Dictionnaire
    public List<VmPieceRefResponseDto> PiecesRef { get; init; } = new();
}

public record VmPieceRefResponseDto
{
    public Guid Id { get; init; }
    public Guid? FamilleId { get; init; }
    public string RoleVerif { get; init; }
    public Guid? PieceRefId { get; init; } // ✅ 100% Dictionnaire
}

// ==========================================
// VERSIONING & ÉDITION
// ==========================================
public record NouvelleVersionVerifMachineDto
{
    public Guid AncienId { get; init; }
    public string ModifiePar { get; init; }
    public string MotifModification { get; init; }
}

public record CreatePlanVerifMachineDto
{
    public string MachineCode { get; init; }
    public Guid? FormulaireId { get; init; }
    public string Nom { get; init; }
    public string CommentaireVersion { get; init; }
}

public record VerifMachineLigneEditDto
{
    public Guid? Id { get; init; }
    public int OrdreAffiche { get; init; }
    public string TypeLigne { get; init; }
    public string LibelleRisque { get; init; }
    public string LibelleMethode { get; init; }
    public List<VerifMachineEcheanceEditDto> Echeances { get; init; } = new();
}

public record VerifMachineEcheanceEditDto
{
    public Guid? Id { get; init; }
    public int OrdreAffiche { get; init; }
    public Guid PeriodiciteId { get; init; }
    public Guid? RefMoyenDetectionId { get; init; }
    public List<VerifMachinePieceRefEditDto> PiecesRef { get; init; } = new();
}

public record VerifMachinePieceRefEditDto
{
    public Guid? Id { get; init; }
    public Guid? FamilleId { get; init; }
    public string RoleVerif { get; init; }
    public Guid? PieceRefId { get; init; }
}