using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Referentiels;

public record CreatePieceReferenceDto(
    string Code,
    string TypePiece,
    string? Designation
);

public record ReferenceItemDto(
    Guid? Id,
    string Code,
    string Libelle,
    bool Actif,
    bool? EstGenerique
);

public record ReferenceItemIntDto(
    int? Id,
    string Code,
    string Libelle,
    bool Actif
);

public record InstrumentDto(
    string CodeInstrument,
    string Designation,
    bool Actif
);

public record PeriodiciteDto(
    Guid Id,
    int NbrPieces,
    string Unite,
    int? NbrHeures,
    bool Actif
)
{
    public string Libelle => SopalTrace.Application.Helpers.LibellesSection.Periodicite(NbrPieces, Unite, NbrHeures);
}

public record CreatePeriodiciteDto
{
    public int NbrPieces { get; init; }
    public required string Unite { get; init; }
    public int? NbrHeures { get; init; }
    public bool Actif { get; init; } = true;
}

public record CreateCaracteristiqueDto
{
    public string Libelle { get; init; } = string.Empty;
}
// Ajout du DTO pour les gammes
public record GammeDto(
    string NatureComposantCode,
    string OperationCode
);

// Ajout du DTO pour les familles
public record FamilleProduitDto(
    string Code,
    string Designation,
    string TypeRobinetCode,
    bool PlanParPoste
);

public record ReferentielsResponseDto(
    List<ReferenceItemDto> TypesRobinet,
    List<ReferenceItemDto> NaturesComposant,
    List<ReferenceItemDto> Operations,
    List<ReferenceItemDto> TypesControle,
    List<ReferenceItemDto> TypesCaracteristique,
    List<ReferenceItemDto> MoyensControle,
    List<PeriodiciteDto> Periodicites,
    List<ReferenceItemDto> TypesSections,
    List<InstrumentDto> Instruments,
    List<ReferenceItemDto> Postes,
    List<GammeDto> Gammes,
    List<ReferenceItemIntDto> Nqa,
    List<FamilleProduitDto> FamillesProduit
)
{
    public ReferentielsResponseDto()
        : this(new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new())
    {
    }
}

public record VerifMachineReferentielsDto(
    List<MachineVerifDto> Machines,
    List<PeriodiciteDto> Periodicites,
    List<PeriodiciteMachineDto> PeriodicitesMachine,
    List<PieceRefDto> PiecesReferences,
    List<PieceRefDto> FuitesEtalon,
    List<ReferenceItemDto> FamillesCorps,
    List<ReferenceItemDto> MoyensDetection
);

/// <summary>
/// Machine et présentation de son formulaire de vérification (lue en base, plus déduite du code machine).
/// VerifModele : BEE, MAS, SER ou null (générique).
/// </summary>
public record MachineVerifDto(
    string Code,
    string Libelle,
    bool Actif,
    string? VerifModele,
    bool SansConformite,
    bool FuiteEtalon,
    bool MoyenControleNumerote,
    bool SansDeltaP,
    bool SansPression,
    bool TablesEtalonSeparees,
    bool FuiteEnPa,
    bool ColonnesParTableau
);

/// <summary>Périodicité machine ; ControlMoment (DEMARRAGE, APRES_PAUSE, FIN_POSTE ou null) est son moment de contrôle.</summary>
public record PeriodiciteMachineDto(Guid Id, string Code, string Libelle, int OrdreAffichage, bool Actif, string? ControlMoment = null);

public record PieceRefDto(
    Guid Id,
    string Code,
    string? Designation,
    string? TypePiece  // PRC, PRNC, FEC, FENC
);

public record ArticleDto(
    string CodeArticle,
    string? Designation,
    string? TypeRobinetCode,
    string? NatureComposantCode,
    List<string>? ValidOperations = null,
    string? FamilleProduitCode = null
);
public record MachinePosteDto(
    string Code,
    string Libelle,
    string? PosteCode
);

public record ControlePosteReferentielsDto(
    List<ReferenceItemDto> Postes,
    List<MachinePosteDto> Machines,
    List<ReferenceItemDto> RisquesDefauts,
    List<FamilleProduitDto> FamillesProduit
);

public record FormulaireStructureDto(
    Guid Id,
    string CodeReference,
    string Designation,
    string? ConfigurationStructureJson,
    string Role,
    int Version
);

public record FormulaireReferenceItemDto(
    Guid Id,
    string CodeReference,
    string Designation,
    string Role,
    int Version,
    string? ConfigurationStructureJson,
    string Statut,
    // Champ d'application : poste et famille repris par les documents créés sur ce formulaire.
    string? PosteCode = null,
    string? FamilleProduitFiniCode = null
);

public record EquipeReferenceDto(string Code, string Libelle);

public record UpdateFormulaireStructureDto(
    string? ConfigurationStructureJson,
    int? VersionInitiale
);

