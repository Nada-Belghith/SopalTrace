using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IDocumentVerifMachineService
{
    Task<Guid> CreerDocumentVerifMachineAsync(CreateDocumentVerifMachineRequestDto request);
    Task<DocumentVerifMachineEnteteDto> GetDocumentVerifMachineByIdAsync(Guid id);
    Task<IEnumerable<DocumentVerifMachineEnteteDto>> GetAllPlansAsync();
    Task<IEnumerable<DocumentVerifMachineEnteteDto>> GetPlansByMachineCodeAsync(string machineCode);
    Task MettreAJourDocumentVerifMachineAsync(Guid id, UpdateDocumentVerifMachineRequestDto request);
    Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionVerifMachineRequestDto request);
    Task ArchiverPlansByFormulaireAsync(Guid formulaireId);
}
