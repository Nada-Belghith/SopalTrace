using SopalTrace.Application.Interfaces;
using SopalTrace.Infrastructure.Services.ExcelImport;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Infrastructure.Services;

public class ExcelImportFactory : IExcelImportFactory
{
    private readonly IEnumerable<IFusionStrategy> _strategies;

    public ExcelImportFactory(IEnumerable<IFusionStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IFusionStrategy GetStrategy(string machineCode)
    {
        if (string.IsNullOrWhiteSpace(machineCode))
        {
            return _strategies.FirstOrDefault(i => i.MachineCode == "DEFAULT");
        }

        var machineUpper = machineCode.ToUpper();
        
        // Find the specific strategy if it exists (e.g. BEE47 matches BEE role)
        var strategy = _strategies.FirstOrDefault(i => machineUpper.Contains(i.MachineCode));
        
        return strategy ?? _strategies.FirstOrDefault(i => i.MachineCode == "DEFAULT");
    }
}
