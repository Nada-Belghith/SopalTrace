using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using System.IO;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IExcelImportService
{
    Task<ImportExcelResultDto> ParsePlanExcelAsync(Stream excelStream);
}
