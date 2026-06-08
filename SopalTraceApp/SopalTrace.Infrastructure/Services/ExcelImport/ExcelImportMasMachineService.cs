using SopalTrace.Application.Interfaces;

namespace SopalTrace.Infrastructure.Services.ExcelImport;

public class ExcelImportMasMachineService : ExcelImportService, IExcelImporter
{
    public ExcelImportMasMachineService(IUnitOfWork unitOfWork, IFrequencyParserService frequencyParserService) 
        : base(unitOfWork, frequencyParserService)
    {
    }

    public string Role => "MAS";
}
