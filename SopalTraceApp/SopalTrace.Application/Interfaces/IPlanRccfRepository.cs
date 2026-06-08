using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces
{
    public interface IPlanRccfRepository
    {
        Task<List<PlanResultatControleCfEntete>> GetTousLesPlansAsync();
        Task<PlanResultatControleCfEntete?> GetPlanAvecRelationsAsync(Guid id);
        Task<PlanResultatControleCfEntete?> GetPlanActifAsync(string posteCode, Guid formulaireId);
        Task AddPlanAsync(PlanResultatControleCfEntete plan);
        void RemoveLigne(PlanResultatControleCfLigne ligne);
        void RemoveSection(PlanResultatControleCfSection section);
        void AddSection(PlanResultatControleCfSection section);
        void AddLigne(PlanResultatControleCfLigne ligne);
        Task SaveChangesAsync();
    }
}

