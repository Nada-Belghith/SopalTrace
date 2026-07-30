using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Tracabilite;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services
{
    public class TracabiliteService : ITracabiliteService
    {
        private readonly ITracabiliteRepository _tracabiliteRepository;

        public TracabiliteService(ITracabiliteRepository tracabiliteRepository)
        {
            _tracabiliteRepository = tracabiliteRepository;
        }

        public async Task<RegistreTracabiliteDto> GetRegistreTracabiliteAsync(Guid execControleOfId)
        {
            var execOf = await _tracabiliteRepository.GetExecOfAsync(execControleOfId);

            if (execOf == null)
            {
                throw new Exception("Exécution OF introuvable.");
            }

            var numeroOf = execOf.NumeroOf;

            // Fetch previous entries
            var historique = await _tracabiliteRepository.GetHistoriqueAsync(execControleOfId);

            // Fetch components scanned by storekeeper
            var preps = await _tracabiliteRepository.GetPreparationsMagasinAsync(numeroOf);

            var lotsByArticle = new Dictionary<string, ComposantDisponibleDto>();

            foreach (var prep in preps)
            {
                foreach (var lot in prep.MagPreparationOfLots)
                {
                    var designation = lot.CodeArticleNavigation?.Designation ?? lot.CodeArticle;
                    
                    if (!lotsByArticle.ContainsKey(lot.CodeArticle))
                    {
                        lotsByArticle[lot.CodeArticle] = new ComposantDisponibleDto
                        {
                            CodeArticle = lot.CodeArticle,
                            DesignationComposant = designation,
                            LotsDisponibles = new List<string>()
                        };
                    }

                    if (!string.IsNullOrEmpty(lot.NumeroLotScanne) && !lotsByArticle[lot.CodeArticle].LotsDisponibles.Contains(lot.NumeroLotScanne))
                    {
                        lotsByArticle[lot.CodeArticle].LotsDisponibles.Add(lot.NumeroLotScanne);
                    }
                }
            }

            var statut = await _tracabiliteRepository.GetStatutTracabiliteAsync(execControleOfId);

            var dto = new RegistreTracabiliteDto
            {
                EstTermine = statut?.EstTermine ?? false,
                ComposantsDisponibles = lotsByArticle.Values.ToList(),
                LignesHistorique = historique.Select(h => new RegistreTracabiliteRowDto
                {
                    Id = h.Id,
                    DateHeure = h.DateHeure,
                    Version = h.Version,
                    Corps = h.Corps,
                    Volant = h.Volant,
                    Composants = h.ExecRegistreTracabiliteComposants.Select(c => new ComposantSelectionneDto
                    {
                        DesignationComposant = c.DesignationComposant,
                        LotSelectionne = c.LotSelectionne
                    }).ToList()
                }).ToList()
            };

            return dto;
        }

        public async Task<RegistreTracabiliteRowDto> AddLigneTracabiliteAsync(AddRegistreTracabiliteDto dto)
        {
            var statut = await _tracabiliteRepository.GetStatutTracabiliteAsync(dto.ExecControleOfId);
            if (statut != null && statut.EstTermine)
            {
                throw new Exception("Le registre de traçabilité est déjà clôturé.");
            }

            var latestRow = await _tracabiliteRepository.GetLatestLigneAsync(dto.ExecControleOfId);

            int nextVersion = 1;

            if (latestRow != null)
            {
                nextVersion = latestRow.Version;
                
                bool changed = false;
                if (latestRow.Corps != dto.Corps || latestRow.Volant != dto.Volant)
                {
                    changed = true;
                }
                else
                {
                    var latestComposants = latestRow.ExecRegistreTracabiliteComposants.ToDictionary(c => c.DesignationComposant, c => c.LotSelectionne);
                    
                    // If any new component is added, or an existing one changed its lot
                    foreach (var comp in dto.Composants)
                    {
                        if (!latestComposants.ContainsKey(comp.DesignationComposant) || latestComposants[comp.DesignationComposant] != comp.LotSelectionne)
                        {
                            changed = true;
                            break;
                        }
                    }

                    // Also check if a component was removed
                    if (!changed && dto.Composants.Count != latestComposants.Count)
                    {
                        changed = true;
                    }
                }

                if (changed)
                {
                    nextVersion++;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Corps))
            {
                var isValid = await ValidateLotAsync("CORPS", dto.Corps);
                if (!isValid) throw new Exception($"Le numéro de lot '{dto.Corps}' pour le Corps n'est pas reconnu.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Volant))
            {
                var isValid = await ValidateLotAsync("VOLANT", dto.Volant);
                if (!isValid) throw new Exception($"Le numéro de lot '{dto.Volant}' pour le Volant n'est pas reconnu.");
            }

            var newRow = new ExecRegistreTracabilite
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = dto.ExecControleOfId,
                DateHeure = DateTime.Now,
                Version = nextVersion,
                Corps = dto.Corps,
                Volant = dto.Volant,
                ExecRegistreTracabiliteComposants = dto.Composants.Select(c => new ExecRegistreTracabiliteComposant
                {
                    Id = Guid.NewGuid(),
                    DesignationComposant = c.DesignationComposant,
                    LotSelectionne = c.LotSelectionne
                }).ToList()
            };

            await _tracabiliteRepository.AddLigneAsync(newRow);

            return new RegistreTracabiliteRowDto
            {
                Id = newRow.Id,
                DateHeure = newRow.DateHeure,
                Version = newRow.Version,
                Corps = newRow.Corps,
                Volant = newRow.Volant,
                Composants = newRow.ExecRegistreTracabiliteComposants.Select(c => new ComposantSelectionneDto
                {
                    DesignationComposant = c.DesignationComposant,
                    LotSelectionne = c.LotSelectionne
                }).ToList()
            };
        }

        public async Task<bool> ValidateLotAsync(string typeArticle, string numeroLot)
        {
            if (string.IsNullOrWhiteSpace(numeroLot)) return false;
            return await _tracabiliteRepository.IsLotValideAsync(numeroLot, typeArticle);
        }

        public async Task<List<string>> SearchLotsAsync(string typeArticle, string query)
        {
            return await _tracabiliteRepository.SearchLotsAsync(typeArticle, query);
        }

        public async Task<bool> CloturerRegistreAsync(Guid execControleOfId, string posteCode, string matriculeOperateur)
        {
            var statut = await _tracabiliteRepository.GetStatutTracabiliteAsync(execControleOfId);

            if (statut == null)
            {
                statut = new ExecControleDocumentStatut
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfId = execControleOfId,
                    PosteCode = posteCode,
                    TypeDocument = "REGISTRE_TRACA",
                    EstTermine = true,
                    DateTermine = DateTime.Now,
                    DateExecution = DateTime.Now,
                    MatriculeOperateur = matriculeOperateur
                };
                await _tracabiliteRepository.AddStatutTracabiliteAsync(statut);
            }
            else
            {
                if (!statut.EstTermine)
                {
                    statut.EstTermine = true;
                    statut.DateTermine = DateTime.Now;
                    statut.MatriculeOperateur = matriculeOperateur;
                    
                    await _tracabiliteRepository.SaveChangesAsync();
                }
            }
            
            return true;
        }

        public async Task<bool> DeleteLigneTracabiliteAsync(Guid ligneId)
        {
            var ligne = await _tracabiliteRepository.GetLigneAsync(ligneId);
            if (ligne == null)
            {
                return false;
            }

            var statut = await _tracabiliteRepository.GetStatutTracabiliteAsync(ligne.ExecControleOfId);
            if (statut != null && statut.EstTermine)
            {
                throw new Exception("Le registre de traçabilité est déjà clôturé.");
            }

            await _tracabiliteRepository.DeleteLigneAsync(ligne);
            return true;
        }
    }
}
