using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IDocumentVerifMachineService
{
    Task<Guid> CreateDocumentAsync(CreateDocumentVerifMachineRequestDto request);
    Task<DocumentVerifMachineEnteteDto> GetDocumentByIdAsync(Guid id);
    Task<IEnumerable<DocumentVerifMachineEnteteDto>> GetAllDocumentsAsync();
    Task<IEnumerable<DocumentVerifMachineEnteteDto>> GetDocumentsByMachineCodeAsync(string machineCode);
    Task UpdateDocumentAsync(Guid id, UpdateDocumentVerifMachineRequestDto request);
    Task<Guid> CreateNewVersionAsync(NouvelleVersionVerifMachineRequestDto request);
    Task ArchiveDocumentsByFormulaireAsync(Guid formulaireId);
}
