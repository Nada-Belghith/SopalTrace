using SopalTrace.Domain.Constants;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Application.Mappers.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services
{
    /// <summary>
    /// Résultat contrôle en cours de fabrication (ENCF) : fiche de réponse de l'OF. Elle s'ouvre sur l'exécution en cours
    /// de l'OF (statut RESULTAT_CONTROLE_CF), reçoit à sa création son formulaire (famille, donc type de robinet, et
    /// poste ; code, version et modèle dédié), affiche les lignes du plan suivi par l'OF et enregistre les pièces types
    /// et résultats par tranche de cette exécution.
    /// </summary>
    public class ExecEncfService : IExecEncfService
    {
        private readonly IExecEncfRepository _repository;

        public ExecEncfService(IExecEncfRepository repository)
        {
            _repository = repository;
        }

        public async Task<ExecEncfDto?> GetExecEncfAsync(Guid execControleOfId)
        {
            var execution = await _repository.GetExecutionAsync(execControleOfId);
            if (execution == null) return null;
            var statut = StatutEncf(execution, posteCode: null);
            return ExecEncfMapper.ToDto(execution, statut, await PlanAsync(execution, statut));
        }

        /// <summary>Plan affiché : celui suivi par l'OF ; une fiche d'avant les fiches de réponse garde son document appliqué.</summary>
        private async Task<Guid?> PlanAsync(ExecControleOf execution, ExecControleDocumentStatut? statut) =>
            statut is { FormId: null, DocId: { } ancien } ? ancien : await _repository.TrouverPlanAsync(execution, statut?.PosteCode);

        public async Task<ExecEncfDto> GetOrCreateExecEncfByOfAsync(string numeroOf, string posteCode, string matriculeOperateur)
        {
            var execution = await _repository.GetExecutionOuverteAsync(numeroOf, posteCode)
                ?? throw new RegleMetierException("Démarrez l'OF sur ce poste avant de remplir le résultat contrôle en cours de fabrication.");

            var statut = StatutEncf(execution, posteCode);
            if (statut == null)
            {
                var formulaire = await _repository.TrouverFormulaireAsync(execution.NumeroOfNavigation?.CodeArticle, posteCode)
                    ?? throw new RessourceIntrouvableException("Aucun formulaire « résultats du contrôle en cours de fabrication » pour cet article et ce poste : "
                        + "le superviseur qualité doit le créer (page Formulaires).");
                var session = execution.ExecControleOfSessions.FirstOrDefault(s => s.DateFin == null);

                statut = new ExecControleDocumentStatut
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfId = execution.Id,
                    TypeDocument = TypesDocumentExecution.ResultatControleCf,
                    PosteCode = posteCode,
                    FormId = formulaire,
                    SessionId = session?.Id,
                    Equipe = session?.Equipe,
                    DateExecution = DateTime.Now,
                    MatriculeOperateur = matriculeOperateur
                };
                _repository.AddStatut(statut);
                await _repository.SaveChangesAsync();
            }

            return ExecEncfMapper.ToDto(execution, statut, await PlanAsync(execution, statut));
        }

        public async Task<ExecEncfDto?> SaveExecEncfAsync(ExecEncfDto dto, string matriculeOperateur)
        {
            if (!dto.Id.HasValue) throw new RegleMetierException("Exécution de l'OF non précisée.");
            var execution = await _repository.GetExecutionAsync(dto.Id.Value)
                ?? throw new RessourceIntrouvableException("Exécution de l'OF introuvable.");
            if (execution.Statut == StatutsExecution.Cloture)
                throw new EtatExecutionInvalideException("Cet OF est clôturé : le résultat contrôle en cours de fabrication ne peut plus être modifié.");

            var maintenant = DateTime.Now;
            EnregistrerPiecesTypes(execution, dto, matriculeOperateur, maintenant);
            EnregistrerTranches(execution, dto, matriculeOperateur);

            // Une pièce type conforme termine le réglage, comme lors de la réponse à une occurrence.
            if (execution.Statut == StatutsExecution.Reglage && execution.ExecPieceTypes.Any(p => p.Resultat == ResultatsControle.Conforme))
            {
                execution.Statut = StatutsExecution.EnCours;
                execution.SuspendedAt = null;
            }

            // Le statut de l'OF (clôture…) ne se change pas depuis ce document : il suit le parcours de l'OF.
            await _repository.SaveChangesAsync();
            return await GetExecEncfAsync(execution.Id);
        }

        /// <summary>Ajoute les nouvelles pièces types et corrige les existantes ; aucune preuve n'est supprimée.</summary>
        private void EnregistrerPiecesTypes(ExecControleOf execution, ExecEncfDto dto, string matricule, DateTime maintenant)
        {
            if (dto.PiecesTypes.Any(p => p.Resultat != ResultatsControle.Conforme && p.Resultat != ResultatsControle.NonConforme))
                throw new RegleMetierException("Chaque pièce type doit être déclarée conforme (C) ou non conforme (NC).");

            foreach (var recue in dto.PiecesTypes)
            {
                var existante = recue.Id.HasValue ? execution.ExecPieceTypes.FirstOrDefault(p => p.Id == recue.Id.Value) : null;
                if (existante == null)
                {
                    var nouvelle = new ExecPieceType
                    {
                        ExecControleOfid = execution.Id,
                        HeureValidation = recue.HeureValidation != default ? recue.HeureValidation : maintenant,
                        Resultat = recue.Resultat!,
                        Remarque = recue.Remarque,
                        MatriculeOperateur = matricule
                    };
                    execution.ExecPieceTypes.Add(nouvelle);
                    _repository.AddPieceType(nouvelle);
                }
                else if (existante.Resultat != recue.Resultat || existante.Remarque != recue.Remarque)
                {
                    existante.Resultat = recue.Resultat!;
                    existante.Remarque = recue.Remarque;
                    existante.MatriculeOperateur = matricule;
                }
            }
        }

        /// <summary>Ajoute ou corrige les résultats par tranche ; l'approbateur est l'utilisateur connecté.</summary>
        private void EnregistrerTranches(ExecControleOf execution, ExecEncfDto dto, string matricule)
        {
            foreach (var recue in dto.Tranches)
            {
                var existante = recue.Id.HasValue ? execution.ExecControleTranches.FirstOrDefault(t => t.Id == recue.Id.Value) : null;
                if (existante == null)
                {
                    _repository.AddTranche(new ExecControleTranche
                    {
                        ExecControleOfid = execution.Id,
                        TrancheHoraire = recue.TrancheHoraire ?? string.Empty,
                        HeureDebut = recue.HeureDebut,
                        HeureFin = recue.HeureFin,
                        ResultatFinal = recue.ResultatFinal ?? string.Empty,
                        DetailsNc = recue.DetailsNC,
                        ActionsCorrection = recue.ActionsCorrection,
                        MatriculeApprobateur = matricule
                    });
                }
                else if (existante.ResultatFinal != recue.ResultatFinal || existante.DetailsNc != recue.DetailsNC || existante.ActionsCorrection != recue.ActionsCorrection)
                {
                    existante.ResultatFinal = recue.ResultatFinal ?? existante.ResultatFinal;
                    existante.DetailsNc = recue.DetailsNC;
                    existante.ActionsCorrection = recue.ActionsCorrection;
                    existante.MatriculeApprobateur = matricule;
                }
            }
        }

        private static ExecControleDocumentStatut? StatutEncf(ExecControleOf execution, string? posteCode) =>
            execution.ExecControleDocumentStatuts
                .Where(s => s.TypeDocument == TypesDocumentExecution.ResultatControleCf && (posteCode == null || s.PosteCode == posteCode))
                .OrderByDescending(s => s.DateExecution)
                .FirstOrDefault();
    }
}
