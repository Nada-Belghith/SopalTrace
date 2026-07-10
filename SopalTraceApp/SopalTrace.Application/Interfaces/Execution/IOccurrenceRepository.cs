using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IOccurrenceRepository
{
    Task<ExecControleOf?> GetExecControleOfWithIntermediairesAsync(Guid execControleOfId);
    Task<List<PlanFabricationSection>> GetSectionsActivesAsync(Guid planEnteteId);
    Task<List<ExecPrelevementIntermediaire>> GetIntermediairesActifsAsync(Guid execControleOfId);
    Task<ExecPrelevementIntermediaire?> GetIntermediaireAsync(Guid id);
    Task<List<ExecPrelevementIntermediaire>> GetIntermediairesParTrancheAsync(Guid execControleOfId, string trancheHoraire);
    Task<ExecControleTranche?> GetTrancheExistanteAsync(Guid execControleOfId, string trancheHoraire);
    Task<ExecControleTranche?> GetDerniereTrancheAsync(Guid execControleOfId);
    void AddIntermediaires(IEnumerable<ExecPrelevementIntermediaire> intermediaires);
    void AddTranche(ExecControleTranche tranche);
    void AddPieceType(ExecPieceType pieceType);

    void RemoveIntermediaires(IEnumerable<ExecPrelevementIntermediaire> intermediaires);
    Task<List<PlanFabricationLigne>> GetLignesForSectionAsync(Guid sectionId);
    Task SaveChangesAsync();
}
