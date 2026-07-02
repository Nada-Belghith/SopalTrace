using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces;

public interface IOperateurService
{
    Task<ExecControleOfDto> DemarrerOfAsync(DemarrerOfRequest request);
    Task<bool> MettreEnReglageAsync(Guid execControleOfId);
    Task<bool> ReprendreOfAsync(Guid execControleOfId);
    Task<bool> CloturerOfAsync(Guid execControleOfId);
    Task<IEnumerable<PosteTravailDto>> GetPostesDisponiblesAsync();
    Task<IEnumerable<MachineDto>> GetMachinesPourPosteAsync(string posteCode);
    Task<IEnumerable<OperateurOfDto>> GetAllOfOperationsDisponiblesAsync();
    Task<bool> UpdatePlanLigneAsync(Guid ligneId, UpdatePlanLigneDto dto);
    Task<bool> VerifierPlanActifAsync(string articleCode);
}
