using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Infrastructure.Services;

public class ReferentielService : IReferentielService
{
    private readonly SopalTraceDbContext _context;

    public ReferentielService(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<ReferentielsResponseDto> GetFabricationReferentielsAsync(string? natureComposantCode = null, string? operationCode = null)
    {
        var typesRobinet = (await _context.TypeRobinets
            .Where(x => x.Actif)
            .Select(x => new { x.Code, x.Libelle })
            .ToListAsync())
            .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true, null))
            .ToList();

        var natureQuery = _context.NatureArticles.Where(x => x.Actif && x.Origine == "FABRIQUE");
        if (!string.IsNullOrEmpty(operationCode))
        {
            natureQuery = natureQuery.Where(n => _context.NatureArticleOperations
                .Any(nao => nao.NatureArticleCode == n.Code && nao.OperationCode == operationCode));
        }

        var naturesComposant = (await natureQuery
            .Select(x => new { x.Code, x.Libelle })
            .ToListAsync())
            .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true, x.Code == "MATIERE" || x.Code == "SEMI_FINI"))
            .ToList();

        var opQuery = _context.Operations.Where(x => x.Actif);
        if (!string.IsNullOrEmpty(natureComposantCode))
        {
            opQuery = opQuery.Where(o => _context.NatureArticleOperations
                .Any(nao => nao.NatureArticleCode == natureComposantCode && nao.OperationCode == o.Code));
        }

        var operations = (await opQuery
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

        var postes = (await _context.PosteTravails
            .Where(x => x.Actif)
            .OrderBy(x => x.CodePoste)
            .Select(x => new { x.CodePoste, x.Libelle })
            .ToListAsync())
            .Select(x => new ReferenceItemDto(null, x.CodePoste, x.Libelle, true, null))
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

            Postes: postes,

            Gammes: await _context.NatureArticleOperations
                .Select(nao => new GammeDto(nao.NatureArticleCode, nao.OperationCode))
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
                .ToList(),
            ReglesEchantillonnage: (await _context.RefRegleEchantillonnages
                .Where(x => x.Actif)
                .Select(x => new { x.Id, x.Code, x.Libelle })
                .ToListAsync())
                .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true, null))
                .ToList(),

            FamillesProduit: await _context.FamilleProduitFinis
                .Where(x => x.Actif == true)
                .Select(x => new FamilleProduitDto(x.Code, x.Designation ?? string.Empty, x.TypeRobinetCode ?? string.Empty))
                .ToListAsync()
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
            .Select(p => new PieceRefDto(p.Id, p.Code, p.Designation, p.FamilleDesc, p.TypePiece))
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
        var article = await _context.Articles
            .AsNoTracking()
            .Include(a => a.NatureArticleCodeNavigation)
            .Include(a => a.ProduitFini)
            .Where(a => a.CodeArticle == codeArticle)
            .FirstOrDefaultAsync();

        if (article == null) return null;

        // Security check: Only allow fabricated articles
        if (article.NatureArticleCodeNavigation.Origine != "FABRIQUE") return null;

        var validOps = await _context.ModeleFabricationEntetes
            .AsNoTracking()
            .Where(m => EF.Property<string>(m, "NatureArticleCode") == article.NatureArticleCode && EF.Property<string>(m, "Statut") == "ACTIF")
            .Select(m => m.OperationCode)
            .Distinct()
            .ToListAsync();

        var typeRobinetCode = article.ProduitFini?.TypeRobinetCode;

        // Si le type de robinet (famille) n'est pas renseigné (cas fréquent des semi-finis SF comme Corps/Volant),
        // on cherche le produit fini (PF) parent dans la nomenclature pour hériter de sa famille.
        if (string.IsNullOrEmpty(typeRobinetCode))
        {
            typeRobinetCode = await _context.BomdNomenclatures
                .AsNoTracking()
                .Where(b => b.CodeComposant == codeArticle)
                .Select(b => b.ArticleParentNavigation.ProduitFini != null ? b.ArticleParentNavigation.ProduitFini.TypeRobinetCode : null)
                .FirstOrDefaultAsync(x => x != null);
        }

        return new ArticleDto(
            article.CodeArticle,
            article.Designation,
            typeRobinetCode,
            article.NatureArticleCode,
            validOps
        );
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
            throw new InvalidOperationException("Une caractéristique avec ce nom existe déjà.");

        _context.TypeCaracteristiques.Add(caracteristique);
        await _context.SaveChangesAsync();

        return caracteristique.Id;
    }

    public async Task<PieceRefDto> CreatePieceReferenceAsync(CreatePieceReferenceDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Le code de la pièce est obligatoire.");

        var typesValides = new[] { "PRC", "PRNC", "FEC", "FENC" };
        if (!typesValides.Contains(request.TypePiece))
            throw new ArgumentException($"Type de pièce invalide : '{request.TypePiece}'. Valeurs acceptées : PRC, PRNC, FEC, FENC.");

        var codeNormalisé = request.Code.Trim().ToUpperInvariant();
        var existeDeja = await _context.PieceReferences.AnyAsync(p => p.Code == codeNormalisé);
        if (existeDeja)
            throw new InvalidOperationException($"Une pièce référence avec le code '{codeNormalisé}' existe déjà.");

        var entite = new SopalTrace.Domain.Entities.PieceReference
        {
            Id = Guid.NewGuid(),
            Code = codeNormalisé,
            TypePiece = request.TypePiece,
            Designation = request.Designation?.Trim(),
            FamilleDesc = request.FamilleDesc?.Trim(),
            Actif = true
        };

        _context.PieceReferences.Add(entite);
        await _context.SaveChangesAsync();

        return new PieceRefDto(entite.Id, entite.Code, entite.Designation, entite.FamilleDesc, entite.TypePiece);
    }

    public async Task<FormulaireStructureDto?> GetFormulaireByRoleAsync(string role)
    {
        var formulaire = await _context.RefFormulaires
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Role == role && f.Statut == "ACTIF");

        if (formulaire == null) return null;

        return new FormulaireStructureDto(
            formulaire.Id,
            formulaire.CodeReference,
            formulaire.Designation,
            formulaire.ConfigurationStructureJson,
            formulaire.Role ?? string.Empty,
            formulaire.Version
        );
    }

    public async Task<bool> UpdateFormulaireStructureAsync(string role, string? configurationStructureJson)
    {
        var formulaireActuel = await _context.RefFormulaires
            .FirstOrDefaultAsync(f => f.Role == role && f.Statut == "ACTIF");

        if (formulaireActuel != null)
        {
            // Archive the current active version
            formulaireActuel.Statut = "ARCHIVE";

            // Create a new active version with version incremented
            var newVersion = formulaireActuel.Version + 1;
            var nouveauFormulaire = new RefFormulaire
            {
                Id = Guid.NewGuid(),
                CodeReference = formulaireActuel.CodeReference,
                Designation = formulaireActuel.Designation,
                Version = newVersion,
                Statut = "ACTIF",
                CreeLe = DateTime.UtcNow,
                Role = role,
                ConfigurationStructureJson = configurationStructureJson
            };
            _context.RefFormulaires.Add(nouveauFormulaire);
        }
        else
        {
            // Fallback if no active version exists yet
            var nouveauFormulaire = new RefFormulaire
            {
                Id = Guid.NewGuid(),
                CodeReference = "PRC",
                Designation = "Formulaire " + role,
                Version = 1,
                Statut = "ACTIF",
                CreeLe = DateTime.UtcNow,
                Role = role,
                ConfigurationStructureJson = configurationStructureJson
            };
            _context.RefFormulaires.Add(nouveauFormulaire);
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
