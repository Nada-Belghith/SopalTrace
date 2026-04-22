using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Referentiels;

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
    public string Libelle { get; init; }
    public string? UniteDefaut { get; init; }
    public bool EstNumerique { get; init; }
}
// Ajout du DTO pour les gammes
public record GammeDto(
    string NatureComposantCode,
    string OperationCode
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
    List<GammeDto> Gammes,
    List<ReferenceItemIntDto> Nqa,
    List<ReferenceItemDto> Defautheque
)
{
    public ReferentielsResponseDto()
        : this(new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new(), new()) 
    {
    }
}

public record ArticleDto(
    string CodeArticle,
    string? Designation,
    string? TypeRobinetCode,
    string? NatureComposantCode
);
