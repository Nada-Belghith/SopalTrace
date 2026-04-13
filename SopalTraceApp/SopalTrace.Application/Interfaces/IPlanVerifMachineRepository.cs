using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanVerifMachineRepository
{
    Task<bool> ExistePlanActifAsync(string machineCode, string typeRapport, string typeRobinetCode);
    Task<PlanVerifMachineEntete> GetPlanActifAsync(string machineCode, string typeRapport, string typeRobinetCode);
    Task<PlanVerifMachineEntete> GetPlanAvecRelationsAsync(Guid planId);

    Task AddPlanAsync(PlanVerifMachineEntete plan);

    // Pour gérer les suppressions d'enfants dans le Tree Update
    void RemoveLigne(PlanVerifMachineLigne ligne);
    void RemoveEcheance(PlanVerifMachineEcheance echeance);
    void RemovePieceRef(PlanVerifMachinePieceRef pieceRef);
}
