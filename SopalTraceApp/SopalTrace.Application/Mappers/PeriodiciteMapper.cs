using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Mappers;

public static class PeriodiciteMapper
{
    public static Periodicite MapToEntity(CreatePeriodiciteDto dto)
    {
        return new Periodicite
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Libelle = dto.Libelle,
            FrequenceNum = dto.FrequenceNum,
            FrequenceUnite = dto.FrequenceUnite,
            OrdreAffichage = dto.OrdreAffichage,
            Actif = dto.Actif
        };
    }
}
