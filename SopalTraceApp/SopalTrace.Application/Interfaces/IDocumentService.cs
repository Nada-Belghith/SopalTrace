using SopalTrace.Application.DTOs.QualityPlans.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IDocumentService
{
    Task<Guid> CreerDocumentAsync(CreateDocumentRequestDto request);
    Task<DocumentEnteteDto> GetDocumentByIdAsync(Guid documentId);
    Task<IReadOnlyList<DocumentEnteteDto>> GetDocumentsByFiltersAsync(
        string typeDocumentCode,
        string? natureComposantCode = null, 
        string? operationCode = null, 
        string? posteCode = null, 
        string? familleProduitCode = null,
        string? statut = null);
        
    Task<Guid> CreerNouvelleVersionDocumentAsync(NouvelleVersionDocumentRequestDto request);
    Task<Guid> RestaurerDocumentArchiveAsync(RestaurerDocumentRequestDto request);
    Task<bool> MettreAJourDocumentAsync(Guid id, UpdateDocumentRequestDto request);
    Task<bool> SupprimerDocumentAsync(Guid id);
}
