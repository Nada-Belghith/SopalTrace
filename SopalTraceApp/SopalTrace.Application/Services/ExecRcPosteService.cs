using SopalTrace.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services
{
    /// <summary>
    /// Résultat contrôle au poste : un document par jour de production et par poste, commun aux équipes. Les
    /// équipes et leurs heures sont définies dans le document par le superviseur (<see cref="EquipesControlePoste"/>) ;
    /// seule l'équipe en production saisit, et seulement ses heures, sur le document du jour en cours, puis
    /// valide ses heures (obligatoire avant sa fin de poste).
    /// Seules les saisies de l'opérateur sont stockées : non-conformités par ligne et par heure (réponses
    /// COMPTAGE), pièces réalisées par heure, pièces rebutées par équipe ; les totaux sont recalculés à la lecture.
    /// </summary>
    public class ExecRcPosteService : IExecRcPosteService
    {
        private readonly IExecRcPosteRepository _repository;
        private readonly IFormulaireStructureService _formulaireStructureService;

        public ExecRcPosteService(IExecRcPosteRepository repository, IFormulaireStructureService formulaireStructureService)
        {
            _repository = repository;
            _formulaireStructureService = formulaireStructureService;
        }

        /// <summary>Document du jour de production en cours (créé à la première ouverture).</summary>
        public async Task<ExecRcPosteDto?> GetPlanPourOfAsync(Guid execControleOfId, string posteCode, string equipe)
        {
            var planActif = await _repository.GetPlanActifByPosteAsync(posteCode, execControleOfId);
            if (planActif == null) return null;

            var jour = await JourCourantAsync(planActif);
            var statut = await _repository.GetStatutWithDetailsAsync(execControleOfId, posteCode, equipe, jour);
            // Version exacte du document avec laquelle la journée a commencé, sinon le plan actif du poste.
            var plan = statut?.DocId is Guid docId ? await _repository.GetPlanByIdAsync(docId) ?? planActif : planActif;

            if (statut == null)
            {
                statut = new ExecControleDocumentStatut
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfId = execControleOfId,
                    PosteCode = posteCode,
                    DateExecution = jour,
                    TypeDocument = TypesDocumentExecution.ResultatControlePoste,
                    DocId = plan.Id,
                    EstTermine = false
                };
                _repository.AddStatut(statut);
                await _repository.SaveChangesAsync();
            }

            return await ConstruireDtoAsync(statut, plan);
        }

        public async Task<ExecRcPosteDto?> GetPlanByStatutIdAsync(Guid statutId)
        {
            var statut = await _repository.GetStatutByIdWithDetailsAsync(statutId)
                ?? throw new RessourceIntrouvableException("Statut introuvable.");

            var plan = statut.DocId is Guid docId ? await _repository.GetPlanByIdAsync(docId) : null;
            return plan == null ? null : await ConstruireDtoAsync(statut, plan);
        }

        /// <summary>
        /// Enregistre les heures de l'équipe en production : ses comptages, ses pièces réalisées et ses rebuts.
        /// Refusé hors de ses heures, sur un document terminé ou d'un autre jour, ou si une autre tablette a
        /// enregistré entre-temps pour la même équipe. Aucun total n'est accepté du client.
        /// </summary>
        public async Task<ExecRcPosteDto?> UpdatePlanAsync(Guid execControleDocumentStatutId, ExecRcPosteDto request, string matriculeOperateur)
        {
            var (statut, plan, equipe, tranches) = await ContexteSaisieAsync(execControleDocumentStatutId);

            var lignesDuPlan = plan.DocumentLignes.Select(l => l.Id).ToHashSet();
            if (request.Lignes.Any(l => !lignesDuPlan.Contains(l.DocLigneId)))
                throw new RegleMetierException("Une ou plusieurs lignes ne font pas partie du plan de contrôle du poste.");

            var horsEquipe = request.Lignes.SelectMany(l => l.Heures.Select(h => h.TrancheHoraire))
                .Concat(request.Reponses.Select(r => r.TrancheHoraire))
                .Where(t => !tranches.Contains(t))
                .Distinct()
                .ToList();
            if (horsEquipe.Count > 0)
                throw new RegleMetierException(
                    $"Heures hors de l'équipe {equipe.Code} ({string.Join(", ", horsEquipe)}) : elle saisit seulement de {equipe.Debut} h à {equipe.Fin} h.");

            // Deux tablettes de la même équipe : la seconde qui enregistre est prévenue au lieu d'écraser.
            var derniere = DerniereSaisie(statut, equipe.Code);
            if (derniere.HasValue && (!request.DerniereSaisieEquipe.HasValue ||
                                      Math.Abs((derniere.Value - request.DerniereSaisieEquipe.Value).TotalMilliseconds) > 1))
                throw new EtatExecutionInvalideException(
                    $"Les heures de l'équipe {equipe.Code} ont été modifiées depuis une autre tablette à {derniere.Value:HH:mm} : rechargez le document avant d'enregistrer.");

            var maintenant = DateTime.Now;
            EnregistrerComptages(statut, request, matriculeOperateur, equipe.Code, tranches, maintenant);
            EnregistrerPiecesRealisees(statut, request.Reponses, matriculeOperateur, equipe.Code, tranches, maintenant);
            if (request.NbPiecesRebuteesEquipe.HasValue)
                EnregistrerPiecesRebutees(statut, request.NbPiecesRebuteesEquipe.Value, matriculeOperateur, equipe.Code, maintenant);

            await _repository.SaveChangesAsync();
            return await GetPlanByStatutIdAsync(statut.Id);
        }

        /// <summary>
        /// L'équipe en production valide ses heures sur le document du jour (au moins une heure de pièces réalisées
        /// saisie) : sa tâche « contrôle au poste » est faite. Le document reste ouvert pour les équipes suivantes.
        /// </summary>
        public async Task<ExecRcPosteDto?> ValiderEquipeAsync(Guid execControleDocumentStatutId, string matriculeOperateur)
        {
            var (statut, _, equipe, tranches) = await ContexteSaisieAsync(execControleDocumentStatutId);
            if (!statut.ExecRcPosteReponses.Any(r => tranches.Contains(r.TrancheHoraire)))
                throw new RegleMetierException($"Saisissez au moins les pièces réalisées d'une heure de l'équipe {equipe.Code} avant de valider.");

            var session = await _repository.GetSessionEnCoursAsync(statut.ExecControleOfId);
            var posteCode = statut.PosteCode ?? "";
            var validations = await _repository.GetValidationsEquipesAsync(statut.ExecControleOfId, posteCode, statut.DateExecution);
            if (!validations.Any(v => v.SessionId == session!.Id))
            {
                _repository.AddStatut(new ExecControleDocumentStatut
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfId = statut.ExecControleOfId,
                    PosteCode = posteCode,
                    TypeDocument = TypesDocumentExecution.ClotureResultatControlePoste,
                    SessionId = session!.Id,
                    Equipe = session.Equipe,
                    // Jour de production du document validé ; heure de validation dans DateTermine.
                    DateExecution = statut.DateExecution,
                    DateTermine = DateTime.Now,
                    EstTermine = true,
                    MatriculeOperateur = matriculeOperateur
                });
                await _repository.SaveChangesAsync();
            }
            return await GetPlanByStatutIdAsync(statut.Id);
        }

        public async Task<bool> CloturerTousLesDocumentsAsync(Guid execControleOfId, string posteCode)
        {
            var statuts = await _repository.GetAllStatutsAsync(execControleOfId, posteCode);
            foreach (var statut in statuts)
            {
                statut.EstTermine = true;
            }

            // Marqueur de clôture de la catégorie pour le poste (sans document associé).
            _repository.AddStatut(new ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = execControleOfId,
                PosteCode = posteCode,
                TypeDocument = TypesDocumentExecution.ClotureResultatControlePoste,
                DateExecution = DateTime.Now,
                EstTermine = true
            });

            await _repository.SaveChangesAsync();
            return true;
        }

        /// <summary>Document d'un jour de production passé ou en cours (jamais futur).</summary>
        public async Task<ExecRcPosteDto?> CreerNouveauDocumentAsync(Guid execControleOfId, string posteCode, DateTime dateExecution, string equipe, string matriculeOperateur)
        {
            var plan = await _repository.GetPlanActifByPosteAsync(posteCode, execControleOfId)
                ?? throw new RessourceIntrouvableException("Aucun plan actif trouvé pour ce poste.");
            if (dateExecution.Date > await JourCourantAsync(plan))
                throw new RegleMetierException("Un document de contrôle au poste ne peut pas être créé pour un jour à venir.");

            var existing = await _repository.GetStatutWithDetailsAsync(execControleOfId, posteCode, equipe, dateExecution.Date);
            if (existing != null) return await GetPlanByStatutIdAsync(existing.Id);

            var nouveau = new ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = execControleOfId,
                PosteCode = posteCode,
                TypeDocument = TypesDocumentExecution.ResultatControlePoste,
                DocId = plan.Id,
                EstTermine = false,
                DateExecution = dateExecution.Date,
                MatriculeOperateur = matriculeOperateur
            };
            _repository.AddStatut(nouveau);
            await _repository.SaveChangesAsync();
            return await GetPlanByStatutIdAsync(nouveau.Id);
        }

        // ── Contexte de saisie ──────────────────────────────────────

        private async Task<IReadOnlyList<EquipePoste>> EquipesAsync(DocumentEntete plan) =>
            EquipesControlePoste.Depuis(await _repository.GetEquipesAsync(plan.FormulaireId));

        private async Task<DateTime> JourCourantAsync(DocumentEntete plan) =>
            EquipesControlePoste.JourProduction(DateTime.Now, EquipesControlePoste.HeureDebutJournee(await EquipesAsync(plan)));

        /// <summary>
        /// Saisie possible : document du jour de production en cours, non terminé, équipe en production définie
        /// dans le document. Renvoie ses tranches horaires.
        /// </summary>
        private async Task<(ExecControleDocumentStatut Statut, DocumentEntete Plan, EquipePoste Equipe, HashSet<string> Tranches)> ContexteSaisieAsync(Guid statutId)
        {
            var statut = await _repository.GetStatutByIdWithDetailsAsync(statutId)
                ?? throw new RessourceIntrouvableException("Statut document introuvable.");
            var plan = statut.DocId is Guid docId ? await _repository.GetPlanByIdAsync(docId) : null;
            if (plan == null) throw new RessourceIntrouvableException("Plan de contrôle au poste introuvable.");

            var equipes = await EquipesAsync(plan);
            var session = await _repository.GetSessionEnCoursAsync(statut.ExecControleOfId);
            var motif = MotifLectureSeule(statut, equipes, session?.Equipe);
            if (motif != null) throw new EtatExecutionInvalideException(motif);

            var equipe = EquipesControlePoste.Equipe(equipes, session!.Equipe)!;
            return (statut, plan, equipe, EquipesControlePoste.Tranches(equipe).ToHashSet());
        }

        private static string? MotifLectureSeule(ExecControleDocumentStatut statut, IReadOnlyList<EquipePoste> equipes, string? equipeEnProduction)
        {
            if (statut.EstTermine) return "Ce document est terminé : il ne peut plus être modifié.";
            var jourCourant = EquipesControlePoste.JourProduction(DateTime.Now, EquipesControlePoste.HeureDebutJournee(equipes));
            if (statut.DateExecution?.Date != jourCourant)
                return $"Document du {statut.DateExecution:dd/MM/yyyy} : seules les heures du jour de production en cours se saisissent.";
            if (equipeEnProduction == null) return "Aucune équipe ne produit sur cet OF : reprenez la production pour saisir vos heures.";
            if (EquipesControlePoste.Equipe(equipes, equipeEnProduction) == null)
                return $"L'équipe {equipeEnProduction} n'est pas définie dans ce document de contrôle au poste : demandez au superviseur de l'ajouter.";
            return null;
        }

        /// <summary>Dernière saisie de l'équipe sur le document (comptages, pièces réalisées, rebuts).</summary>
        private static DateTime? DerniereSaisie(ExecControleDocumentStatut statut, string equipe) =>
            statut.ExecControleLigneReponses.Where(c => c.ResultatType == ResultatsLigne.Comptage && c.Equipe == equipe).Select(c => (DateTime?)c.DateSaisie)
                .Concat(statut.ExecRcPosteReponses.Where(r => r.Equipe == equipe).Select(r => r.DateSaisie))
                .Concat(statut.ExecRcPosteBilans.Where(b => b.Equipe == equipe).Select(b => b.DateSaisie))
                .Max();

        // ── Enregistrement des saisies ──────────────────────────────────────

        private void EnregistrerComptages(ExecControleDocumentStatut statut, ExecRcPosteDto request, string matricule,
            string equipe, HashSet<string> tranches, DateTime maintenant)
        {
            var existants = statut.ExecControleLigneReponses
                .Where(r => r.ResultatType == ResultatsLigne.Comptage && tranches.Contains(r.TrancheHoraire!))
                .ToList();
            var recus = request.Lignes
                .SelectMany(l => l.Heures.Select(h => (Ligne: l.DocLigneId, h.TrancheHoraire, h.NbNcParHeure)))
                .GroupBy(r => (r.Ligne, r.TrancheHoraire))
                .ToDictionary(g => g.Key, g => g.Last().NbNcParHeure);

            foreach (var existant in existants.Where(e => !recus.ContainsKey((e.DocumentLigneId, e.TrancheHoraire!))))
                _repository.RemoveComptage(existant);

            foreach (var ((ligne, tranche), quantite) in recus)
            {
                var existant = existants.FirstOrDefault(e => e.DocumentLigneId == ligne && e.TrancheHoraire == tranche);
                if (existant == null)
                {
                    _repository.AddComptage(new ExecControleLigneReponse
                    {
                        ExecControleOfid = statut.ExecControleOfId,
                        ExecControleDocumentStatutId = statut.Id,
                        DocumentLigneId = ligne,
                        Contexte = ContextesReponse.ControlePoste,
                        TrancheHoraire = tranche,
                        ResultatType = ResultatsLigne.Comptage,
                        Quantite = quantite,
                        MatriculeOperateur = matricule,
                        Equipe = equipe,
                        DateSaisie = maintenant
                    });
                }
                else if (existant.Quantite != quantite)
                {
                    existant.Quantite = quantite;
                    existant.MatriculeOperateur = matricule;
                    existant.Equipe = equipe;
                    existant.DateSaisie = maintenant;
                }
            }
        }

        private void EnregistrerPiecesRealisees(ExecControleDocumentStatut statut, IEnumerable<ExecRcPosteReponseDto> reponses,
            string matricule, string equipe, HashSet<string> tranches, DateTime maintenant)
        {
            var recues = reponses.GroupBy(r => r.TrancheHoraire).ToDictionary(g => g.Key, g => g.Last().TotalRealiseHeure);

            foreach (var existante in statut.ExecRcPosteReponses.Where(r => tranches.Contains(r.TrancheHoraire) && !recues.ContainsKey(r.TrancheHoraire)).ToList())
                _repository.RemoveReponse(existante);

            foreach (var (tranche, realisees) in recues)
            {
                var existante = statut.ExecRcPosteReponses.FirstOrDefault(r => r.TrancheHoraire == tranche);
                if (existante == null)
                    _repository.AddReponse(new ExecRcPosteReponse
                    {
                        ExecControleDocumentStatutId = statut.Id, TrancheHoraire = tranche, TotalRealiseHeure = realisees,
                        Equipe = equipe, MatriculeOperateur = matricule, DateSaisie = maintenant
                    });
                else if (existante.TotalRealiseHeure != realisees)
                {
                    existante.TotalRealiseHeure = realisees;
                    existante.Equipe = equipe;
                    existante.MatriculeOperateur = matricule;
                    existante.DateSaisie = maintenant;
                }
            }
        }

        private void EnregistrerPiecesRebutees(ExecControleDocumentStatut statut, int rebutees, string matricule, string equipe, DateTime maintenant)
        {
            var bilan = statut.ExecRcPosteBilans.FirstOrDefault(b => b.Equipe == equipe);
            if (bilan == null)
                _repository.AddBilan(new ExecRcPosteBilan
                {
                    Id = Guid.NewGuid(), ExecControleDocumentStatutId = statut.Id, Equipe = equipe,
                    NbPiecesRebutees = rebutees, MatriculeOperateur = matricule, DateSaisie = maintenant
                });
            else if (bilan.NbPiecesRebutees != rebutees)
            {
                bilan.NbPiecesRebutees = rebutees;
                bilan.MatriculeOperateur = matricule;
                bilan.DateSaisie = maintenant;
            }
        }

        // ── Lecture : saisies + valeurs calculées ───────────────────────────

        private async Task<ExecRcPosteDto> ConstruireDtoAsync(ExecControleDocumentStatut statut, DocumentEntete plan)
        {
            var comptages = statut.ExecControleLigneReponses.Where(r => r.ResultatType == ResultatsLigne.Comptage).ToList();
            var equipesDefinies = await _repository.GetEquipesAsync(plan.FormulaireId);
            var equipes = EquipesControlePoste.Depuis(equipesDefinies);
            var session = await _repository.GetSessionEnCoursAsync(statut.ExecControleOfId);
            var motif = MotifLectureSeule(statut, equipes, session?.Equipe);
            var equipeSaisie = motif == null ? EquipesControlePoste.Equipe(equipes, session!.Equipe) : null;
            var rebutees = statut.ExecRcPosteBilans.Sum(b => b.NbPiecesRebutees);

            var dto = new ExecRcPosteDto
            {
                ExecControleDocumentStatutId = statut.Id,
                ExecControleOfId = statut.ExecControleOfId,
                PosteCode = statut.PosteCode ?? "",
                Equipe = session?.Equipe,
                PlanNom = plan.Nom,
                PlanRemarques = plan.Remarques ?? "",
                PlanLegendeMoyens = plan.LegendeMoyens ?? "",
                ConfigurationColonnesJson = await LireConfigurationColonnesAsync(plan),
                JourProduction = statut.DateExecution,
                Equipes = equipes.Select(e => new ExecRcPosteEquipeDto
                {
                    Code = e.Code, Nom = e.Nom, Debut = e.Debut, Fin = e.Fin, Tranches = EquipesControlePoste.Tranches(e)
                }).ToList(),
                EquipesParDefaut = equipesDefinies.Count == 0,
                EquipeSaisie = equipeSaisie?.Code,
                TranchesEquipe = equipeSaisie == null ? new List<string>() : EquipesControlePoste.Tranches(equipeSaisie),
                PeutSaisir = equipeSaisie != null,
                MotifLectureSeule = motif,
                DerniereSaisieEquipe = equipeSaisie == null ? null : DerniereSaisie(statut, equipeSaisie.Code),
                NbPiecesRebuteesEquipe = equipeSaisie == null ? null
                    : statut.ExecRcPosteBilans.FirstOrDefault(b => b.Equipe == equipeSaisie.Code)?.NbPiecesRebutees,
                RebutsParEquipe = statut.ExecRcPosteBilans
                    .Select(b => new ExecRcPosteRebutDto { Equipe = b.Equipe, NbPiecesRebutees = b.NbPiecesRebutees }).ToList(),
                Lignes = plan.DocumentLignes.OrderBy(l => l.OrdreAffiche).Select(ligne => new ExecRcPosteLigneDto
                {
                    DocLigneId = ligne.Id,
                    MachineCode = ligne.MachineCode ?? "-",
                    RisqueDefautId = ligne.RisqueDefautId,
                    LibelleAffiche = !string.IsNullOrEmpty(ligne.LibelleAffiche) ? ligne.LibelleAffiche : (ligne.RisqueDefaut?.LibelleDefaut ?? "Défaut inconnu"),
                    OrdreAffiche = ligne.OrdreAffiche,
                    Heures = comptages.Where(c => c.DocumentLigneId == ligne.Id).Select(c => new ExecRcPosteHeureDto
                    {
                        Id = c.Id,
                        DocLigneId = c.DocumentLigneId,
                        TrancheHoraire = c.TrancheHoraire ?? "",
                        NbNcParHeure = c.Quantite ?? 0,
                        MatriculeOp = c.MatriculeOperateur,
                        Equipe = c.Equipe
                    }).ToList()
                }).ToList(),
                Reponses = statut.ExecRcPosteReponses.Select(r => new ExecRcPosteReponseDto
                {
                    Id = r.Id,
                    TrancheHoraire = r.TrancheHoraire,
                    TotalNcHeure = comptages.Where(c => c.TrancheHoraire == r.TrancheHoraire).Sum(c => c.Quantite ?? 0),
                    TotalRealiseHeure = r.TotalRealiseHeure,
                    Equipe = r.Equipe
                }).ToList(),
                Bilan = CalculerBilan(comptages, statut.ExecRcPosteReponses, rebutees)
            };

            var posteCode = statut.PosteCode ?? "";
            dto.Validations = (await _repository.GetValidationsEquipesAsync(statut.ExecControleOfId, posteCode, statut.DateExecution))
                .Select(v => new ExecRcPosteValidationDto { Equipe = v.Equipe ?? "", DateValidation = v.DateTermine, MatriculeOperateur = v.MatriculeOperateur })
                .ToList();

            var validationsParJour = (await _repository.GetValidationsEquipesAsync(statut.ExecControleOfId, posteCode))
                .GroupBy(v => v.DateExecution?.Date)
                .ToDictionary(g => g.Key ?? DateTime.MinValue, g => g.Select(v => v.Equipe ?? "").Distinct().ToList());
            dto.Jours = (await _repository.GetAllStatutsAsync(statut.ExecControleOfId, posteCode))
                .Select(s => new ExecRcPosteJourDto
                {
                    StatutId = s.Id,
                    Jour = s.DateExecution,
                    EstTermine = s.EstTermine,
                    EquipesValidees = validationsParJour.GetValueOrDefault(s.DateExecution?.Date ?? DateTime.MinValue) ?? new List<string>()
                })
                .ToList();

            return dto;
        }

        /// <summary>Pièces testées = réalisées + défauts ; conformes = testées − rebutées (règles de l'écran opérateur).</summary>
        internal static ExecRcPosteBilanDto CalculerBilan(IEnumerable<ExecControleLigneReponse> comptages, IEnumerable<ExecRcPosteReponse> productions, int rebutees)
        {
            var totalDefauts = comptages.Sum(c => c.Quantite ?? 0);
            var piecesTestees = productions.Sum(p => p.TotalRealiseHeure) + totalDefauts;
            return new ExecRcPosteBilanDto
            {
                TotalDefauts = totalDefauts,
                TotalPiecesTestees = piecesTestees,
                TauxNc = CalculerTauxNc(totalDefauts, piecesTestees),
                NbPiecesRebutees = rebutees,
                NbPieceConforme = Math.Max(0, piecesTestees - rebutees)
            };
        }

        private async Task<string> LireConfigurationColonnesAsync(DocumentEntete plan)
        {
            if (plan.Formulaire == null) return "{}";
            var structure = await _formulaireStructureService.GetFormulaireActifParCodeReferenceAsync(plan.Formulaire.CodeReference);
            return structure?.ConfigurationStructureJson ?? "{}";
        }

        /// <summary>Taux de non-conformité en % (2 décimales) : calculé à la lecture, jamais stocké.</summary>
        private static double CalculerTauxNc(int totalDefauts, int totalPiecesTestees) =>
            totalPiecesTestees > 0 ? Math.Round(totalDefauts * 100.0 / totalPiecesTestees, 2) : 0;
    }
}
