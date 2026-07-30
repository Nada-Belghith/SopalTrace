using SopalTrace.Application.DTOs.QualityPlans.Echantillonnage;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IDocumentEchantillonnageService
{
    Task<DocumentEchantillonnageResponseDto?> GetActiveDocumentAsync();
    Task<DocumentEchantillonnageResponseDto?> GetDocumentByIdAsync(Guid id);
    Task<Guid> CreateDocumentAsync(CreateDocumentEchantillonnageRequestDto request, string creePar);
    Task UpdateDocumentAsync(Guid id, UpdateDocumentEchantillonnageRequestDto request);
    Task ActivateDocumentAsync(Guid id, string modifiePar);
    Task<Guid> CreateNewVersionAsync(CreateNewVersionDocumentEchantillonnageRequestDto request);
}
