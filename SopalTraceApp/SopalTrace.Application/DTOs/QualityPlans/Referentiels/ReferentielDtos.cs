using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Referentiels;


public record ReferenceItemDto(
    Guid? Id,
    string Code,
    string Libelle,
    bool Actif = true
);


public record OutilControleDetailedDto(
    Guid Id,
    string Code,
    string Libelle,
    ReferenceItemDto TypeControle,
    ReferenceItemDto TypeCaracteristique,
    ReferenceItemDto MoyenControle,
    ReferenceItemDto GroupeInstrument,
    ReferenceItemDto PeriodiciteDefaut,
    string LimiteSpecTexte,
    string Instruction,
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


public record ReferentielsResponseDto(
    List<ReferenceItemDto> TypesRobinet,
    List<ReferenceItemDto> NaturesComposant,
    List<ReferenceItemDto> Operations,
    List<ReferenceItemDto> TypesControle,
    List<ReferenceItemDto> TypesCaracteristique,
    List<ReferenceItemDto> MoyensControle,
    List<ReferenceItemDto> Periodicites,
    List<ReferenceItemDto> TypesSections,
    List<OutilControleDetailedDto> OutilsControle,
    List<GroupeInstrumentDetailedDto> GroupesInstruments
)
{
    public ReferentielsResponseDto()
        : this(new(), new(), new(), new(), new(), new(), new(), new(), new(), new())
    {
    }
}
