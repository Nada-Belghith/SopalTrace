using SopalTrace.Application.DTOs.QualityPlans.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanFabricationService
{
    Task<DocumentEnteteDto?> GetPlanByIdAsync(Guid id);
    Task<Guid> CreerPlanAsync(CreateDocumentRequestDto request);
    Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionDocumentRequestDto request);
    Task<bool> MettreAJourPlanAsync(Guid id, UpdateDocumentRequestDto request);
    Task<Guid> RestaurerPlanArchiveAsync(RestaurerDocumentRequestDto request);
    Task<bool> SupprimerPlanAsync(Guid id);
}
