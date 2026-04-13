#nullable disable
using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.VerifMachine;

// ==========================================
// REQUÊTES (Création & Versioning)
// ==========================================
public record CreatePlanVerifMachineDto
{
    public string MachineCode { get; init; }
    public string TypeRobinetCode { get; init; } // Optionnel
    public string TypeRapport { get; init; } // BEE, MAS_ASS, MARQUAGE
    public Guid? FormulaireId { get; init; } // ISO 9001
    public string Nom { get; init; }
    public string CommentaireVersion { get; init; }
}

public record NouvelleVersionVerifMachineDto
{
    public Guid AncienId { get; init; }
    public string ModifiePar { get; init; }
    public string MotifModification { get; init; }
}

// ==========================================
// REQUÊTES (L'Arbre de Mise à Jour - 4 Niveaux)
// ==========================================
public record VerifMachineLigneEditDto
{
    public Guid? Id { get; init; }
    public int OrdreAffiche { get; init; }
    public string LibelleRisque { get; init; }
    public Guid? RisqueDefautId { get; init; }
    public string LibelleMethode { get; init; }
    public string TypeSaisie { get; init; } // CONFORMITE, MESURE, TEXTE
    public double? ValeurNominale { get; init; }
    public double? ToleranceSup { get; init; }
    public double? ToleranceInf { get; init; }
    public string Unite { get; init; }
    public string Instruction { get; init; }
    public bool EstCritique { get; init; }

    public List<VerifMachineEcheanceEditDto> Echeances { get; init; } = new();
}

public record VerifMachineEcheanceEditDto
{
    public Guid? Id { get; init; }
    public Guid PeriodiciteId { get; init; }
    public int OrdreAffiche { get; init; }
    public string LibelleMoyen { get; init; }

    public List<VerifMachinePieceRefEditDto> PiecesRef { get; init; } = new();
}

public record VerifMachinePieceRefEditDto
{
    public Guid? Id { get; init; }
    public Guid PieceRefId { get; init; }
    public string RoleVerif { get; init; } // PRC, PRNC, FEC, FENC
    public string ResultatAttendu { get; init; } // C, BLOQUE
    public string FamilleDesc { get; init; }
    public string Notes { get; init; }
}

// ==========================================
// RÉPONSES (Lecture GET)
// ==========================================
public record PlanVerifMachineResponseDto
{
    public Guid Id { get; init; }
    public string MachineCode { get; init; }
    public string TypeRobinetCode { get; init; }
    public string TypeRapport { get; init; }
    public Guid? FormulaireId { get; init; }
    public string CodeFormulaire { get; init; } // Rapatrié depuis Ref_Formulaire
    public string Nom { get; init; }
    public int Version { get; init; }
    public string Statut { get; init; }
    public string CreePar { get; init; }
    public DateTime CreeLe { get; init; }
    public string ModifiePar { get; init; }
    public DateTime? ModifieLe { get; init; }
    public string CommentaireVersion { get; init; }

    public List<VerifMachineLigneResponseDto> Lignes { get; init; } = new();
}

public record VerifMachineLigneResponseDto
{
    public Guid Id { get; init; }
    public int OrdreAffiche { get; init; }
    public string LibelleRisque { get; init; }
    public Guid? RisqueDefautId { get; init; }
    public string LibelleMethode { get; init; }
    public string TypeSaisie { get; init; }
    public double? ValeurNominale { get; init; }
    public double? ToleranceSup { get; init; }
    public double? ToleranceInf { get; init; }
    public string Unite { get; init; }
    public string Instruction { get; init; }
    public bool EstCritique { get; init; }

    public List<VerifMachineEcheanceResponseDto> Echeances { get; init; } = new();
}

public record VerifMachineEcheanceResponseDto
{
    public Guid Id { get; init; }
    public Guid PeriodiciteId { get; init; }
    public int OrdreAffiche { get; init; }
    public string LibelleMoyen { get; init; }

    public List<VerifMachinePieceRefResponseDto> PiecesRef { get; init; } = new();
}

public record VerifMachinePieceRefResponseDto
{
    public Guid Id { get; init; }
    public Guid PieceRefId { get; init; }
    public string RoleVerif { get; init; }
    public string ResultatAttendu { get; init; }
    public string FamilleDesc { get; init; }
    public string Notes { get; init; }
}
