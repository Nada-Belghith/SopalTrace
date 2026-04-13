using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using SopalTrace.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Services;

public class ReferentielService : IReferentielService
{
    private readonly SopalTraceDbContext _context;

    public ReferentielService(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<ReferentielsResponseDto> GetFabricationReferentielsAsync()
    {
        return new ReferentielsResponseDto(
            TypesRobinet: await _context.TypeRobinets
                .Where(x => x.Actif)
                .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true))
                .ToListAsync(),
            NaturesComposant: await _context.NatureComposants
                .Where(x => x.Actif)
                .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true))
                .ToListAsync(),
            Operations: await _context.Operations
                .Where(x => x.Actif)
                .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true))
                .ToListAsync(),
            TypesControle: await _context.TypeControles
                .Where(x => x.Actif)
                .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true))
                .ToListAsync(),
            TypesCaracteristique: await _context.TypeCaracteristiques
                .Where(x => x.Actif)
                .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true))
                .ToListAsync(),
            MoyensControle: await _context.MoyenControles
                .Where(x => x.Actif)
                .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true))
                .ToListAsync(),
            Periodicites: await _context.Periodicites
                .Where(x => x.Actif)
                .OrderBy(x => x.OrdreAffichage)
                .Select(x => new PeriodiciteDto(
                    x.Id,
                    x.Code,
                    x.Libelle,
                    x.FrequenceNum,
                    x.FrequenceUnite,
                    x.OrdreAffichage,
                    true))
                .ToListAsync(),
            TypesSections: await _context.TypeSections
                .Where(x => x.Actif)
                .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true))
                .ToListAsync(),
            GroupesInstruments: await _context.GroupeInstruments
                .Where(x => x.Actif)
                .Select(x => new GroupeInstrumentDetailedDto(x.Id, x.CodeAlias, x.Libelle, true))
                .ToListAsync(),
            Instruments: await _context.Instruments
                .Where(x => x.Actif)
                .Select(x => new InstrumentDto(x.CodeInstrument, x.Designation, true))
                .ToListAsync()
        );
    }
}