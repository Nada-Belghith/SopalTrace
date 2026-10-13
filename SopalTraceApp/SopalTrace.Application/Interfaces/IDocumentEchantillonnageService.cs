using SopalTrace.Application.DTOs.QualityPlans.Echantillonnage;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IDocumentEchantillonnageService
{
    Task<DocumentEchantillonnageResponseDto?> GetActiveDocumentAsync();
    Task<DocumentEchantillonnageResponseDto?> GetDocumentByIdAsync(Guid id);
    Task<Guid> CreateDocumentAsync(CreateDocumentEchantillonnageRequestDto request);
    /// <returns>Nouvelle version du document (RowVersion en base64).</returns>
    Task<string> UpdateDocumentAsync(Guid id, UpdateDocumentEchantillonnageRequestDto request);
    Task ActivateDocumentAsync(Guid id);
    Task<Guid> CreateNewVersionAsync(CreateNewVersionDocumentEchantillonnageRequestDto request);
}
