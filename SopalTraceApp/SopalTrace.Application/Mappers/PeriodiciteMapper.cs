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
            NbrPieces = dto.NbrPieces,
            Unite = dto.Unite,
            NbrHeures = dto.NbrHeures,
            Actif = dto.Actif
        };
    }
}
