using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Referentiels;


public record ReferenceItemDto(
    Guid? Id,
    string Code,
    string Libelle,
    bool Actif = true
);


public record GroupeInstrumentDetailedDto(
    Guid Id,
    string CodeAlias,
    string Libelle,
    List<ReferenceItemDto> Instruments,
    bool Actif = true
)
{
    public GroupeInstrumentDetailedDto(Guid Id, string CodeAlias, string Libelle, bool Actif = true)
        : this(Id, CodeAlias, Libelle, new(), Actif)
    {
    }
}

public record InstrumentDto(
    string CodeInstrument,
    string Designation,
    bool Actif = true
);

public record PeriodiciteDto(
    Guid Id,
    string Code,
    string Libelle,
    int? FrequenceNum,
    string? FrequenceUnite,
    int OrdreAffichage,
    bool Actif = true
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


public record ReferentielsResponseDto(
    List<ReferenceItemDto> TypesRobinet,
    List<ReferenceItemDto> NaturesComposant,
    List<ReferenceItemDto> Operations,
    List<ReferenceItemDto> TypesControle,
    List<ReferenceItemDto> TypesCaracteristique,
    List<ReferenceItemDto> MoyensControle,
    List<PeriodiciteDto> Periodicites,
    List<ReferenceItemDto> TypesSections,
    List<GroupeInstrumentDetailedDto> GroupesInstruments,
    List<InstrumentDto> Instruments
)
{
    public ReferentielsResponseDto()
        : this(new(), new(), new(), new(), new(), new(), new(), new(), new(), new())
    {
    }
}
