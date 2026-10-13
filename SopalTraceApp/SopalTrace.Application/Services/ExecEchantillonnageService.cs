using SopalTrace.Domain.Constants;
using System;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services
{
    public class ExecEchantillonnageService : IExecEchantillonnageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIso2859Service _iso2859Service;
        private readonly IOperateurRepository _operateurRepository;
        private readonly IDemarragePosteRepository _demarrages;

        public ExecEchantillonnageService(IUnitOfWork unitOfWork, IIso2859Service iso2859Service, IOperateurRepository operateurRepository,
            IDemarragePosteRepository demarrages)
        {
            _unitOfWork = unitOfWork;
            _iso2859Service = iso2859Service;
            _operateurRepository = operateurRepository;
            _demarrages = demarrages;
        }

        /// <summary>
        /// Fiche d'échantillonnage d'un poste de l'OF (une par poste, chaque poste vit comme si l'OF n'en avait
        /// qu'un) : remplie par l'équipe qui a démarré le poste, les équipes suivantes la trouvent faite.
        /// </summary>
        public async Task<ExecEchantillonnageDto> InitPlanPourOfAsync(Guid execControleOfId, InitEchantillonnageRequest request)
        {
            var execOf = await _unitOfWork.ExecControleOfRepository.GetByIdWithOFAsync(execControleOfId);

            if (execOf == null)
                throw new RessourceIntrouvableException("Exécution non trouvée.");
            if (execOf.Statut == StatutsExecution.Cloture)
                throw new EtatExecutionInvalideException("Cet OF est clôturé.");

            var posteCode = request.PosteCode?.Trim();
            if (string.IsNullOrEmpty(posteCode))
                throw new RegleMetierException("Le poste de la fiche d'échantillonnage est obligatoire.");
            if (!(await _operateurRepository.GetPostesForExecutionAsync(execControleOfId)).Contains(posteCode))
                throw new RegleMetierException($"Le poste {posteCode} n'est pas affecté à cet OF.");
            var session = await _demarrages.GetSessionAsync(execControleOfId, posteCode);
            if (session == null || session.Statut != StatutsSession.EnCours)
                throw new EtatExecutionInvalideException($"Aucune équipe ne produit sur le poste {posteCode} : reprenez la production pour remplir la fiche.");
            if (!await _demarrages.EstEnCoursAsync(session.Id, posteCode))
                throw new EtatExecutionInvalideException(ExecPlanAssemblageService.MessagePosteNonDemarre(posteCode, session.Equipe));
            if (await _unitOfWork.ExecEchantillonnageRepository.GetByExecControleOfIdAndPosteAsync(execControleOfId, posteCode) != null)
                throw new EtatExecutionInvalideException($"La fiche d'échantillonnage du poste {posteCode} est déjà remplie pour cet OF.");
            request.PosteCode = posteCode;

            var tailleLot = Convert.ToInt32(execOf.NumeroOfNavigation.QuantiteLancee);
            if (tailleLot <= 0)
                throw new RegleMetierException("L'OF n'a pas de quantité valide.");

            var plan = (await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync())?.SamplingPlan
                ?? throw new RessourceIntrouvableException("Plan d'échantillonnage source introuvable.");

            // Get LettreCode
            var lettreCode = await _iso2859Service.GetLettreCodeAsync(tailleLot, plan.InspectionLevelCode);
            if (string.IsNullOrEmpty(lettreCode))
                throw new RegleMetierException($"Impossible de déterminer la Lettre Code pour le lot de {tailleLot} et niveau {plan.InspectionLevelCode}.");


            var effectif = await _iso2859Service.GetEffectifEchantillonAsync(lettreCode);
            if (effectif == null)
                throw new RegleMetierException($"Impossible de déterminer l'effectif pour la lettre code {lettreCode}.");

            // Ac and Re criteria
            int critereAc = plan.AcceptanceNumber;
            int critereRe = plan.RejectionNumber;

            // Le poste est traité comme le seul de l'OF, sauf si l'opérateur répartit l'effectif sur plusieurs postes.
            int nbPostesTotal = Math.Max(1, request.NbPostes ?? 1);
            int effectifParPoste = (int)Math.Ceiling((double)effectif.Value / nbPostesTotal);

            var activeSession = session;

            var docStatut = new ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = execOf.Id,
                TypeDocument = TypesDocumentExecution.Echantillonnage,
                PosteCode = request.PosteCode,
                DocId = plan.DocumentId,
                SessionId = activeSession?.Id,
                EstDemarrageTermine = true,
                EstPauseTermine = true,
                EstTermine = true,
                DateExecution = DateTime.Now,
                DateTermine = DateTime.Now,
                Equipe = Equipes.NormaliserOuNull(request.Equipe),
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
                CritereRejetRe = critereRe,
            };

            if (request.InstrumentCodes != null && request.InstrumentCodes.Any())
            {
                var instruments = await _unitOfWork.DictionnaireQualiteRepository.GetInstrumentsByCodesAsync(request.InstrumentCodes);
                execEchantillonnage.CodeInstruments = instruments;
            }

            await _unitOfWork.ExecEchantillonnageRepository.AddAsync(execEchantillonnage);

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
                InstrumentCodes = request.InstrumentCodes ?? new()
            };
        }

        public async Task<ExecEchantillonnageDto?> GetPlanPourOfAsync(Guid execControleOfId, string? posteCode)
        {
            var exec = await _unitOfWork.ExecEchantillonnageRepository.GetByExecControleOfIdAndPosteAsync(execControleOfId, posteCode);
            if (exec == null) return null;

            // Statut de la fiche (quelle que soit l'équipe qui l'a remplie).
            var statutEchantillonnage = exec.ExecControleDocumentStatut;

            string? niveauControle = null;
            string? typePlan = null;
            string? modeControle = null;
            decimal? nqaValeur = null;

            if (statutEchantillonnage?.DocId != null)
            {
                var plan = (await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetByIdAsync(statutEchantillonnage.DocId.Value))?.SamplingPlan;
                if (plan != null)
                {
                    niveauControle = plan.InspectionLevelCode;
                    typePlan = plan.SamplingType;
                    modeControle = plan.InspectionSeverity;
                    nqaValeur = plan.AcceptableQualityLevel?.ValeurNqa;
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

    }
}
