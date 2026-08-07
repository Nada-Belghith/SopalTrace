using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IAssemblageExecutionService
{
    Task<ExecControleOfDto> DemarrerOfAssemblageAsync(DemarrerOfAssemblageRequest request);
    Task<bool> InitDocumentsAsync(Guid execControleOfId, string typeDocument, string? posteCode, string? machineCode = null, string? equipe = null, string? matricule = null);
    Task<IEnumerable<DocumentStatutDto>> GetDocumentsAssemblageStatusAsync(Guid execControleOfId);
    Task<bool> MarquerDocumentTermineAsync(Guid statutId, string? matricule = null, string periodicite = "");
    Task<bool> RouvrirDocumentAsync(Guid statutId, string periodicite = "");
    Task<IEnumerable<MachineDto>> GetMachinesByPosteAsync(string posteCode);
    Task<IEnumerable<OfAssemblageStatutDto>> GetOfsAssemblageStatutAsync();
    Task<bool> AjouterPostesAsync(Guid execControleOfId, List<string> posteCodes);
}
