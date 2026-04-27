using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Infrastructure.Data;

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
        var typesRobinet = (await _context.TypeRobinets
            .Where(x => x.Actif)
            .Select(x => new { x.Code, x.Libelle })
            .ToListAsync())
            .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true, null))
            .ToList();

        var naturesComposant = (await _context.NatureComposants
            .Where(x => x.Actif)
            .Select(x => new { x.Code, x.Libelle, x.EstGenerique })
            .ToListAsync())
            .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true, x.EstGenerique))
            .ToList();

        var operations = (await _context.Operations
            .Where(x => x.Actif)
            .Select(x => new { x.Code, x.Libelle })
            .ToListAsync())
            .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true, null))
            .ToList();

        var typesControle = (await _context.TypeControles
            .Where(x => x.Actif)
            .Select(x => new { x.Id, x.Code, x.Libelle })
            .ToListAsync())
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true, null))
            .ToList();

        var typesCaracteristique = (await _context.TypeCaracteristiques
            .Where(x => x.Actif)
            .Select(x => new { x.Id, x.Code, x.Libelle })
            .ToListAsync())
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true, null))
            .ToList();

        var moyensControle = (await _context.MoyenControles
            .Where(x => x.Actif)
            .Select(x => new { x.Id, x.Code, x.Libelle })
            .ToListAsync())
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true, null))
            .ToList();

        var typesSections = (await _context.TypeSections
            .Where(x => x.Actif)
            .Select(x => new { x.Id, x.Code, x.Libelle })
            .ToListAsync())
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true, null))
            .ToList();

        return new ReferentielsResponseDto(
            TypesRobinet: typesRobinet,

            NaturesComposant: naturesComposant,

            Operations: operations,

            TypesControle: typesControle,

            TypesCaracteristique: typesCaracteristique,

            MoyensControle: moyensControle,

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

            TypesSections: typesSections,

            Instruments: await _context.Instruments
                .Where(x => x.Actif)
                .Select(x => new InstrumentDto(x.CodeInstrument, x.Designation, true))
                .ToListAsync(),

            Gammes: await _context.NatureComposantOperations
                .Select(g => new GammeDto(g.NatureComposantCode, g.OperationCode))
                .ToListAsync(),

            Nqa: (await _context.Nqas
                .Select(x => new { x.Id, x.ValeurNqa })
                .ToListAsync())
                .Select(x => new ReferenceItemIntDto(x.Id, x.ValeurNqa.ToString(), x.ValeurNqa.ToString(), true))
                .ToList(),

            Defautheque: (await _context.Defautheques
                .Select(x => new { x.Id, x.Code, x.Description })
                .ToListAsync())
                .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Description ?? x.Code, true, null))
                .ToList()
        );
    }

    public async Task<PlanNcReferentielsDto> GetPlanNcReferentielsAsync()
    {
        var postes = (await _context.PosteTravails
            .Where(p => p.Actif)
            .Select(p => new { p.CodePoste, p.Libelle })
            .ToListAsync())
            .Select(p => new ReferenceItemDto(null, p.CodePoste, p.Libelle, true, null))
            .ToList();

        // Pour les machines, on inclut le code du poste si associ
        var machines = await _context.Machines
            .Where(m => m.Actif)
            .Select(m => new MachinePosteDto(
                m.CodeMachine,
                m.Libelle,
                m.CodePostes.Select(cp => cp.CodePoste).FirstOrDefault()
            ))
            .ToListAsync();

        var risquesDefauts = (await _context.RisqueDefauts
            .Where(r => r.Actif)
            .Select(r => new { r.Id, r.CodeDefaut, r.LibelleDefaut })
            .ToListAsync())
            .Select(r => new ReferenceItemDto(r.Id, r.CodeDefaut, r.LibelleDefaut, true, null))
            .ToList();

        return new PlanNcReferentielsDto(postes, machines, risquesDefauts);
    }

    public async Task<VerifMachineReferentielsDto> GetVerifMachineReferentielsAsync()
    {
        var machines = (await _context.Machines
            .Where(m => m.Actif)
            .Select(m => new { m.CodeMachine, m.Libelle })
            .ToListAsync())
            .Select(m => new ReferenceItemDto(null, m.CodeMachine, m.Libelle, true, null))
            .ToList();

        var periodicites = await _context.Periodicites
            .Where(p => p.Actif)
            .OrderBy(p => p.OrdreAffichage)
            .Select(p => new PeriodiciteDto(p.Id, p.Code, p.Libelle, p.FrequenceNum, p.FrequenceUnite, p.OrdreAffichage, true))
            .ToListAsync();

        var allPieces = await _context.PieceReferences
            .Where(p => p.Actif)
            .Select(p => new PieceRefDto(p.Id, p.Code, p.Designation, p.FamilleDesc, p.MachineCode, p.TypePiece))
            .ToListAsync();

        var piecesRef = allPieces.Where(p => p.TypePiece == "PRC" || p.TypePiece == "PRNC").ToList();
        var fuitesEtalon = allPieces.Where(p => p.TypePiece == "FEC" || p.TypePiece == "FENC").ToList();

        var famillesCorps = (await _context.RefFamilleCorps
            .Where(f => f.Actif)
            .Select(f => new { f.Id, f.Code, f.Designation })
            .ToListAsync())
            .Select(f => new ReferenceItemDto(f.Id, f.Code, f.Designation, true, null))
            .ToList();

        var moyensDetection = (await _context.RefMoyenDetections
            .Where(m => m.Actif)
            .Select(m => new { m.Id, m.Code, m.Designation })
            .ToListAsync())
            .Select(m => new ReferenceItemDto(m.Id, m.Code, m.Designation, true, null))
            .ToList();

        return new VerifMachineReferentielsDto(machines, periodicites, piecesRef, fuitesEtalon, famillesCorps, moyensDetection);
    }

    public async Task<ArticleDto?> GetArticleInfosAsync(string codeArticle)
    {
        var article = await _context.Itmmasters
            .Where(a => a.CodeArticle == codeArticle)
            .Select(a => new ArticleDto(
                a.CodeArticle,
                a.Designation,
                a.TypeRobinetCode,
                a.NatureComposantCode
            ))
            .FirstOrDefaultAsync();

        return article;
    }

    public async Task<Guid> CreatePeriodiciteAsync(CreatePeriodiciteDto request)
    {
        var existeDeja = await _context.Periodicites.AnyAsync(x => x.Code == request.Code);
        if (existeDeja)
            throw new InvalidOperationException("Une p�riodicit� avec ce code existe d�j�.");

        var periodicite = PeriodiciteMapper.MapToEntity(request);
        _context.Periodicites.Add(periodicite);
        await _context.SaveChangesAsync();

        return periodicite.Id;
    }

    public async Task<Guid> CreateCaracteristiqueAsync(CreateCaracteristiqueDto request)
    {
        var caracteristique = CaracteristiqueMapper.MapToEntity(request);

        var existeDeja = await _context.TypeCaracteristiques.AnyAsync(
            x => x.Code == caracteristique.Code || x.Libelle == request.Libelle);
        if (existeDeja)
            throw new InvalidOperationException("Une caract�ristique avec ce nom existe d�j�.");

        _context.TypeCaracteristiques.Add(caracteristique);
        await _context.SaveChangesAsync();

        return caracteristique.Id;
    }
}