using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces;

public interface IDictionnaireQualiteRepository
{
    Task<Periodicite?> GetPeriodiciteByLibelleAsync(string libelle);
    Task AddPeriodiciteAsync(Periodicite entite);
    Task<TypeSection> GetTypeSectionByLibelleAsync(string libelle);
    Task AddTypeSectionAsync(TypeSection entite);
    Task<System.Collections.Generic.List<TypeSection>> GetAllTypeSectionsAsync();
    
    Task<TypeCaracteristique> GetTypeCaracteristiqueByLibelleAsync(string libelle);
    Task AddTypeCaracteristiqueAsync(TypeCaracteristique entite);

    Task<TypeControle> GetTypeControleByLibelleAsync(string libelle);
    Task AddTypeControleAsync(TypeControle entite);

    Task<MoyenControle> GetMoyenControleByLibelleAsync(string libelle);
    Task AddMoyenControleAsync(MoyenControle entite);

    Task<Instrument> GetInstrumentByCodeAsync(string codeInstrument);
    Task AddInstrumentAsync(Instrument entite);

    Task<RefRegleEchantillonnage> GetRegleEchantillonnageByLibelleAsync(string libelle);
    Task AddRegleEchantillonnageAsync(RefRegleEchantillonnage entite);

    Task<PieceReference> GetPieceReferenceByCodeAsync(string code);
    Task AddPieceReferenceAsync(PieceReference entite);

    Task<RefFamilleCorp> GetFamilleCorpsByCodeAsync(string code);
    Task AddFamilleCorpsAsync(RefFamilleCorp entite);

    Task<RefMoyenDetection> GetMoyenDetectionByLibelleAsync(string libelle);
    Task AddMoyenDetectionAsync(RefMoyenDetection entite);

    Task<RisqueDefaut?> GetRisqueDefautByLibelleAsync(string libelle);
    Task<RisqueDefaut?> GetRisqueDefautByCodeAsync(string code);
    Task AddRisqueDefautAsync(RisqueDefaut entite);
}
