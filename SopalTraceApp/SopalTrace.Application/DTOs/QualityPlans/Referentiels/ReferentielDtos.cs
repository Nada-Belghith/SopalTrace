using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Referentiels;

public record CreatePieceReferenceDto(
    string Code,
    string TypePiece,
    string? Designation,
    string? FamilleDesc
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
    string Code,
    string Libelle,
    int? FrequenceNum,
    string? FrequenceUnite,
    int OrdreAffichage,
    bool Actif
);

public record CreatePeriodiciteDto
{
    public required string Code { get; init; }
    public required string Libelle { get; init; }
    public int? FrequenceNum { get; init; }
    public string? FrequenceUnite { get; init; }
    public int OrdreAffichage { get; init; }
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
    string TypeRobinetCode
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
    List<ReferenceItemDto> Defautheque,
    List<ReferenceItemDto> ReglesEchantillonnage,
    List<FamilleProduitDto> FamillesProduit
)
{
    public ReferentielsResponseDto()
        : this(new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new()) 
    {
    }
}

public record VerifMachineReferentielsDto(
    List<ReferenceItemDto> Machines,
    List<PeriodiciteDto> Periodicites,
    List<PieceRefDto> PiecesReferences,
    List<PieceRefDto> FuitesEtalon,
    List<ReferenceItemDto> FamillesCorps,
    List<ReferenceItemDto> MoyensDetection
);

public record PieceRefDto(
    Guid Id,
    string Code,
    string? Designation,
    string? FamilleDesc,
    string? TypePiece  // PRC, PRNC, FEC, FENC
);

public record ArticleDto(
    string CodeArticle,
    string? Designation,
    string? TypeRobinetCode,
    string? NatureComposantCode,
    List<string>? ValidOperations = null
);
public record MachinePosteDto(
    string Code,
    string Libelle,
    string? PosteCode
);

public record PlanNcReferentielsDto(
    List<ReferenceItemDto> Postes,
    List<MachinePosteDto> Machines,
    List<ReferenceItemDto> RisquesDefauts
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
    string? ConfigurationStructureJson
);

public record UpdateFormulaireStructureDto(
    string? ConfigurationStructureJson
);

