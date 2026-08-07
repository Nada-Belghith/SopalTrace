using System;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services
{
    public class ExecRcPosteService : IExecRcPosteService
    {
        private readonly IExecRcPosteRepository _repository;
        private readonly IFormulaireStructureService _formulaireStructureService;

        public ExecRcPosteService(IExecRcPosteRepository repository, IFormulaireStructureService formulaireStructureService)
        {
            _repository = repository;
            _formulaireStructureService = formulaireStructureService;
        }

        public async Task<ExecRcPosteDto> GetPlanPourOfAsync(Guid execControleOfId, string posteCode, string equipe)
        {
            var today = DateTime.Now.Date;

            // Find the Statut row
            var statut = await _repository.GetStatutWithDetailsAsync(execControleOfId, posteCode, equipe, today);

            SopalTrace.Domain.Entities.DocumentEntete plan = null;

            if (statut != null && statut.DocId.HasValue)
            {
                // Fetch the exact document version the operator started with
                plan = await _repository.GetPlanByIdAsync(statut.DocId.Value);

                // Si le statut pointait par erreur vers le document RESULTAT_CF, réinitialiser pour trouver le bon plan RESULTAT_CONTROLE_POSTE
                if (plan != null && plan.TypeDocumentCode == "RESULTAT_CF")
                {
                    plan = null;
                }
            }

            if (plan == null)
            {
                // Fetch current active plan
                plan = await _repository.GetPlanActifByPosteAsync(posteCode, execControleOfId);
            }

            if (plan == null) return null;

            if (statut == null) 
            {
                statut = new SopalTrace.Domain.Entities.ExecControleDocumentStatut
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfId = execControleOfId,
                    PosteCode = posteCode,
                    Equipe = equipe,
                    DateExecution = today,
                    TypeDocument = "RESULTAT_CONTROLE_POSTE",
                    DocId = plan.Id,
                    EstTermine = false,
                    ExecRcPosteHeures = new List<SopalTrace.Domain.Entities.ExecRcPosteHeure>(),
                    ExecRcPosteReponses = new List<SopalTrace.Domain.Entities.ExecRcPosteReponse>(),
                    ExecRcPosteBilans = new List<SopalTrace.Domain.Entities.ExecRcPosteBilan>()
                };
                
                _repository.AddStatut(statut);
                await _repository.SaveChangesAsync();
            }
            else if (statut.DocId != plan.Id)
            {
                statut.DocId = plan.Id;
                await _repository.SaveChangesAsync();
            }
            
            string configJson = "{}";
            if (plan.Formulaire != null)
            {
                var structDto = await _formulaireStructureService.GetFormulaireActifParCodeReferenceAsync(plan.Formulaire.CodeReference);
                if (structDto != null)
                {
                    configJson = structDto.ConfigurationStructureJson;
                }
            }

            var dto = new ExecRcPosteDto
            {
                ExecControleDocumentStatutId = statut.Id,
                ExecControleOfId = statut.ExecControleOfId,
                PosteCode = statut.PosteCode,
                Equipe = statut.Equipe,
                PlanNom = plan.Nom,
                PlanRemarques = plan.Remarques,
                PlanLegendeMoyens = plan.LegendeMoyens,
                ConfigurationColonnesJson = configJson ?? "{}"
            };

            foreach (var ligne in plan.DocumentLignes.OrderBy(l => l.OrdreAffiche))
            {
                var ligneDto = new ExecRcPosteLigneDto
                {
                    DocLigneId = ligne.Id,
                    MachineCodeCtrlPoste = ligne.MachineCodeCtrlPoste ?? ligne.MachineCode ?? "-",
                    RisqueDefautId = ligne.RisqueDefautId,
                    LibelleAffiche = !string.IsNullOrEmpty(ligne.LibelleAffiche) ? ligne.LibelleAffiche : (ligne.RisqueDefaut?.LibelleDefaut ?? "Défaut inconnu"),
                    OrdreAffiche = ligne.OrdreAffiche
                };

                // Attach existing answers
                ligneDto.Heures = statut.ExecRcPosteHeures
                .Where(h => h.DocLigneId == ligne.Id)
                .Select(h => new ExecRcPosteHeureDto
                {
                    Id = h.Id,
                    DocLigneId = h.DocLigneId,
                    TrancheHoraire = h.TrancheHoraire,
                    NbNcParHeure = h.NbNcParHeure,
                    MatriculeOp = h.MatriculeOp
                }).ToList();

                dto.Lignes.Add(ligneDto);
            }

            dto.Reponses = statut.ExecRcPosteReponses.Select(r => new ExecRcPosteReponseDto
            {
                Id = r.Id,
                TrancheHoraire = r.TrancheHoraire,
                TotalNcHeure = r.TotalNcHeure,
                TotalRealiseHeure = r.TotalRealiseHeure
            }).ToList();

            var bilan = statut.ExecRcPosteBilans.FirstOrDefault();
            if (bilan != null)
            {
                dto.Bilan = new ExecRcPosteBilanDto
                {
                    Id = bilan.Id,
                    TotalDefauts = bilan.TotalDefauts,
                    TotalPiecesTestees = bilan.TotalPiecesTestees,
                    TauxNc = bilan.TauxNc,
                    NbPiecesRebutees = bilan.NbPiecesRebutees,
                    NbPieceConforme = bilan.NbPieceConforme
                };
            }
            else
            {
                dto.Bilan = new ExecRcPosteBilanDto(); // Empty
            }
            var allStatutsRaw = await _repository.GetAllStatutsAsync(execControleOfId, posteCode);
            var allStatuts = allStatutsRaw.Where(s => s.TypeDocument != "CLOTURE_RC_POSTE").ToList();
            dto.AvailableSessions = allStatuts.Select(s => new ExecRcPosteSessionInfoDto
            {
                StatutId = s.Id,
                DateExecution = s.DateExecution,
                Equipe = s.Equipe,
                EstTermine = s.EstTermine
            }).ToList();

            return dto;
        }
        public async Task<ExecRcPosteDto> GetPlanByStatutIdAsync(Guid statutId)
        {
            var statut = await _repository.GetStatutByIdWithDetailsAsync(statutId);
            if (statut == null) throw new Exception("Statut introuvable.");

            SopalTrace.Domain.Entities.DocumentEntete plan = null;
            if (statut.DocId.HasValue)
            {
                plan = await _repository.GetPlanByIdAsync(statut.DocId.Value);
            }
            if (plan == null) return null;
            string configJson = "{}";
            if (plan.Formulaire != null)
            {
                var structDto = await _formulaireStructureService.GetFormulaireActifParCodeReferenceAsync(plan.Formulaire.CodeReference);
                if (structDto != null)
                {
                    configJson = structDto.ConfigurationStructureJson;
                }
            }

            var dto = new ExecRcPosteDto
            {
                ExecControleDocumentStatutId = statut.Id,
                ExecControleOfId = statut.ExecControleOfId,
                PosteCode = statut.PosteCode ?? "",
                Equipe = statut.Equipe,
                PlanNom = plan.Nom,
                PlanRemarques = plan.Remarques ?? "",
                PlanLegendeMoyens = plan.LegendeMoyens ?? "",
                ConfigurationColonnesJson = configJson ?? "{}"
            };

            foreach (var ligne in plan.DocumentLignes.OrderBy(l => l.OrdreAffiche))
            {
                var ligneDto = new ExecRcPosteLigneDto
                {
                    DocLigneId = ligne.Id,
                    MachineCodeCtrlPoste = ligne.MachineCodeCtrlPoste ?? ligne.MachineCode ?? "-",
                    RisqueDefautId = ligne.RisqueDefautId,
                    LibelleAffiche = !string.IsNullOrEmpty(ligne.LibelleAffiche) ? ligne.LibelleAffiche : (ligne.RisqueDefaut?.LibelleDefaut ?? "Défaut inconnu"),
                    OrdreAffiche = ligne.OrdreAffiche
                };

                // Attach existing answers
                ligneDto.Heures = statut.ExecRcPosteHeures
                    .Where(h => h.DocLigneId == ligne.Id)
                    .Select(h => new ExecRcPosteHeureDto
                    {
                        Id = h.Id,
                        DocLigneId = h.DocLigneId,
                        TrancheHoraire = h.TrancheHoraire,
                        NbNcParHeure = h.NbNcParHeure,
                        MatriculeOp = h.MatriculeOp
                    }).ToList();

                dto.Lignes.Add(ligneDto);
            }

            dto.Reponses = statut.ExecRcPosteReponses.Select(r => new ExecRcPosteReponseDto
            {
                Id = r.Id,
                TrancheHoraire = r.TrancheHoraire,
                TotalNcHeure = r.TotalNcHeure,
                TotalRealiseHeure = r.TotalRealiseHeure
            }).ToList();

            var bilan = statut.ExecRcPosteBilans.FirstOrDefault();
            if (bilan != null)
            {
                dto.Bilan = new ExecRcPosteBilanDto
                {
                    Id = bilan.Id,
                    TotalDefauts = bilan.TotalDefauts,
                    TotalPiecesTestees = bilan.TotalPiecesTestees,
                    TauxNc = bilan.TauxNc,
                    NbPiecesRebutees = bilan.NbPiecesRebutees,
                    NbPieceConforme = bilan.NbPieceConforme
                };
            }
            else
            {
                dto.Bilan = new ExecRcPosteBilanDto();
            }

            var allStatutsRaw = await _repository.GetAllStatutsAsync(statut.ExecControleOfId, statut.PosteCode ?? "");
            var allStatuts = allStatutsRaw.Where(s => s.TypeDocument != "CLOTURE_RC_POSTE").ToList();
            dto.AvailableSessions = allStatuts.Select(s => new ExecRcPosteSessionInfoDto
            {
                StatutId = s.Id,
                DateExecution = s.DateExecution,
                Equipe = s.Equipe,
                EstTermine = s.EstTermine
            }).ToList();

            return dto;
        }

        public async Task<ExecRcPosteDto> UpdatePlanAsync(Guid execControleDocumentStatutId, ExecRcPosteDto request, string matriculeOperateur)
        {
            var statut = await _repository.GetStatutByIdWithDetailsAsync(execControleDocumentStatutId);

            if (statut == null) throw new Exception("Statut document introuvable.");
            // Le client souhaite pouvoir modifier un document même s'il est clôturé.

            var now = DateTime.Now;

            // Clear old values to rewrite completely for simplicity (or update existing)
            _repository.RemoveHeures(statut.ExecRcPosteHeures);
            _repository.RemoveReponses(statut.ExecRcPosteReponses);
            _repository.RemoveBilans(statut.ExecRcPosteBilans);

            // Recreate Heures
            foreach (var ligne in request.Lignes)
            {
                foreach (var h in ligne.Heures)
                {
                    _repository.AddHeure(new ExecRcPosteHeure
                    {
                        ExecControleDocumentStatutId = statut.Id,
                        DocLigneId = ligne.DocLigneId,
                        DateExecution = now,
                        MatriculeOp = matriculeOperateur,
                        Equipe = statut.Equipe,
                        TrancheHoraire = h.TrancheHoraire,
                        NbNcParHeure = h.NbNcParHeure
                    });
                }
            }

            // Recreate Reponses
            foreach (var r in request.Reponses)
            {
                _repository.AddReponse(new ExecRcPosteReponse
                {
                    ExecControleDocumentStatutId = statut.Id,
                    TrancheHoraire = r.TrancheHoraire,
                    TotalNcHeure = r.TotalNcHeure,
                    TotalRealiseHeure = r.TotalRealiseHeure
                });
            }

            // Recreate Bilan
            if (request.Bilan != null)
            {
                _repository.AddBilan(new ExecRcPosteBilan
                {
                    ExecControleDocumentStatutId = statut.Id,
                    TotalDefauts = request.Bilan.TotalDefauts,
                    TotalPiecesTestees = request.Bilan.TotalPiecesTestees,
                    TauxNc = request.Bilan.TauxNc,
                    NbPiecesRebutees = request.Bilan.NbPiecesRebutees,
                    NbPieceConforme = request.Bilan.NbPieceConforme
                });
            }

            await _repository.SaveChangesAsync();

            return await GetPlanPourOfAsync(statut.ExecControleOfId, statut.PosteCode ?? "", statut.Equipe ?? "");
        }

        public async Task<bool> CloturerTousLesDocumentsAsync(Guid execControleOfId, string posteCode)
        {
            var statuts = await _repository.GetAllStatutsAsync(execControleOfId, posteCode);
            bool updated = false;
            foreach (var statut in statuts)
            {
                if (!statut.EstTermine)
                {
                    statut.EstTermine = true;
                    updated = true;
                }
            }
            
            // Ajouter un document factice pour marquer la catégorie complète comme clôturée
            var clotureDoc = new ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = execControleOfId,
                PosteCode = posteCode,
                TypeDocument = "CLOTURE_RC_POSTE",
                DateExecution = DateTime.Now,
                EstTermine = true
            };
            _repository.AddStatut(clotureDoc);
            
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<ExecRcPosteDto> CreerNouveauDocumentAsync(Guid execControleOfId, string posteCode, DateTime dateExecution, string equipe, string matriculeOperateur)
        {
            var plan = await _repository.GetPlanActifByPosteAsync(posteCode, execControleOfId);
            if (plan == null)
            {
                throw new Exception("Aucun plan actif trouvé pour ce poste.");
            }

            var existing = await _repository.GetStatutWithDetailsAsync(execControleOfId, posteCode, equipe, dateExecution.Date);
            if (existing != null)
            {
                return await GetPlanByStatutIdAsync(existing.Id);
            }

            var newStatut = new SopalTrace.Domain.Entities.ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = execControleOfId,
                PosteCode = posteCode,
                TypeDocument = "RESULTAT_CONTROLE_POSTE",
                DocId = plan.Id,
                EstTermine = false,
                Equipe = equipe,
                DateExecution = dateExecution.Date,
                MatriculeOperateur = matriculeOperateur
            };

            _repository.AddStatut(newStatut);
            await _repository.SaveChangesAsync();

            return await GetPlanByStatutIdAsync(newStatut.Id);
        }
    }
}
