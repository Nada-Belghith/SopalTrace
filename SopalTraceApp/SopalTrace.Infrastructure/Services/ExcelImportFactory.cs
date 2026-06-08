using SopalTrace.Application.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Infrastructure.Services;

public class ExcelImportFactory : IExcelImportFactory
{
    private readonly IEnumerable<IExcelImporter> _importers;

    public ExcelImportFactory(IEnumerable<IExcelImporter> importers)
    {
        _importers = importers;
    }

    public IExcelImporter GetImporter(string machineCode)
    {
        if (string.IsNullOrWhiteSpace(machineCode))
        {
            return _importers.FirstOrDefault(i => i.Role == "DEFAULT");
        }

        var machineUpper = machineCode.ToUpper();
        
        // Find the specific importer if it exists (e.g. BEE47 matches BEE role)
        var importer = _importers.FirstOrDefault(i => machineUpper.Contains(i.Role));
        
        return importer ?? _importers.FirstOrDefault(i => i.Role == "DEFAULT");
    }
}
