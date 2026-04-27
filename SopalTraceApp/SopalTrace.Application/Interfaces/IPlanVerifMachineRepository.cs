using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanVerifMachineRepository
{
    Task<bool> ExistePlanActifAsync(string machineCode);
    Task<PlanVerifMachineEntete> GetPlanActifAsync(string machineCode);
    Task<PlanVerifMachineEntete> GetPlanAvecRelationsAsync(Guid planId);
    Task<List<PlanVerifMachineEntete>> GetTousLesPlanAsync();

    Task<Guid> GetDefaultRefMoyenDetectionIdAsync();

    Task AddPlanAsync(PlanVerifMachineEntete plan);

    // Pour gérer les suppressions d'enfants dans le Tree Update
    void RemoveLigne(PlanVerifMachineLigne ligne);
    void RemoveEcheance(PlanVerifMachineEcheance echeance);
    void RemoveMatricePiece(PlanVerifMachineMatricePiece matricePiece);
}
