using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services
{
    public class PlanArchiverService : IPlanArchiverService
    {
        private readonly IPlanFabricationRepository _planFabRepository;
            private readonly IPlanPfRepository _planPfRepository;
            private readonly IPlanAssRepository _planAssRepository;
            private readonly IPlanNcRepository _planNcRepository;

            public PlanArchiverService(
                IPlanFabricationRepository planFabRepository,
                IPlanPfRepository planPfRepository,
                IPlanAssRepository planAssRepository,
                IPlanNcRepository planNcRepository)
            {
                _planFabRepository = planFabRepository;
                _planPfRepository = planPfRepository;
                _planAssRepository = planAssRepository;
                _planNcRepository = planNcRepository;
            }

        public async Task ArchivePlanFabricationActifAsync(string codeArticleSage, string? operationCode, string user)
        {
            var planActif = await _planFabRepository.GetPlanActifPourArticleEtOperationAsync(codeArticleSage, operationCode ?? "");
            if (planActif != null)
            {
                planActif.Statut = StatutsPlan.Archive;
                //planActif.ModifieLe = DateTime.UtcNow;
                //planActif.ModifiePar = user;
            }
        }

        public async Task ArchivePlansPfActifsAsync(string familleProduitFiniCode, string user)
        {
            var plansActifs = await _planPfRepository.GetActivePlansByFamilleAsync(familleProduitFiniCode);
            foreach (var p in plansActifs)
            {
                p.Statut = StatutsPlan.Archive;
                //p.ModifieLe = DateTime.UtcNow;
                //p.ModifiePar = user;
            }
        }

        public async Task ArchivePlanAssActifAsync(string operationCode, string? familleProduitFiniCode, string? natureComposantCode, string? posteCode, string user, Guid currentPlanId)
        {
            var ancienPlanActif = await _planAssRepository.GetPlanActifMaitreAsync(operationCode, familleProduitFiniCode, natureComposantCode, posteCode);

            if (ancienPlanActif is not null && ancienPlanActif.Id != currentPlanId)
            {
                ancienPlanActif.Statut = StatutsPlan.Archive;
                //ancienPlanActif.ModifiePar = user.Length > 20 ? user.Substring(0, 20) : user;
                //ancienPlanActif.ModifieLe = DateTime.UtcNow;
            }
        }

        public async Task ArchivePlanNcActifAsync(string posteCode, string user)
        {
            var planActif = await _planNcRepository.GetPlanActifAsync(posteCode);
            if (planActif != null)
            {
                planActif.Statut = StatutsPlan.Archive;
                //planActif.ModifiePar = user;
                //planActif.ModifieLe = DateTime.UtcNow;
            }
        }
    }
}
