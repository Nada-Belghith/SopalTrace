using System;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services
{
    public class ExecEchantillonnageService : IExecEchantillonnageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIso2859Service _iso2859Service;
        private readonly IOperateurRepository _operateurRepository;

        public ExecEchantillonnageService(IUnitOfWork unitOfWork, IIso2859Service iso2859Service, IOperateurRepository operateurRepository)
        {
            _unitOfWork = unitOfWork;
            _iso2859Service = iso2859Service;
            _operateurRepository = operateurRepository;
        }

        public async Task<ExecEchantillonnageDto> InitPlanPourOfAsync(Guid execControleOfId, int nbPostes)
        {
            var execOf = await _unitOfWork.ExecControleOfRepository.GetByIdWithOFAsync(execControleOfId);

            if (execOf == null)
                throw new Exception("Exécution non trouvée.");

            var tailleLot = Convert.ToInt32(execOf.NumeroOfNavigation.QuantiteLancee);
            if (tailleLot <= 0)
                throw new Exception("L'OF n'a pas de quantité valide.");

            var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync();

            if (plan == null)
                throw new Exception("Plan d'échantillonnage source introuvable.");

            // Get LettreCode
            var lettreCode = await _iso2859Service.GetLettreCodeAsync(tailleLot, plan.NiveauControle);
            if (string.IsNullOrEmpty(lettreCode))
                throw new Exception($"Impossible de déterminer la Lettre Code pour le lot de {tailleLot} et niveau {plan.NiveauControle}.");

            var nqaValeur = plan.Nqa?.ValeurNqa ?? 0.01; // Fallback if Nqa is null
            if (plan.Nqa == null)
            {
                 // We might need a repository for NQA or modify GetByIdAsync
            }

            var effectif = await _iso2859Service.GetEffectifEchantillonAsync(lettreCode, nqaValeur);
            if (effectif == null)
                throw new Exception($"Impossible de déterminer l'effectif pour Lettre {lettreCode} et NQA {nqaValeur}.");

            // Ac and Re criteria
            int critereAc = plan.CritereAcceptationAc;
            int critereRe = plan.CritereRejetRe;

            int? effectifParPoste = nbPostes > 0 ? (int?)Math.Ceiling((double)effectif.Value / nbPostes) : null;

            var execEchantillonnage = new ExecEchantillonnage
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = execControleOfId,
                TailleLot = tailleLot,
                NbPostesB = nbPostes,
                LettreCode = lettreCode,
                EffectifEchantillonA = effectif.Value,
                EffectifParPosteAb = effectifParPoste,
                CritereAcceptationAc = critereAc,
                CritereRejetRe = critereRe
            };

            await _unitOfWork.ExecEchantillonnageRepository.AddAsync(execEchantillonnage);
            
            // Add to DocumentStatut so it shows up in the dashboard as created
            _operateurRepository.AddExecControleDocumentStatut(new ExecControleDocumentStatut
            {
                ExecControleOfId = execOf.Id,
                TypeDocument = "ECHANTILLONNAGE",
                DocId = plan.Id,
                EstTermine = false
            });

            await _unitOfWork.CommitAsync();

            return new ExecEchantillonnageDto
            {
                Id = execEchantillonnage.Id,
                ExecControleOfId = execEchantillonnage.ExecControleOfId,
                TailleLot = execEchantillonnage.TailleLot,
                NbPostesB = execEchantillonnage.NbPostesB,
                LettreCode = execEchantillonnage.LettreCode,
                EffectifEchantillonA = execEchantillonnage.EffectifEchantillonA,
                EffectifParPosteAb = execEchantillonnage.EffectifParPosteAb,
                CritereAcceptationAc = execEchantillonnage.CritereAcceptationAc,
                CritereRejetRe = execEchantillonnage.CritereRejetRe
            };
        }

        public async Task<ExecEchantillonnageDto?> GetPlanPourOfAsync(Guid execControleOfId)
        {
            var exec = await _unitOfWork.ExecEchantillonnageRepository.GetByExecControleOfIdAsync(execControleOfId);
            if (exec == null) return null;

            var statuts = await _operateurRepository.GetDocumentStatutsAsync(execControleOfId);
            var statutEchantillonnage = statuts.FirstOrDefault(s => s.TypeDocument == "ECHANTILLONNAGE");

            string? niveauControle = null;
            string? typePlan = null;
            string? modeControle = null;
            double? nqaValeur = null;

            if (statutEchantillonnage?.DocId != null)
            {
                var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetByIdAsync(statutEchantillonnage.DocId.Value);
                if (plan != null)
                {
                    niveauControle = plan.NiveauControle;
                    typePlan = plan.TypePlan;
                    modeControle = plan.ModeControle;
                    nqaValeur = plan.Nqa?.ValeurNqa;
                }
            }

            return new ExecEchantillonnageDto
            {
                Id = exec.Id,
                ExecControleOfId = exec.ExecControleOfId,
                TailleLot = exec.TailleLot,
                NbPostesB = exec.NbPostesB,
                LettreCode = exec.LettreCode,
                EffectifEchantillonA = exec.EffectifEchantillonA,
                EffectifParPosteAb = exec.EffectifParPosteAb,
                CritereAcceptationAc = exec.CritereAcceptationAc,
                CritereRejetRe = exec.CritereRejetRe,
                NiveauControle = niveauControle,
                TypePlan = typePlan,
                ModeControle = modeControle,
                NqaValeur = nqaValeur
            };
        }

        public async Task<bool> SourcePlanExistsAsync(Guid execControleOfId)
        {
            var execOf = await _unitOfWork.ExecControleOfRepository.GetByIdWithOFAsync(execControleOfId);
            if (execOf == null) return false;
            
            // 1. Vérifier si on a DÉJÀ initialisé l'échantillonnage pour cet OF
            var existingExec = await _unitOfWork.ExecEchantillonnageRepository.GetByExecControleOfIdAsync(execControleOfId);
            if (existingExec != null) return true;

            // 2. Sinon, vérifier s'il y a un plan actif
            var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync();
            return plan != null;
        }
    }
}
