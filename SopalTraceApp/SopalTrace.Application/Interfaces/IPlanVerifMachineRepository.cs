using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanVerifMachineRepository
{
    Task<bool> ExistePlanActifAsync(string machineCode);
    Task<bool> ExistePlanActifParFormulaireAsync(Guid formulaireId);
    Task<PlanVerifMachineEntete> GetPlanActifAsync(string machineCode);
    Task<PlanVerifMachineEntete?> GetPlanActifParFormulaireAsync(Guid formulaireId);
    Task<PlanVerifMachineEntete> GetPlanAvecRelationsAsync(Guid planId);
    Task<List<PlanVerifMachineEntete>> GetTousLesPlanAsync();

    Task<Guid> GetDefaultRefMoyenDetectionIdAsync();

    /// <summary>Retourne les familles de corps configurées pour une machine dans Machine_FamilleCorps.</summary>
    Task<List<RefFamilleCorp>> GetFamillesParMachineAsync(string machineCode);
    Task SyncMachineFamillesAsync(string machineCode, List<Guid> refFamilleCorpsIds);

    Task AddPlanAsync(PlanVerifMachineEntete plan);

    // Pour gérer les suppressions d'enfants dans le Tree Update
    void RemoveLigne(PlanVerifMachineLigne ligne);
    void RemoveEcheance(PlanVerifMachineEcheance echeance);
    void RemoveMatricePiece(PlanVerifMachineMatricePiece matricePiece);
}
