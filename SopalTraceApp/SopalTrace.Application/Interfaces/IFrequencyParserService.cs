using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;

namespace SopalTrace.Application.Interfaces
{
    public interface IFrequencyParserService
    {
        Task ParseFrequencyAsync(ImportExcelSectionDto section, string parenthesesContent);
    }
}
