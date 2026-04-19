using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Utilities;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Mappers;

public static class CaracteristiqueMapper
{
    public static TypeCaracteristique MapToEntity(CreateCaracteristiqueDto dto)
    {
        var codeGenere = CodeGenerator.GenerateCodeFromLibelle(dto.Libelle);

        return new TypeCaracteristique
        {
            Id = Guid.NewGuid(),
            Code = codeGenere,
            Libelle = dto.Libelle,
            UniteDefaut = dto.UniteDefaut,
            EstNumerique = dto.EstNumerique,
            Actif = true
        };
    }
}
