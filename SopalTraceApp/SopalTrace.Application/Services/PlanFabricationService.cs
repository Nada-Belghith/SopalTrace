using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class PlanFabricationService : IPlanFabricationService
{
    private readonly IPlanFabricationRepository _repository;
    private readonly IValidator<CreatePlanRequestDto> _createPlanValidator;
    private readonly IValidator<ClonePlanRequestDto> _clonePlanValidator;
    private readonly IValidator<PlanFabEntete> _planActivationValidator;

    public PlanFabricationService(
        IPlanFabricationRepository repository,
        IValidator<CreatePlanRequestDto> createPlanValidator,
        IValidator<ClonePlanRequestDto> clonePlanValidator,
        IValidator<PlanFabEntete> planActivationValidator)
    {
        _repository = repository;
        _createPlanValidator = createPlanValidator;
        _clonePlanValidator = clonePlanValidator;
        _planActivationValidator = planActivationValidator;
    }

    public async Task<Guid> InstancierPlanDepuisModeleAsync(CreatePlanRequestDto request)
    {
        var validationResult = await _createPlanValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var designationSage = await _repository.GetDesignationArticleSageAsync(request.CodeArticleSage);
        if (string.IsNullOrEmpty(designationSage)) throw new ArticleSageIntrouvableException(request.CodeArticleSage);

        var modeleSourceId = request.ModeleSourceId.HasValue && request.ModeleSourceId.Value != Guid.Empty
            ? request.ModeleSourceId
            : null;

        var brouillonExistant = await _repository.GetBrouillonLePlusRecentAsync(request.CodeArticleSage, modeleSourceId, request.OperationCode);
        if (brouillonExistant != null) return brouillonExistant.Id;

        PlanFabEntete nouveauPlan;

        if (modeleSourceId.HasValue)
        {
            var modeleSource = await _repository.GetModeleActifAvecRelationsAsync(modeleSourceId.Value);
            if (modeleSource == null) throw new ModeleIntrouvableException(modeleSourceId.Value);

            if (modeleSource.NatureComposantCodeNavigation != null && modeleSource.NatureComposantCodeNavigation.EstGenerique)
                throw new ModeleGeneriqueException(modeleSourceId.Value);

            nouveauPlan = PlanFabricationMapper.ConstruireEntitePlanAPartirDeModele(modeleSource, request, designationSage);
        }
        else
        {
            nouveauPlan = PlanFabricationMapper.ConstruireEntitePlanVierge(request, designationSage);
        }

        var prochaineVersion = await _repository.GetDerniereVersionPlanAsync(request.CodeArticleSage) + 1;
        nouveauPlan.Version = prochaineVersion;

        if (string.IsNullOrWhiteSpace(request.Nom))
            nouveauPlan.Nom = $"PC-{request.CodeArticleSage}-V{prochaineVersion}";

        try
        {
            await _repository.AddPlanAsync(nouveauPlan);
            await _repository.SaveChangesAsync();
        }
        catch (ConflitConcurrenceException)
        {
            var draft = await _repository.GetBrouillonLePlusRecentAsync(request.CodeArticleSage, modeleSourceId);
            if (draft != null) return draft.Id;
            throw;
        }

        return nouveauPlan.Id;
    }

    // ⚠️ NOUVELLE MÉTHODE : Suppression Physique (Hard Delete)
    public async Task<bool> SupprimerBrouillonAsync(Guid planId)
    {
        var plan = await _repository.GetPlanByIdAsync(planId);
        
        if (plan == null) return false;

        if (plan.Statut != StatutsPlan.Brouillon)
        {
            throw new InvalidOperationException("Impossible de supprimer ce plan : seul un BROUILLON peut être détruit physiquement.");
        }

        _repository.Delete(plan);
        await _repository.SaveChangesAsync();
        
        return true;
    }

    public async Task<PlanResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new PlanIntrouvableException(planId);

        return PlanFabricationMapper.MapperEntitePlanVersDto(plan);
    }

    public async Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionEditDto> sectionsModifiees, string? legendeMoyens = null, bool finaliser = true, string? nom = null)
    {
        var plan = await _repository.GetPlanCompletPourMiseAJourAsync(planId);
        if (plan == null) return false;

        // Mise à jour du nom personnalisé manuellement saisi
        if (!string.IsNullOrWhiteSpace(nom))
        {
            plan.Nom = nom;
        }

        // Mise à jour des autres valeurs du plan existant avec les nouvelles valeurs
        if (legendeMoyens is not null)
        {
            plan.LegendeMoyens = string.IsNullOrWhiteSpace(legendeMoyens) ? null : legendeMoyens;
        }

        var sectionsAconserver = new List<PlanFabSection>();

        foreach (var secDto in sectionsModifiees)
        {
            var section = secDto.Id.HasValue ? plan.PlanFabSections.FirstOrDefault(s => s.Id == secDto.Id.Value) : null;

            if (section != null)
            {
                section.OrdreAffiche = secDto.OrdreAffiche;
                section.LibelleSection = secDto.LibelleSection ?? section.LibelleSection;
                section.FrequenceLibelle = string.IsNullOrWhiteSpace(secDto.FrequenceLibelle) ? null : secDto.FrequenceLibelle;
            }
            else
            {
                section = PlanFabricationMapper.ConstruireNouvelleSectionPlan(planId, secDto);
                plan.PlanFabSections.Add(section);
            }

            sectionsAconserver.Add(section);

            var lignesAconserver = new List<PlanFabLigne>();
            foreach (var ligDto in secDto.Lignes)
            {
                var ligne = ligDto.Id.HasValue ? section.PlanFabLignes.FirstOrDefault(l => l.Id == ligDto.Id.Value) : null;

                if (ligne != null)
                {
                    PlanFabricationMapper.MettreAJourEntiteLigne(ligne, ligDto);
                    ligne.OrdreAffiche = ligDto.OrdreAffiche;
                }
                else
                {
                    ligne = PlanFabricationMapper.ConstruireNouvelleLignePlan(planId, section.Id, ligDto);
                    section.PlanFabLignes.Add(ligne);
                }
                lignesAconserver.Add(ligne);
            }

            var lignesASupprimer = section.PlanFabLignes.Where(l => !lignesAconserver.Contains(l)).ToList();
            foreach (var l in lignesASupprimer) 
            {
                section.PlanFabLignes.Remove(l);
                _repository.DeleteLigne(l); // Oblige EF Core à supprimer l'entité
            }
        }

        var sectionsASupprimer = plan.PlanFabSections.Where(s => !sectionsAconserver.Contains(s)).ToList();
        foreach (var s in sectionsASupprimer) 
        {
            plan.PlanFabSections.Remove(s);
            _repository.DeleteSection(s); // Oblige EF Core à supprimer l'entité
        }

        if (plan.Statut == StatutsPlan.Brouillon && finaliser)
        {
             // Validation stricte avant d'activer le plan
             await ValiderPlanPourActivationAsync(plan);

             await ArchiverPlanActifExistantAsync(plan.CodeArticleSage, plan.ModifiePar ?? "SYSTEM");
             await _repository.SaveChangesAsync();

             plan.Statut = StatutsPlan.Actif;
             plan.DateApplication = DateOnly.FromDateTime(DateTime.UtcNow);

             if (string.IsNullOrWhiteSpace(plan.Nom))
             {
                 plan.Nom = $"PC-{plan.CodeArticleSage}-V{plan.Version}";
             }
         }

         await _repository.SaveChangesAsync();
         return true;
    }

    public async Task<bool> ChangerStatutPlanAsync(Guid planId, ChangePlanStatusRequestDto request, string modifiePar)
    {
        var plan = await _repository.GetPlanByIdAsync(planId);
        if (plan == null) throw new PlanIntrouvableException(planId);

        if (plan.Statut == request.NouveauStatut) return false;

        plan.Statut = request.NouveauStatut;
        plan.ModifiePar = SecuriserNomAuteur(modifiePar);
        plan.ModifieLe = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Motif)) plan.CommentaireVersion = request.Motif;

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<Guid> ClonerPlanPourNouvelArticleAsync(ClonePlanRequestDto request)
    {
        var validationResult = await _clonePlanValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var designationSage = await _repository.GetDesignationArticleSageAsync(request.NouveauCodeArticleSage);
        if (string.IsNullOrEmpty(designationSage)) throw new ArticleSageIntrouvableException(request.NouveauCodeArticleSage);

        var planSource = await _repository.GetPlanAvecRelationsAsync(request.PlanExistantId);
        if (planSource == null) throw new PlanIntrouvableException(request.PlanExistantId);

        // <-- Reprise silencieuse d'un brouillon existant de Clone (évite le conflit autosave simultané)
        var brouillonExistant = await _repository.GetBrouillonLePlusRecentAsync(request.NouveauCodeArticleSage, planSource.ModeleSourceId, planSource.OperationCode);
        if (brouillonExistant != null)
        {
            return brouillonExistant.Id; 
        }

        var planClone = PlanFabricationMapper.DupliquerEntitePlan(planSource, request.NouveauCodeArticleSage, designationSage, SecuriserNomAuteur(request.CreePar));

        // Prendre la version max relative à cette opération uniquement
        var prochaineVersion = await _repository.GetDerniereVersionPlanAsync(request.NouveauCodeArticleSage, planSource.OperationCode) + 1;
        planClone.Version = prochaineVersion;
        planClone.Nom = $"PC-{request.NouveauCodeArticleSage}-V{prochaineVersion}";

        try
        {
            await _repository.AddPlanAsync(planClone);
            await _repository.SaveChangesAsync();
        }
        catch (ConflitConcurrenceException) // Filet de sécurité si UI/autosave force un doublon
        {
            var draft = await _repository.GetBrouillonLePlusRecentAsync(request.NouveauCodeArticleSage, planSource.ModeleSourceId, planSource.OperationCode);
            if (draft != null) return draft.Id;
            throw;
        }

        return planClone.Id;
    }

    public async Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionRequestDto request)
    {
        var ancienPlan = await _repository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new PlanIntrouvableException(request.AncienId);
        if (ancienPlan.Statut == StatutsPlan.Archive) throw new PlanArchiveException();
        if (ancienPlan.Statut != StatutsPlan.Actif) throw new PlanSourceNonActifException(request.AncienId);

        var brouillonExistant = await _repository.GetBrouillonLePlusRecentAsync(ancienPlan.CodeArticleSage, ancienPlan.ModeleSourceId, ancienPlan.OperationCode);
        if (brouillonExistant != null)
        {
            return brouillonExistant.Id;
        }

        var nouveauPlan = PlanFabricationMapper.DupliquerEntitePlan(ancienPlan, ancienPlan.CodeArticleSage, ancienPlan.Designation ?? $"Copy-{ancienPlan.Id}", SecuriserNomAuteur(request.ModifiePar), request.MotifModification);

        var prochaineVersion = await _repository.GetDerniereVersionPlanAsync(ancienPlan.CodeArticleSage, ancienPlan.OperationCode) + 1;
        nouveauPlan.Version = prochaineVersion;
        nouveauPlan.Nom = string.IsNullOrWhiteSpace(nouveauPlan.Nom)
            ? $"PC-{ancienPlan.CodeArticleSage}-V{prochaineVersion}"
            : ModeleFabricationMapper.IncrementerSuffixeVersion(nouveauPlan.Nom, prochaineVersion);

        try
        {
            await _repository.AddPlanAsync(nouveauPlan);
            await _repository.SaveChangesAsync();
        }
        catch (ConflitConcurrenceException)
        {
            var draft = await _repository.GetBrouillonLePlusRecentAsync(ancienPlan.CodeArticleSage, ancienPlan.ModeleSourceId, ancienPlan.OperationCode);
            if (draft != null) return draft.Id;
            throw;
        }

        return nouveauPlan.Id;
    }

    public async Task<Guid> RestaurerPlanArchiveAsync(RestaurerPlanRequestDto request)
    {
        var planArchive = await _repository.GetPlanAvecRelationsAsync(request.PlanArchiveId);
        if (planArchive == null) throw new PlanIntrouvableException(request.PlanArchiveId);
        if (planArchive.Statut != StatutsPlan.Archive) throw new Exception("Seul un plan archivé peut être restauré.");

        var auteurSecure = SecuriserNomAuteur(request.RestaurePar);
        
        await ArchiverPlanActifExistantAsync(planArchive.CodeArticleSage, auteurSecure, $"Archivé suite à la restauration de la version: {planArchive.Id}");
        
        var commentaireResto = $"[Restauré depuis archive] {request.MotifRestoration}";
        var nouveauPlan = PlanFabricationMapper.DupliquerEntitePlan(planArchive, planArchive.CodeArticleSage, planArchive.Designation, auteurSecure, commentaireResto);

        var prochaineVersion = await _repository.GetDerniereVersionPlanAsync(planArchive.CodeArticleSage) + 1;
        nouveauPlan.Version = prochaineVersion;
        nouveauPlan.Nom = string.IsNullOrWhiteSpace(nouveauPlan.Nom)
            ? $"PC-{planArchive.CodeArticleSage}-V{prochaineVersion}"
            : ModeleFabricationMapper.IncrementerSuffixeVersion(nouveauPlan.Nom, prochaineVersion);

        nouveauPlan.Statut = StatutsPlan.Actif;
        nouveauPlan.DateApplication = DateOnly.FromDateTime(DateTime.UtcNow);

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }

    public async Task<IReadOnlyList<PlanResponseDto>> GetPlansByFiltersAsync(string? typeRobinetCode, string? natureComposantCode, string? operationCode)
    {
        var plans = await _repository.GetPlansParFiltresAsync(typeRobinetCode, natureComposantCode, operationCode);
        return plans.Select(PlanFabricationMapper.MapperEntitePlanVersDto).ToList();
    }

    private string SecuriserNomAuteur(string auteur) => string.IsNullOrWhiteSpace(auteur) ? "SYSTEM" : (auteur.Length > 20 ? auteur[..20] : auteur);

    private async Task ValiderPlanPourActivationAsync(PlanFabEntete plan)
    {
        var validationResult = await _planActivationValidator.ValidateAsync(plan);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
    }

    private async Task ArchiverPlanActifExistantAsync(string codeArticleSage, string auteur, string commentaireVersion = null)
    {
        var planActifExiste = await _repository.GetPlanActifPourArticleAsync(codeArticleSage);
        if (planActifExiste != null)
        {
            planActifExiste.Statut = StatutsPlan.Archive;
            planActifExiste.ModifiePar = auteur;
            planActifExiste.ModifieLe = DateTime.UtcNow;
            if (commentaireVersion != null) planActifExiste.CommentaireVersion = commentaireVersion;
        }
    }
}