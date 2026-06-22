using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Services.ExcelImport;

public interface IExcelImportFactory
{
    IFusionStrategy GetStrategy(string machineCode);
}
