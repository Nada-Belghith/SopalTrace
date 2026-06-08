using SopalTrace.Application.Interfaces;
using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using System.IO;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Services.ExcelImport;

public class ExcelImportBeeMachineService : ExcelImportService, IExcelImporter
{
    public ExcelImportBeeMachineService(IUnitOfWork unitOfWork, IFrequencyParserService frequencyParserService) 
        : base(unitOfWork, frequencyParserService)
    {
    }

    public string Role => "BEE";

    // You can override BuildVMMap or ParseVerifMachineExcelAsync here 
    // to add BEE specific Excel parsing rules in the future.
}
