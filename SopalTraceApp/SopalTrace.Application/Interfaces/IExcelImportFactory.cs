using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IExcelImportFactory
{
    IExcelImporter GetImporter(string machineCode);
}
