using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using System.IO;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IExcelImportService
{
    Task<ImportExcelResultDto> ParsePlanExcelAsync(Stream excelStream, string fileName, string configurationColonnesJson = null);
    Task<ImportVerifMachineExcelResultDto> ParseVerifMachineExcelAsync(Stream excelStream, string fileName, string configurationColonnesJson = null);
    Task<ImportNcExcelResultDto> ParsePlanNcExcelAsync(Stream excelStream, string fileName, string configurationColonnesJson = null);
}
