using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces;

public interface IDictionnaireQualiteRepository
{
    Task<Periodicite?> GetPeriodiciteByLibelleAsync(string libelle);
    Task<Periodicite?> GetPeriodiciteByCodeAsync(string code);
    Task AddPeriodiciteAsync(Periodicite entite);
    Task<TypeSection?> GetTypeSectionByLibelleAsync(string libelle);
    Task AddTypeSectionAsync(TypeSection entite);
    Task<System.Collections.Generic.List<TypeSection>> GetAllTypeSectionsAsync();
    
    Task<TypeCaracteristique?> GetTypeCaracteristiqueByLibelleAsync(string libelle);
    Task AddTypeCaracteristiqueAsync(TypeCaracteristique entite);

    Task<TypeControle?> GetTypeControleByLibelleAsync(string libelle);
    Task AddTypeControleAsync(TypeControle entite);

    Task<MoyenControle?> GetMoyenControleByLibelleAsync(string libelle);
    Task AddMoyenControleAsync(MoyenControle entite);

    Task<Instrument?> GetInstrumentByCodeAsync(string codeInstrument);
    Task AddInstrumentAsync(Instrument entite);

    Task<RefRegleEchantillonnage?> GetRegleEchantillonnageByLibelleAsync(string libelle);
    Task AddRegleEchantillonnageAsync(RefRegleEchantillonnage entite);

    Task<PieceReference?> GetPieceReferenceByCodeAsync(string code);
    Task AddPieceReferenceAsync(PieceReference entite);

    Task<RefFamilleCorp?> GetFamilleCorpsByCodeAsync(string code);
    Task AddFamilleCorpsAsync(RefFamilleCorp entite);

    Task<RefMoyenDetection?> GetMoyenDetectionByLibelleAsync(string libelle);
    Task AddMoyenDetectionAsync(RefMoyenDetection entite);

    Task<RisqueDefaut?> GetRisqueDefautByLibelleAsync(string libelle);
    Task<RisqueDefaut?> GetRisqueDefautByCodeAsync(string code);
    Task AddRisqueDefautAsync(RisqueDefaut entite);
    Task<System.Collections.Generic.List<TypeRobinet>> GetActiveTypeRobinetsAsync();
    Task<System.Collections.Generic.List<NatureArticle>> GetActiveNatureArticlesFabriqueAsync();
    Task<System.Collections.Generic.List<Operation>> GetActiveOperationsAsync();
    Task<System.Collections.Generic.List<NatureArticleOperation>> GetAllNatureArticleOperationsAsync();
    Task<System.Collections.Generic.List<TypeControle>> GetActiveTypeControlesAsync();
    Task<System.Collections.Generic.List<TypeCaracteristique>> GetActiveTypeCaracteristiquesAsync();
    Task<System.Collections.Generic.List<MoyenControle>> GetActiveMoyenControlesAsync();
    Task<System.Collections.Generic.List<PosteTravail>> GetActivePosteTravailsAsync();
    Task<System.Collections.Generic.List<Periodicite>> GetAllPeriodicitesAsync();
    Task<System.Collections.Generic.List<Instrument>> GetActiveInstrumentsAsync();
    Task<System.Collections.Generic.List<Nqa>> GetActiveNqasAsync();
    Task AddNqaAsync(Nqa entite);
    Task<System.Collections.Generic.List<Defautheque>> GetActiveDefauthequesAsync();
    Task<System.Collections.Generic.List<RefRegleEchantillonnage>> GetActiveRegleEchantillonnagesAsync();
    Task<System.Collections.Generic.List<FamilleProduitFini>> GetActiveFamilleProduitFinisAsync();
    Task<System.Collections.Generic.List<Machine>> GetAllMachinesAsync();
    Task<System.Collections.Generic.List<Machine>> GetActiveMachinesAsync();
    Task<System.Collections.Generic.List<RisqueDefaut>> GetAllRisqueDefautsAsync();
    Task<System.Collections.Generic.List<PieceReference>> GetActivePieceReferencesAsync();
    Task<System.Collections.Generic.List<RefFamilleCorp>> GetAllFamilleCorpsAsync();
    Task<System.Collections.Generic.List<RefMoyenDetection>> GetAllMoyenDetectionsAsync();
    Task<Article?> GetArticleByCodeNormaliseAsync(string codeNormalise);
    Task<System.Collections.Generic.IReadOnlyList<Article>> SearchArticlesSfAsync(string query, int maxResults = 15);
    Task<string?> GetTypeRobinetCodeForArticleAsync(string codeNormalise);
    Task<string?> GetFamilleProduitCodeForArticleAsync(string codeNormalise);
}
