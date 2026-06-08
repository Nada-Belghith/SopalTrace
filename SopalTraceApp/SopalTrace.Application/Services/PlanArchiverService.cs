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
            private readonly IControlePosteRepository _ControlePosteRepository;

            public PlanArchiverService(
                IPlanFabricationRepository planFabRepository,
                IPlanPfRepository planPfRepository,
                IPlanAssRepository planAssRepository,
                IControlePosteRepository ControlePosteRepository)
            {
                _planFabRepository = planFabRepository;
                _planPfRepository = planPfRepository;
                _planAssRepository = planAssRepository;
                _ControlePosteRepository = ControlePosteRepository;
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
            }
        }

        public async Task ArchivePlanPfActifParFormulaireAsync(Guid formulaireId, string user)
        {
            // Archiver uniquement le plan PF actif associé à ce formulaire précis
            var planActif = await _planPfRepository.GetPlanActifParFormulaireAsync(formulaireId);
            if (planActif != null)
            {
                planActif.Statut = StatutsPlan.Archive;
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

        public async Task ArchiveControlePosteActifAsync(string posteCode, string user)
        {
            var tousLesPlans = await _ControlePosteRepository.GetTousLesPlansAsync();
            var plansActifs = tousLesPlans.Where(p => p.PosteCode == posteCode && p.Statut == StatutsPlan.Actif);
            
            foreach (var plan in plansActifs)
            {
                plan.Statut = StatutsPlan.Archive;
            }
        }

        public async Task ArchiveControlePosteActifParFormulaireAsync(Guid formulaireId, string user)
        {
            // Archiver uniquement le plan actif associé à ce formulaire précis
            // => PAS71 et PAS71_SOUPAPE ont des formulaireId différents, donc pas de confusion
            var planActif = await _ControlePosteRepository.GetPlanActifParFormulaireAsync(formulaireId);
            if (planActif != null)
            {
                planActif.Statut = StatutsPlan.Archive;
            }
        }
    }
}
