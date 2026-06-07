using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IReferentielService
{
    Task<ReferentielsResponseDto> GetFabricationReferentielsAsync(string? natureComposantCode = null, string? operationCode = null);

    /// <summary>Retourne les machines actives et les périodicités pour le module Vérif Machine.</summary>
    Task<VerifMachineReferentielsDto> GetVerifMachineReferentielsAsync();
    
    Task<ControlePosteReferentielsDto> GetControlePosteReferentielsAsync();

    Task<ArticleDto?> GetArticleInfosAsync(string codeArticle);

    Task<Guid> CreatePeriodiciteAsync(CreatePeriodiciteDto request);

    Task<Guid> CreateCaracteristiqueAsync(CreateCaracteristiqueDto request);

    Task<PieceRefDto> CreatePieceReferenceAsync(CreatePieceReferenceDto request);

    Task<FormulaireStructureDto?> GetFormulaireByRoleAsync(string role);
    Task<FormulaireStructureDto?> GetFormulaireByIdAsync(Guid id);
    Task<FormulaireStructureDto?> GetFormulaireActifParCodeReferenceAsync(string codeReference);
    Task<IEnumerable<FormulaireReferenceItemDto>> GetFormulairesListByRoleAsync(string role);
    /// <summary>
    /// Archive le formulaire actif identifié par son codeReference et crée une nouvelle version active avec version+1.
    /// Si codeReference est null, utilise le role pour trouver le formulaire actif (comportement générique).
    /// </summary>
    Task<(Guid Id, int Version)?> UpdateFormulaireStructureAsync(string role, string? configurationStructureJson, string? codeReference = null, int? versionInitiale = null);
    Task<bool> ActiverFormulaireAsync(Guid id);
}
