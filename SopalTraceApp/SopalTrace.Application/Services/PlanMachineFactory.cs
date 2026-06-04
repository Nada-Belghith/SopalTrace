using SopalTrace.Application.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Services;

public class PlanMachineFactory : IPlanMachineFactory
{
    private readonly IEnumerable<IPlanVerifMachineService> _services;

    public PlanMachineFactory(IEnumerable<IPlanVerifMachineService> services)
    {
        _services = services;
    }

    public IPlanVerifMachineService GetService(string machineCode)
    {
        if (string.IsNullOrWhiteSpace(machineCode))
        {
            return _services.FirstOrDefault(s => s.Role == "DEFAULT");
        }

        var machineUpper = machineCode.ToUpper();
        
        var service = _services.FirstOrDefault(s => machineUpper.Contains(s.Role));
        
        return service ?? _services.FirstOrDefault(s => s.Role == "DEFAULT");
    }
}
