using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces;

public interface IOperateurService
{
    Task<ExecControleOfDto> DemarrerOfAsync(DemarrerOfRequest request);
    Task<bool> MettreEnReglageAsync(Guid execControleOfId);
    Task<(bool Success, string Message)> ReprendreOfAsync(Guid execControleOfId);
    Task<bool> MettreEnPauseAsync(Guid execControleOfId, string raison);
    Task<bool> ReprendreDepuisPauseAsync(Guid execControleOfId);
    Task<bool> CloturerOfAsync(Guid execControleOfId);
    Task<IEnumerable<PosteTravailDto>> GetPostesDisponiblesAsync();
    Task<IEnumerable<MachineDto>> GetMachinesPourPosteAsync(string posteCode);
    Task<IEnumerable<OperateurOfDto>> GetAllOfOperationsDisponiblesAsync();
    Task<bool> UpdatePlanLigneAsync(Guid ligneId, UpdatePlanLigneDto dto);
    Task<object> VerifierPlanActifAsync(string articleCode, string? operationCode = null);
    Task<bool> IgnorerTrancheAsync(Guid execControleOfId, string trancheHoraire, string matriculeOperateur, string? raison = null);
    Task<bool> DeclarerTrancheEnReglageAsync(Guid execControleOfId, string trancheHoraire, string matriculeOperateur);
    
    // Nouveaux endpoints pour Assemblage
    Task<ExecControleOfDto> DemarrerOfAssemblageAsync(DemarrerOfAssemblageRequest request);
    Task<bool> InitDocumentsAsync(Guid execControleOfId, string typeDocument, string? posteCode, string? machineCode = null, string? equipe = null, string? matricule = null);
    Task<IEnumerable<DocumentStatutDto>> GetDocumentsAssemblageStatusAsync(Guid execControleOfId);
    Task<bool> MarquerDocumentTermineAsync(Guid statutId, string? matricule = null);
    Task<IEnumerable<MachineDto>> GetMachinesByPosteAsync(string posteCode);
    Task<IEnumerable<MachineDto>> GetAllMachinesAsync();
    Task<IEnumerable<OfAssemblageStatutDto>> GetOfsAssemblageStatutAsync();
    Task<bool> AjouterPostesAsync(Guid execControleOfId, List<string> posteCodes);

    // Verif Machine Exec
    Task<SopalTrace.Application.Dtos.VerifMachine.ExecVerifMachineSessionDto> GetExecVerifMachineAsync(Guid statutId, Guid? periodiciteId = null);
    Task<bool> SaveExecVerifMachineAsync(SopalTrace.Application.Dtos.VerifMachine.SaveExecVerifMachineRequest request);
    Task<bool> TerminerToutDocumentsMachineAsync(Guid execControleOfId, string machineCode);
    Task<IEnumerable<object>> GetPeriodicitesMachineAsync();
}
