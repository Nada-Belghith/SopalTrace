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

        public async Task<ExecEchantillonnageDto> InitPlanPourOfAsync(Guid execControleOfId, InitEchantillonnageRequest request)
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

            var postesActifs = await _operateurRepository.GetPostesForExecutionAsync(execControleOfId);
            int nbPostesTotal = request.NbPostes ?? (postesActifs.Any() ? postesActifs.Count() : 1);
            int effectifParPoste = (int)Math.Ceiling((double)effectif.Value / nbPostesTotal);

            var docStatut = new ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = execOf.Id,
                TypeDocument = "ECHANTILLONNAGE",
                PosteCode = request.PosteCode,
                DocId = plan.Id,
                EstDemarrageTermine = true,
                EstTermine = true,
                DateExecution = DateTime.Now,
                DateTermine = DateTime.Now,
                Equipe = request.Equipe,
                MachineCode = request.MachineCode,
                MatriculeOperateur = request.MatriculeOperateur
            };
            _operateurRepository.AddExecControleDocumentStatut(docStatut);

            // 2. Create ExecEchantillonnage linked to DocumentStatut
            var execEchantillonnage = new ExecEchantillonnage
            {
                Id = Guid.NewGuid(),
                ExecControleDocumentStatutId = docStatut.Id,
                TailleLot = tailleLot,
                NbPostesB = nbPostesTotal,
                LettreCode = lettreCode,
                EffectifEchantillonA = effectif.Value,
                EffectifParPosteAb = effectifParPoste,
                CritereAcceptationAc = critereAc,
            };

            if (request.InstrumentCodes != null && request.InstrumentCodes.Any())
            {
                var instruments = await _unitOfWork.DictionnaireQualiteRepository.GetInstrumentsByCodesAsync(request.InstrumentCodes);
                execEchantillonnage.CodeInstruments = instruments;
            }

            await _unitOfWork.ExecEchantillonnageRepository.AddAsync(execEchantillonnage);

            // Ajuster TempsPauseTotalMinutes pour que les occurrences d'échantillonnage
            // commencent à partir de MAINTENANT, et non pas depuis le début de l'OF (DateDebut).
            execOf.TempsPauseTotalMinutes = (DateTime.Now - execOf.DateDebut).TotalMinutes;

            await _unitOfWork.CommitAsync();

            return new ExecEchantillonnageDto
            {
                Id = execEchantillonnage.Id,
                ExecControleDocumentStatutId = execEchantillonnage.ExecControleDocumentStatutId,
                ExecControleOfId = docStatut.ExecControleOfId,
                TailleLot = execEchantillonnage.TailleLot,
                NbPostesB = execEchantillonnage.NbPostesB,
                LettreCode = execEchantillonnage.LettreCode,
                EffectifEchantillonA = execEchantillonnage.EffectifEchantillonA,
                EffectifParPosteAb = execEchantillonnage.EffectifParPosteAb,
                PosteCode = docStatut.PosteCode,
                CritereAcceptationAc = execEchantillonnage.CritereAcceptationAc,
                CritereRejetRe = execEchantillonnage.CritereRejetRe,
                InstrumentCodes = request.InstrumentCodes // Passing it back from request for now
            };
        }

        public async Task<ExecEchantillonnageDto?> GetPlanPourOfAsync(Guid execControleOfId, string? posteCode)
        {
            var exec = await _unitOfWork.ExecEchantillonnageRepository.GetByExecControleOfIdAndPosteAsync(execControleOfId, posteCode);
            if (exec == null) return null;

            var statuts = await _operateurRepository.GetDocumentStatutsAsync(execControleOfId);
            var statutEchantillonnage = statuts.FirstOrDefault(s => s.TypeDocument == "ECHANTILLONNAGE" && (posteCode == null || s.PosteCode == posteCode));

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

            var execOf = await _unitOfWork.ExecControleOfRepository.GetByIdWithOFAsync(execControleOfId);

            return new ExecEchantillonnageDto
            {
                Id = exec.Id,
                ExecControleDocumentStatutId = exec.ExecControleDocumentStatutId,
                ExecControleOfId = statutEchantillonnage?.ExecControleOfId ?? execControleOfId,
                TailleLot = exec.TailleLot,
                NbPostesB = exec.NbPostesB,
                LettreCode = exec.LettreCode,
                EffectifEchantillonA = exec.EffectifEchantillonA,
                EffectifParPosteAb = exec.EffectifParPosteAb,
                PosteCode = statutEchantillonnage?.PosteCode,
                CritereAcceptationAc = exec.CritereAcceptationAc,
                CritereRejetRe = exec.CritereRejetRe,
                NiveauControle = niveauControle,
                TypePlan = typePlan,
                ModeControle = modeControle,
                NqaValeur = nqaValeur,
                CodeArticle = execOf?.NumeroOfNavigation?.CodeArticle,
                Designation = execOf?.NumeroOfNavigation?.CodeArticleNavigation?.Designation,
                NumeroOf = execOf?.NumeroOf,
                Atelier = "ASS", // Or we can fetch it dynamically if available
                DateFabrication = execOf?.DateDebut,
                DateEchantillonnage = statutEchantillonnage?.DateTermine ?? statutEchantillonnage?.DateExecution ?? DateTime.Now,
                CodeMachine = statutEchantillonnage?.MachineCode ?? execOf?.MachineCode,
                EstTermine = statutEchantillonnage?.EstTermine ?? false,

                InstrumentCodes = exec.CodeInstruments?.Select(i => i.CodeInstrument).ToList() ?? new System.Collections.Generic.List<string>()
            };
        }

        public async Task<bool> SourcePlanExistsAsync(Guid execControleOfId)
        {
            var execOf = await _unitOfWork.ExecControleOfRepository.GetByIdWithOFAsync(execControleOfId);
            if (execOf == null) return false;
            
            // 1. Vérifier si on a DÉJÀ initialisé l'échantillonnage pour cet OF
            var existingExec = await _unitOfWork.ExecEchantillonnageRepository.GetByExecControleOfIdAndPosteAsync(execControleOfId, null);
            if (existingExec != null) return true;

            var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync();
            return plan != null;
        }

        public async Task<ExecEchantillonnageDto> UpdatePlanAsync(Guid id, ExecEchantillonnageDto request)
        {
            var exec = await _unitOfWork.ExecEchantillonnageRepository.GetByIdAsync(id);
            if (exec == null)
                throw new Exception("Exécution non trouvée.");

            exec.LettreCode = request.LettreCode;
            exec.EffectifEchantillonA = request.EffectifEchantillonA;
            exec.NbPostesB = request.NbPostesB;
            exec.EffectifParPosteAb = request.EffectifParPosteAb;
            exec.CritereAcceptationAc = request.CritereAcceptationAc;
            exec.CritereRejetRe = request.CritereRejetRe;
            if (request.InstrumentCodes != null)
            {
                exec.CodeInstruments.Clear();
                var instruments = await _unitOfWork.DictionnaireQualiteRepository.GetInstrumentsByCodesAsync(request.InstrumentCodes);
                foreach (var inst in instruments)
                {
                    exec.CodeInstruments.Add(inst);
                }
            }

            // Mettre à jour le poste et code machine si modifié (via ExecControleDocumentStatut)
            var statuts = await _operateurRepository.GetDocumentStatutsAsync(exec.ExecControleDocumentStatut.ExecControleOfId);
            var statut = statuts.FirstOrDefault(s => s.Id == exec.ExecControleDocumentStatutId);
            if (statut != null)
            {
                statut.PosteCode = request.PosteCode;
                statut.MachineCode = request.CodeMachine;
                statut.EstDemarrageTermine = true;
                statut.EstTermine = true;
                statut.DateTermine ??= DateTime.Now;
                if (request.DateEchantillonnage.HasValue)
                {
                    statut.DateExecution = request.DateEchantillonnage.Value;
                }
                // Save statut
                _operateurRepository.UpdateExecControleDocumentStatut(statut);
            }

            if (request.DateFabrication.HasValue)
            {
                var execOf = await _unitOfWork.ExecControleOfRepository.GetByIdWithOFAsync(exec.ExecControleDocumentStatut.ExecControleOfId);
                if (execOf != null)
                {
                    execOf.DateDebut = request.DateFabrication.Value;
                    // Note: Update ExecControleOF in repo if needed, assuming ChangeTracker handles it or explicit update needed
                }
            }

            await _unitOfWork.ExecEchantillonnageRepository.UpdateAsync(exec);
            await _unitOfWork.CommitAsync();

            return request;
        }
    }
}
