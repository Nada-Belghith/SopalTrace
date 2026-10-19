using System;
using System.Threading.Tasks;
using SopalTrace.Application.Dtos.VerifMachine;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IVerifMachineExecutionService
{
    Task<ExecVerifMachineSessionDto> GetExecVerifMachineAsync(Guid statutId);
    Task SaveExecVerifMachineAsync(SaveExecVerifMachineRequest request);
    Task TerminerToutDocumentsMachineAsync(Guid execControleOfId, string machineCode);
    Task CloturerTousVerifMachineAsync(Guid execControleOfId, string posteCode);
}
