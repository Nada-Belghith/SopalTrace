using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using System.IO;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IExcelImporter
{
    string Role { get; }
    Task<ImportVerifMachineExcelResultDto> ParseVerifMachineExcelAsync(Stream excelStream, string fileName, string configurationColonnesJson = null);
}
