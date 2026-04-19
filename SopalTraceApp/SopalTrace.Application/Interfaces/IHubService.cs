using SopalTrace.Application.DTOs.QualityPlans.Hub;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IHubService
{
    Task<IReadOnlyList<HubModeleDto>> GetTousLesModelesAsync();
    Task<bool> ChangerStatutModeleAsync(string category, Guid id, string statut);
    Task<IReadOnlyList<HubPlanDto>> GetTousLesPlansAsync();
    Task<bool> ChangerStatutPlanAsync(string category, Guid id, string statut);
    Task<bool> SupprimerBrouillonPlanAsync(string category, Guid id);
}