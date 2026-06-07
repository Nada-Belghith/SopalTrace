using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces
{
    public interface IPlanArchiverService
    {
        Task ArchivePlanFabricationActifAsync(string codeArticleSage, string? operationCode, string user);
        Task ArchivePlansPfActifsAsync(string familleProduitFiniCode, string user);
        Task ArchivePlanPfActifParFormulaireAsync(Guid formulaireId, string user);
        Task ArchivePlanAssActifAsync(string operationCode, string? familleProduitFiniCode, string? natureComposantCode, string? posteCode, string user, Guid currentPlanId);
        Task ArchiveControlePosteActifAsync(string posteCode, string user);
        Task ArchiveControlePosteActifParFormulaireAsync(Guid formulaireId, string user);
    }
}
