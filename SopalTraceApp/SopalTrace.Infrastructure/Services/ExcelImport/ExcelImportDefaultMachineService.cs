using SopalTrace.Application.Interfaces;

namespace SopalTrace.Infrastructure.Services.ExcelImport;

public class ExcelImportDefaultMachineService : ExcelImportService, IExcelImporter
{
    public ExcelImportDefaultMachineService(IUnitOfWork unitOfWork, IFrequencyParserService frequencyParserService) 
        : base(unitOfWork, frequencyParserService)
    {
    }

    public string Role => "DEFAULT";
}
