using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanMachineFactory
{
    IPlanVerifMachineService GetService(string machineCode);
}
