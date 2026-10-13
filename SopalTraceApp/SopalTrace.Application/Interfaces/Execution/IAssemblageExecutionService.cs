using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IAssemblageExecutionService
{
    Task<IEnumerable<OfAssemblageStatutDto>> GetOfsAssemblageStatutAsync();
    Task<ExecControleOfDto> DemarrerOfAssemblageAsync(DemarrerOfAssemblageRequest request, string matricule);
    Task AjouterPostesAsync(Guid execControleOfId, AjouterPostesRequest request, string matricule);

    /// <summary>Crée (ou retrouve) le statut du document de vérification machine du jour et renvoie son identifiant.</summary>
    Task<Guid> InitVerifMachineAsync(Guid execControleOfId, InitVerifMachineRequest request, string matricule);
    Task<IEnumerable<DocumentStatutDto>> GetDocumentsAssemblageStatusAsync(Guid execControleOfId, string? equipe = null);
    Task MarquerDocumentTermineAsync(Guid statutId, string matricule, string? periodicite);
    Task RouvrirDocumentAsync(Guid statutId, string matricule, string? periodicite);
}
