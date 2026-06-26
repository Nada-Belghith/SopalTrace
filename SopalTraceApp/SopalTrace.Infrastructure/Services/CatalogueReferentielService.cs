using System;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Infrastructure.Services;

public class CatalogueReferentielService : ICatalogueReferentielService
{
    private readonly IDictionnaireQualiteRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CatalogueReferentielService(IDictionnaireQualiteRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReferentielsResponseDto> GetFabricationReferentielsAsync(string? natureComposantCode = null, string? operationCode = null)
    {
        var typesRobinet = (await _repository.GetActiveTypeRobinetsAsync())
            .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true, null))
            .ToList();

        var natures = await _repository.GetActiveNatureArticlesFabriqueAsync();
        var naops = await _repository.GetAllNatureArticleOperationsAsync();
        
        if (!string.IsNullOrEmpty(operationCode))
        {
            natures = natures.Where(n => naops.Any(nao => nao.NatureArticleCode == n.Code && nao.OperationCode == operationCode)).ToList();
        }

        var naturesComposant = natures
            .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true, x.Code == "MATIERE" || x.Code == "SEMI_FINI"))
            .ToList();

        var ops = await _repository.GetActiveOperationsAsync();
        if (!string.IsNullOrEmpty(natureComposantCode))
        {
            ops = ops.Where(o => naops.Any(nao => nao.NatureArticleCode == natureComposantCode && nao.OperationCode == o.Code)).ToList();
        }

        var operations = ops
            .Select(x => new ReferenceItemDto(null, x.Code, x.Libelle, true, null))
            .ToList();

        var typesControle = (await _repository.GetActiveTypeControlesAsync())
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true, null))
            .ToList();

        var typesCaracteristique = (await _repository.GetActiveTypeCaracteristiquesAsync())
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true, null))
            .ToList();

        var moyensControle = (await _repository.GetActiveMoyenControlesAsync())
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true, null))
            .ToList();

        var typesSections = (await _repository.GetAllTypeSectionsAsync())
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true, null))
            .ToList();

        var postes = (await _repository.GetActivePosteTravailsAsync())
            .OrderBy(x => x.CodePoste)
            .Select(x => new ReferenceItemDto(null, x.CodePoste, x.Libelle, true, null))
            .ToList();

        var periodicites = (await _repository.GetAllPeriodicitesAsync())
            .Where(x => x.Actif)
            .OrderBy(x => x.OrdreAffichage)
            .Select(x => new PeriodiciteDto(x.Id, x.Code, x.Libelle, x.FrequenceNum, x.FrequenceUnite, x.OrdreAffichage, true))
            .ToList();

        var instruments = (await _repository.GetActiveInstrumentsAsync())
            .Select(x => new InstrumentDto(x.CodeInstrument, x.Designation, true))
            .ToList();

        var gammes = naops
            .Select(nao => new GammeDto(nao.NatureArticleCode, nao.OperationCode))
            .ToList();

        var nqas = (await _repository.GetActiveNqasAsync())
            .Select(x => new ReferenceItemIntDto(x.Id, x.ValeurNqa.ToString(), x.ValeurNqa.ToString(), true))
            .ToList();

        var defautheques = (await _repository.GetActiveDefauthequesAsync())
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Description ?? x.Code, true, null))
            .ToList();

        var reglesEchantillonnage = (await _repository.GetActiveRegleEchantillonnagesAsync())
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Libelle, true, null))
            .ToList();

        var famillesProduit = (await _repository.GetActiveFamilleProduitFinisAsync())
            .Select(x => new FamilleProduitDto(x.Code, x.Designation ?? string.Empty, x.TypeRobinetCode ?? string.Empty))
            .ToList();

        return new ReferentielsResponseDto(
            TypesRobinet: typesRobinet,
            NaturesComposant: naturesComposant,
            Operations: operations,
            TypesControle: typesControle,
            TypesCaracteristique: typesCaracteristique,
            MoyensControle: moyensControle,
            Periodicites: periodicites,
            TypesSections: typesSections,
            Instruments: instruments,
            Postes: postes,
            Gammes: gammes,
            Nqa: nqas,
            Defautheque: defautheques,
            ReglesEchantillonnage: reglesEchantillonnage,
            FamillesProduit: famillesProduit
        );
    }

    public async Task<ControlePosteReferentielsDto> GetControlePosteReferentielsAsync()
    {
        var postes = (await _repository.GetActivePosteTravailsAsync())
            .Select(p => new ReferenceItemDto(null, p.CodePoste, p.Libelle, true, null))
            .ToList();

        var machines = (await _repository.GetActiveMachinesAsync())
            .Select(m => new MachinePosteDto(
                m.CodeMachine,
                m.Libelle,
                m.CodePostes?.Select(cp => cp.CodePoste).FirstOrDefault() // Might be null if not lazy/eager loaded
            ))
            .ToList();

        var risquesDefauts = (await _repository.GetAllRisqueDefautsAsync())
            .Where(r => r.Actif)
            .Select(r => new ReferenceItemDto(r.Id, r.CodeDefaut, r.LibelleDefaut, true, null))
            .ToList();

        return new ControlePosteReferentielsDto(postes, machines, risquesDefauts);
    }

    public async Task<VerifMachineReferentielsDto> GetVerifMachineReferentielsAsync()
    {
        var machines = (await _repository.GetActiveMachinesAsync())
            .Select(m => new ReferenceItemDto(null, m.CodeMachine, m.Libelle, true, null))
            .ToList();

        var periodicites = (await _repository.GetAllPeriodicitesAsync())
            .Where(p => p.Actif)
            .OrderBy(p => p.OrdreAffichage)
            .Select(p => new PeriodiciteDto(p.Id, p.Code, p.Libelle, p.FrequenceNum, p.FrequenceUnite, p.OrdreAffichage, true))
            .ToList();

        var allPieces = await _repository.GetActivePieceReferencesAsync();
        var piecesRef = allPieces.Where(p => p.TypePiece == "PRC" || p.TypePiece == "PRNC")
                                 .Select(p => new PieceRefDto(p.Id, p.Code, p.Designation, p.FamilleDesc, p.TypePiece)).ToList();
        var fuitesEtalon = allPieces.Where(p => p.TypePiece == "FEC" || p.TypePiece == "FENC")
                                    .Select(p => new PieceRefDto(p.Id, p.Code, p.Designation, p.FamilleDesc, p.TypePiece)).ToList();

        var famillesCorps = (await _repository.GetAllFamilleCorpsAsync())
            .Where(f => f.Actif)
            .Select(f => new ReferenceItemDto(f.Id, f.Code, f.Designation, true, null))
            .ToList();

        var moyensDetection = (await _repository.GetAllMoyenDetectionsAsync())
            .Where(m => m.Actif)
            .Select(m => new ReferenceItemDto(m.Id, m.Code, m.Designation, true, null))
            .ToList();

        return new VerifMachineReferentielsDto(machines, periodicites, piecesRef, fuitesEtalon, famillesCorps, moyensDetection);
    }

    public async Task<ArticleDto?> GetArticleInfosAsync(string codeArticle)
    {
        if (string.IsNullOrWhiteSpace(codeArticle))
            return null;

        var code = codeArticle.Trim().ToUpperInvariant();

        var article = await _repository.GetArticleByCodeNormaliseAsync(code);

        if (article == null)
            return null;

        string? typeRobinetCode = await _repository.GetTypeRobinetCodeForArticleAsync(code);
        string? familleProduitCode = await _repository.GetFamilleProduitCodeForArticleAsync(code);

        return new ArticleDto(
            article.CodeArticle,
            article.Designation,
            typeRobinetCode,
            article.NatureArticleCode,
            null,
            familleProduitCode
        );
    }

    public async Task<IReadOnlyList<ArticleDto>> SearchArticlesSfAsync(string query, int maxResults = 15)
    {
        var articles = await _repository.SearchArticlesSfAsync(query, maxResults);
        return articles.Select(a => new ArticleDto(
            a.CodeArticle,
            a.Designation,
            null, // typeRobinetCode non nécessaire pour l'autocomplete
            a.NatureArticleCode
        )).ToList();
    }

    public async Task<Guid> CreatePeriodiciteAsync(CreatePeriodiciteDto request)
    {
        // Chercher d'abord par libellé (cas le plus courant)
        var existingByLibelle = await _repository.GetPeriodiciteByLibelleAsync(request.Libelle);
        if (existingByLibelle != null)
        {
            return existingByLibelle.Id; // Retourner l'existante au lieu d'erreur
        }

        // Puis par code
        var existingByCode = await _repository.GetPeriodiciteByCodeAsync(request.Code);
        if (existingByCode != null)
        {
            return existingByCode.Id; // Retourner l'existante au lieu d'erreur
        }

        var periodicite = new Periodicite
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Libelle = request.Libelle, // ✅ Utiliser le libellé lisible, pas le code
            FrequenceNum = request.FrequenceNum,
            FrequenceUnite = request.FrequenceUnite,
            OrdreAffichage = request.OrdreAffichage,
            Actif = true
        };

        await _repository.AddPeriodiciteAsync(periodicite);
        await _unitOfWork.CommitAsync();
        return periodicite.Id;
    }

    public async Task<Guid> CreateCaracteristiqueAsync(CreateCaracteristiqueDto request)
    {
        var libelleNormalise = request.Libelle.Trim().ToLowerInvariant();
        var existing = await _repository.GetTypeCaracteristiqueByLibelleAsync(request.Libelle);

        if (existing != null)
        {
            throw new InvalidOperationException($"Une caractéristique avec le libellé '{request.Libelle}' existe déjà.");
        }

        var caracteristique = new TypeCaracteristique
        {
            Id = Guid.NewGuid(),
            Code = request.Libelle.Trim().Substring(0, Math.Min(5, request.Libelle.Trim().Length)).ToUpperInvariant(),
            Libelle = request.Libelle.Trim(),
            Actif = true
        };

        await _repository.AddTypeCaracteristiqueAsync(caracteristique);
        await _unitOfWork.CommitAsync();
        return caracteristique.Id;
    }

    public async Task<PieceRefDto> CreatePieceReferenceAsync(CreatePieceReferenceDto request)
    {
        var codeNormalise = request.Code.Trim().ToUpperInvariant();
        var existing = await _repository.GetPieceReferenceByCodeAsync(codeNormalise);

        if (existing != null)
        {
            throw new InvalidOperationException($"Une pièce de référence avec le code {codeNormalise} existe déjà.");
        }

        var entite = new PieceReference
        {
            Id = Guid.NewGuid(),
            Code = codeNormalise,
            TypePiece = request.TypePiece,
            Designation = request.Designation?.Trim(),
            FamilleDesc = request.FamilleDesc?.Trim(),
            Actif = true
        };

        await _repository.AddPieceReferenceAsync(entite);
        await _unitOfWork.CommitAsync();
        return new PieceRefDto(entite.Id, entite.Code, entite.Designation, entite.FamilleDesc, entite.TypePiece);
    }
}
