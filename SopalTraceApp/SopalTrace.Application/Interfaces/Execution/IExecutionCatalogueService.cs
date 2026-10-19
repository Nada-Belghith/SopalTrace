using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IExecutionCatalogueService
{
    Task<IEnumerable<PosteTravailDto>> GetPostesDisponiblesAsync();
    Task<IEnumerable<MachineDto>> GetMachinesPourPosteAsync(string posteCode);
    Task<IEnumerable<OperateurOfDto>> GetAllOfOperationsDisponiblesAsync();
    Task<IEnumerable<MachineDto>> GetAllMachinesAsync();
}
