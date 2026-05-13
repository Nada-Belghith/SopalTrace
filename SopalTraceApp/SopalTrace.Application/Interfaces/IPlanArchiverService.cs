using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces
{
    public interface IPlanArchiverService
    {
        Task ArchivePlanFabricationActifAsync(string codeArticleSage, string? operationCode, string user);
        Task ArchivePlansPfActifsAsync(string familleProduitFiniCode, string user);
        Task ArchivePlanAssActifAsync(string operationCode, string familleProduitFiniCode, string user, Guid currentPlanId);
        Task ArchivePlanNcActifAsync(string posteCode, string user);
    }
}
