using SopalTrace.Application.DTOs.QualityPlans.Hub;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IHubService
{
    Task<IReadOnlyList<HubModeleDto>> GetTousLesModelesAsync();
}