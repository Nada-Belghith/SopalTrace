using System;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;

namespace SopalTrace.Application.Interfaces;

public interface ICatalogueReferentielService
{
    Task<ReferentielsResponseDto> GetFabricationReferentielsAsync(string? natureComposantCode = null, string? operationCode = null);

    /// <summary>Retourne les machines actives et les périodicités pour le module Vérif Machine.</summary>
    Task<VerifMachineReferentielsDto> GetVerifMachineReferentielsAsync();
    
    Task<ControlePosteReferentielsDto> GetControlePosteReferentielsAsync();

    Task<ArticleDto?> GetArticleInfosAsync(string codeArticle);

    /// <summary>Autocomplete : articles SF (hors PISTON) dont le code contient la query.</summary>
    Task<IReadOnlyList<ArticleDto>> SearchArticlesSfAsync(string query, int maxResults = 15);

    Task<Guid> CreatePeriodiciteAsync(CreatePeriodiciteDto request);

    Task<Guid> CreateCaracteristiqueAsync(CreateCaracteristiqueDto request);

    Task<PieceRefDto> CreatePieceReferenceAsync(CreatePieceReferenceDto request);
}
