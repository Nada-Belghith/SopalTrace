using SopalTrace.Application.DTOs.QualityPlans.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IModeleFabricationService
{
    Task<DocumentEnteteDto?> GetModeleByIdAsync(Guid id);
    Task<Guid> CreerModeleAsync(CreateDocumentRequestDto request);
    Task<Guid> CreerNouvelleVersionModeleAsync(NouvelleVersionDocumentRequestDto request);
    Task<bool> MettreAJourModeleAsync(Guid id, UpdateDocumentRequestDto request);
    Task<Guid> RestaurerModeleArchiveAsync(RestaurerDocumentRequestDto request);
    Task<bool> SupprimerModeleAsync(Guid id);
}
