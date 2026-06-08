using SopalTrace.Application.Interfaces;

namespace SopalTrace.Infrastructure.Services.ExcelImport;

public class ExcelImportSer05MachineService : ExcelImportService, IExcelImporter
{
    public ExcelImportSer05MachineService(IUnitOfWork unitOfWork, IFrequencyParserService frequencyParserService) 
        : base(unitOfWork, frequencyParserService)
    {
    }

    public string Role => "SER05";
}
