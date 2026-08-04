using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.Dtos.VerifMachine;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IVerifMachineExecutionService
{
    Task<ExecVerifMachineSessionDto> GetExecVerifMachineAsync(Guid statutId, Guid? periodiciteId = null);
    Task<bool> SaveExecVerifMachineAsync(SaveExecVerifMachineRequest request);
    Task<bool> TerminerToutDocumentsMachineAsync(Guid execControleOfId, string machineCode);
    Task<bool> CloturerTousVerifMachineAsync(Guid execControleOfId, string posteCode);
    Task<IEnumerable<object>> GetPeriodicitesMachineAsync();
}
