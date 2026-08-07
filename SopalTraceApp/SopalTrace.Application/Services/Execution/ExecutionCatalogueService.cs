using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;

namespace SopalTrace.Application.Services.Execution;

public class ExecutionCatalogueService : IExecutionCatalogueService
{
    private readonly IOperateurRepository _operateurRepository;

    public ExecutionCatalogueService(IOperateurRepository operateurRepository)
    {
        _operateurRepository = operateurRepository;
    }

    public async Task<IEnumerable<PosteTravailDto>> GetPostesDisponiblesAsync()
    {
        var postes = await _operateurRepository.GetPostesDisponiblesAsync();
        return postes.Select(p => new PosteTravailDto { CodePoste = p.CodePoste, Libelle = p.Libelle });
    }

    public async Task<IEnumerable<MachineDto>> GetMachinesPourPosteAsync(string posteCode)
    {
        var poste = await _operateurRepository.GetPosteWithMachinesAsync(posteCode);

        if (poste == null) return new List<MachineDto>();
        
        var periodicitesMap = await _operateurRepository.GetMachinesPlanPeriodicitesAsync();

        return poste.CodeMachines
            .Where(m => m.Actif)
            .Select(m => {
                var per = periodicitesMap.ContainsKey(m.CodeMachine) ? periodicitesMap[m.CodeMachine] : new List<string>();
                return new MachineDto { 
                    CodeMachine = m.CodeMachine, 
                    Libelle = m.Libelle, 
                    HasDemarragePlan = per.Any(p => p.Contains("démarrage") || p.Contains("demarrage") || p.Contains("début") || p.Contains("debut")),
                    HasApresPausePlan = per.Any(p => p.Contains("après la pause") || p.Contains("apres la pause") || p.Contains("après pause") || p.Contains("apres pause") || p.Contains("pause")),
                    HasFinPostePlan = per.Any(p => p.Contains("fin de poste") || p.Contains("fin poste") || p.Contains("fin du poste") || p.Contains("fin"))
                };
            })
            .ToList();
    }

    public async Task<IEnumerable<OperateurOfDto>> GetAllOfOperationsDisponiblesAsync()
    {
        return await _operateurRepository.GetAllOfOperationsDisponiblesAsync();
    }

    public async Task<IEnumerable<MachineDto>> GetAllMachinesAsync()
    {
        var machines = await _operateurRepository.GetAllMachinesAsync();
        var periodicitesMap = await _operateurRepository.GetMachinesPlanPeriodicitesAsync();
        return machines.Select(m => {
            var per = periodicitesMap.ContainsKey(m.CodeMachine) ? periodicitesMap[m.CodeMachine] : new List<string>();
                return new MachineDto { 
                    CodeMachine = m.CodeMachine, 
                    Libelle = m.Libelle, 
                    HasDemarragePlan = per.Any(p => p.Contains("démarrage") || p.Contains("demarrage") || p.Contains("début") || p.Contains("debut")),
                    HasApresPausePlan = per.Any(p => p.Contains("après la pause") || p.Contains("apres la pause") || p.Contains("après pause") || p.Contains("apres pause") || p.Contains("pause")),
                    HasFinPostePlan = per.Any(p => p.Contains("fin de poste") || p.Contains("fin poste") || p.Contains("fin du poste") || p.Contains("fin"))
                };
        }).ToList();
    }
}
