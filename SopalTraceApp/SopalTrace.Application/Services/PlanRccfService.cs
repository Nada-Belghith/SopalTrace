using SopalTrace.Application.DTOs.QualityPlans.PlanRCCF;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services
{
    public class PlanRccfService : BasePlanLifecycleService<PlanResultatControleCfEntete, CreatePlanRccfRequest, UpdatePlanRccfRequest>, IPlanRccfService
    {
        private readonly IReferentielService _referentielService;

        public PlanRccfService(IUnitOfWork unitOfWork, IReferentielService referentielService) 
            : base(unitOfWork)
        {
            _referentielService = referentielService;
        }

        protected override async Task<PlanResultatControleCfEntete?> ObtenirEntiteAsync(Guid id)
        {
            return await _unitOfWork.PlanRccfRepository.GetPlanAvecRelationsAsync(id);
        }

        private ICollection<PlanResultatControleCfSection> MapSections(IEnumerable<CreatePlanRccfSectionRequest>? sectionsDto, Guid planId)
        {
            if (sectionsDto == null) return new List<PlanResultatControleCfSection>();
            
            return sectionsDto.Select((s, sIndex) => {
                var sectionId = Guid.NewGuid();
                return new PlanResultatControleCfSection
                {
                    Id = sectionId,
                    PlanRccfenteteId = planId,
                    SectionType = s.SectionType,
                    LibelleAffiche = s.LibelleAffiche,
                    OrdreAffiche = s.OrdreAffiche > 0 ? s.OrdreAffiche : sIndex + 1,
                    PlanResultatControleCfLignes = s.Lignes?.Select((l, lIndex) => new PlanResultatControleCfLigne
                    {
                        Id = Guid.NewGuid(),
                        SectionId = sectionId,
                        Caracteristique = l.Caracteristique,
                        OrdreAffiche = l.OrdreAffiche > 0 ? l.OrdreAffiche : lIndex + 1,
                        TypeControleId = l.TypeControleId,
                        MoyenControleId = l.MoyenControleId,
                        InstrumentCode = l.InstrumentCode,
                        LimiteSpecTexte = l.LimiteSpecTexte,
                        Observations = l.Observations
                    }).ToList() ?? new List<PlanResultatControleCfLigne>()
                };
            }).ToList();
        }

        protected override async Task<PlanResultatControleCfEntete> CreerEntiteAsync(CreatePlanRccfRequest dto, string user)
        {
            var planId = Guid.NewGuid();
            var sections = MapSections(dto.Sections, planId);

            var plan = new PlanResultatControleCfEntete
            {
                Id = planId,
                PosteCode = dto.PosteCode,
                FormulaireId = dto.FormulaireId,
                Nom = dto.Nom,
                ConfigurationJson = dto.ConfigurationJson,
                Remarques = dto.Remarques,
                Version = 0,
                Statut = StatutsPlan.Brouillon,
                CreePar = user,
                CreeLe = DateTime.UtcNow,
                PlanResultatControleCfSections = sections
            };

            // Synchronisation automatique vers RefFormulaire
            string? codeRefFormulaire = null;
            if (plan.FormulaireId.HasValue)
            {
                var form = await _referentielService.GetFormulaireByIdAsync(plan.FormulaireId.Value);
                if (form != null)
                {
                    codeRefFormulaire = form.CodeReference;
                }
            }

            if (!string.IsNullOrEmpty(codeRefFormulaire))
            {
                var refResult = await _referentielService.UpdateFormulaireStructureAsync("RESULTAT_CONTROLE_CF", plan.ConfigurationJson, codeRefFormulaire);
                if (refResult.HasValue)
                {
                    plan.FormulaireId = refResult.Value.Id;
                    plan.Version = refResult.Value.Version;
                }
            }

            return plan;
        }

        protected override async Task ApplierMiseAJourDraftAsync(PlanResultatControleCfEntete plan, UpdatePlanRccfRequest dto, string user)
        {
            plan.Nom = dto.Nom ?? plan.Nom;
            plan.ConfigurationJson = dto.ConfigurationJson;
            plan.Remarques = dto.Remarques;
            plan.FormulaireId = dto.FormulaireId ?? plan.FormulaireId;
            plan.ModifiePar = user;
            plan.ModifieLe = DateTime.UtcNow;

            // Delete old sections and lines manually since EF tracking might have issues with full replacement
            if (plan.PlanResultatControleCfSections != null)
            {
                foreach (var section in plan.PlanResultatControleCfSections.ToList())
                {
                    if (section.PlanResultatControleCfLignes != null)
                    {
                        foreach (var ligne in section.PlanResultatControleCfLignes.ToList())
                        {
                            _unitOfWork.PlanRccfRepository.RemoveLigne(ligne);
                        }
                    }
                    _unitOfWork.PlanRccfRepository.RemoveSection(section);
                }
                plan.PlanResultatControleCfSections.Clear();
            }
            else
            {
                plan.PlanResultatControleCfSections = new List<PlanResultatControleCfSection>();
            }
            
            var newSections = MapSections(dto.Sections, plan.Id);
            foreach(var s in newSections) 
            {
                _unitOfWork.PlanRccfRepository.AddSection(s);
                plan.PlanResultatControleCfSections.Add(s);
            }

            // Synchronisation automatique vers RefFormulaire
            string? codeRefFormulaire = null;
            if (plan.FormulaireId.HasValue)
            {
                var form = await _referentielService.GetFormulaireByIdAsync(plan.FormulaireId.Value);
                if (form != null)
                {
                    codeRefFormulaire = form.CodeReference;
                }
            }

            if (!string.IsNullOrEmpty(codeRefFormulaire))
            {
                var refResult = await _referentielService.UpdateFormulaireStructureAsync("RESULTAT_CONTROLE_CF", plan.ConfigurationJson, codeRefFormulaire);
                if (refResult.HasValue)
                {
                    plan.FormulaireId = refResult.Value.Id;
                    plan.Version = refResult.Value.Version;
                }
            }
        }

        protected override async Task PersisterEntiteAsync(PlanResultatControleCfEntete plan)
        {
            await _unitOfWork.PlanRccfRepository.AddPlanAsync(plan);
        }

        protected override async Task<int> CalculerNouvelleVersionAsync(PlanResultatControleCfEntete plan)
        {
            var plans = await _unitOfWork.PlanRccfRepository.GetTousLesPlansAsync();
            var maxVersion = plans
                .Where(p => p.PosteCode == plan.PosteCode && p.FormulaireId == plan.FormulaireId)
                .Max(p => (int?)p.Version) ?? 0;
            return maxVersion + 1;
        }

        protected override async Task<PlanResultatControleCfEntete> CreerNouvelleVersionEntiteAsync(PlanResultatControleCfEntete ancienPlan, UpdatePlanRccfRequest dto, int nouvelleVersion, string user)
        {
            var planId = Guid.NewGuid();
            var sections = MapSections(dto.Sections, planId);

            var plan = new PlanResultatControleCfEntete
            {
                Id = planId,
                PosteCode = ancienPlan.PosteCode,
                FormulaireId = dto.FormulaireId ?? ancienPlan.FormulaireId,
                Nom = dto.Nom ?? ancienPlan.Nom,
                ConfigurationJson = dto.ConfigurationJson ?? ancienPlan.ConfigurationJson,
                Remarques = dto.Remarques ?? ancienPlan.Remarques,
                Version = nouvelleVersion,
                Statut = StatutsPlan.Brouillon,
                CreePar = user,
                CreeLe = DateTime.UtcNow,
                PlanResultatControleCfSections = sections
            };

            // Synchronisation automatique vers RefFormulaire
            string? codeRefFormulaire = null;
            if (plan.FormulaireId.HasValue)
            {
                var form = await _referentielService.GetFormulaireByIdAsync(plan.FormulaireId.Value);
                if (form != null)
                {
                    codeRefFormulaire = form.CodeReference;
                }
            }

            if (!string.IsNullOrEmpty(codeRefFormulaire))
            {
                var refResult = await _referentielService.UpdateFormulaireStructureAsync("RESULTAT_CONTROLE_CF", plan.ConfigurationJson, codeRefFormulaire, nouvelleVersion);
                if (refResult.HasValue)
                {
                    plan.FormulaireId = refResult.Value.Id;
                    plan.Version = refResult.Value.Version;
                }
            }

            return plan;
        }

        protected override async Task<PlanResultatControleCfEntete?> ObtenirBrouillonExistantAsync(CreatePlanRccfRequest dto)
        {
            var plans = await _unitOfWork.PlanRccfRepository.GetTousLesPlansAsync();
            return plans.FirstOrDefault(p => p.PosteCode == dto.PosteCode && p.FormulaireId == dto.FormulaireId && p.Statut == StatutsPlan.Brouillon);
        }

        protected override async Task HandleVersioningBeforeActivationAsync(PlanResultatControleCfEntete plan, string user)
        {
            var planActif = await _unitOfWork.PlanRccfRepository.GetPlanActifAsync(plan.PosteCode, plan.FormulaireId.Value);
            if (planActif != null)
            {
                planActif.Statut = StatutsPlan.Archive;
                planActif.ModifiePar = user;
                planActif.ModifieLe = DateTime.UtcNow;
            }
        }

        // Implementation of IPlanRccfService methods
        public async Task<IEnumerable<PlanRccfDto>> GetAllAsync(bool includeArchived)
        {
            var plans = await _unitOfWork.PlanRccfRepository.GetTousLesPlansAsync();
            if (!includeArchived)
            {
                plans = plans.Where(p => p.Statut != StatutsPlan.Archive).ToList();
            }
            return plans.Select(MapToDto);
        }

        public async Task<PlanRccfDto> GetByIdAsync(Guid id)
        {
            var plan = await ObtenirEntiteAsync(id);
            if (plan == null) throw new InvalidOperationException("Plan introuvable.");
            return MapToDto(plan);
        }

        public async Task<PlanRccfDto> CreateAsync(CreatePlanRccfRequest request, string matricule)
        {
            var id = await CreerBrouillonAsync(request, matricule);
            var plan = await ObtenirEntiteAsync(id);
            return MapToDto(plan!);
        }

        public async Task<PlanRccfDto> UpdateAsync(Guid id, UpdatePlanRccfRequest request, string matricule)
        {
            var plan = await ObtenirEntiteAsync(id);
            if (plan == null) throw new KeyNotFoundException("Plan introuvable.");

            if (plan.Statut == StatutsPlan.Actif)
            {
                var newVersion = await CalculerNouvelleVersionAsync(plan);
                var newPlan = await CreerNouvelleVersionEntiteAsync(plan, request, newVersion, matricule);
                
                newPlan.Statut = StatutsPlan.Actif;
                plan.Statut = StatutsPlan.Archive;
                
                await PersisterEntiteAsync(newPlan);
                await _unitOfWork.CommitAsync();
                
                var createdPlan = await ObtenirEntiteAsync(newPlan.Id);
                return MapToDto(createdPlan!);
            }
            else
            {
                await UpdateDraftAsync(id, request, matricule);
                var updatedPlan = await ObtenirEntiteAsync(id);
                return MapToDto(updatedPlan!);
            }
        }

        public async Task ValidateAsync(Guid id, string matricule)
        {
            await ActiverPlanAsync(id, matricule, default);
        }

        public async Task CancelValidationAsync(Guid id, string matricule)
        {
            var plan = await ObtenirEntiteAsync(id);
            if (plan == null) return;
            
            if (plan.Statut == StatutsPlan.Actif)
            {
                plan.Statut = StatutsPlan.Brouillon;
                plan.ModifiePar = matricule;
                plan.ModifieLe = DateTime.UtcNow;
                await _unitOfWork.CommitAsync();
            }
        }

        public async Task<PlanRccfDto> ArchiveAsync(Guid id, string matricule)
        {
            await ArchiverPlanAsync(id, matricule);
            var plan = await ObtenirEntiteAsync(id);
            return MapToDto(plan!);
        }

        public async Task<PlanRccfDto> CreateNewVersionAsync(Guid id, string matricule)
        {
            var ancienPlan = await ObtenirEntiteAsync(id);
            if (ancienPlan == null) throw new InvalidOperationException("Plan introuvable.");

            var newVersion = await CalculerNouvelleVersionAsync(ancienPlan);
            
            var dto = new UpdatePlanRccfRequest
            {
                Nom = ancienPlan.Nom,
                ConfigurationJson = ancienPlan.ConfigurationJson,
                Remarques = ancienPlan.Remarques,
                FormulaireId = ancienPlan.FormulaireId,
                Sections = ancienPlan.PlanResultatControleCfSections?.Select(s => new CreatePlanRccfSectionRequest
                {
                    SectionType = s.SectionType,
                    LibelleAffiche = s.LibelleAffiche,
                    OrdreAffiche = s.OrdreAffiche,
                    Lignes = s.PlanResultatControleCfLignes?.Select(l => new CreatePlanRccfLigneRequest
                    {
                        Caracteristique = l.Caracteristique,
                        OrdreAffiche = l.OrdreAffiche,
                        TypeControleId = l.TypeControleId,
                        MoyenControleId = l.MoyenControleId,
                        InstrumentCode = l.InstrumentCode,
                        LimiteSpecTexte = l.LimiteSpecTexte,
                        Observations = l.Observations
                    }).ToList() ?? new List<CreatePlanRccfLigneRequest>()
                }).ToList()
            };

            var nouveauPlan = await CreerNouvelleVersionEntiteAsync(ancienPlan, dto, newVersion, matricule);
            await PersisterEntiteAsync(nouveauPlan);
            await _unitOfWork.CommitAsync();

            var loadedPlan = await ObtenirEntiteAsync(nouveauPlan.Id);
            return MapToDto(loadedPlan!);
        }

        private PlanRccfDto MapToDto(PlanResultatControleCfEntete entity)
        {
            return new PlanRccfDto
            {
                Id = entity.Id,
                PosteCode = entity.PosteCode,
                FormulaireId = entity.FormulaireId,
                Nom = entity.Nom,
                Version = entity.Version,
                Statut = entity.Statut,
                ConfigurationJson = entity.ConfigurationJson,
                Remarques = entity.Remarques,
                CreePar = entity.CreePar,
                CreeLe = entity.CreeLe,
                ModifiePar = entity.ModifiePar,
                ModifieLe = entity.ModifieLe,
                Sections = entity.PlanResultatControleCfSections?.Select(s => new PlanRccfSectionDto
                {
                    Id = s.Id,
                    PlanRCCFEnteteId = s.PlanRccfenteteId,
                    SectionType = s.SectionType,
                    LibelleAffiche = s.LibelleAffiche,
                    OrdreAffiche = s.OrdreAffiche,
                    Lignes = s.PlanResultatControleCfLignes?.Select(l => new PlanRccfLigneDto
                    {
                        Id = l.Id,
                        SectionId = s.Id,
                        Caracteristique = l.Caracteristique,
                        OrdreAffiche = l.OrdreAffiche,
                        TypeControleId = l.TypeControleId,
                        MoyenControleId = l.MoyenControleId,
                        InstrumentCode = l.InstrumentCode,
                        LimiteSpecTexte = l.LimiteSpecTexte,
                        Observations = l.Observations
                    }).OrderBy(l => l.OrdreAffiche).ToList() ?? new List<PlanRccfLigneDto>()
                }).OrderBy(s => s.OrdreAffiche).ToList() ?? new List<PlanRccfSectionDto>()
            };
        }
    }
}
