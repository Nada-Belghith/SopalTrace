using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.Assemblage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Application.DTOs.QualityPlans.Assemblage;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class PlanAssService : IPlanAssService
{
    private readonly IPlanAssRepository _repository;
    private readonly IValidator<CreatePlanAssRequestDto> _createValidator;

    public PlanAssService(IPlanAssRepository repository, IValidator<CreatePlanAssRequestDto> createValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
    }

    public async Task<Guid> CreerPlanAsync(CreatePlanAssRequestDto request, string creePar)
    {
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var estModele = request.EstModele;
        var codeArticle = estModele ? null : request.CodeArticleSage;
        string? designationSage = null;

        if (estModele)
        {
            if (await _repository.ExistePlanMaitreActifAsync(request.OperationCode, request.TypeRobinetCode))
                throw new InvalidOperationException("Un Plan Maître ACTIF existe déjà pour ce type de robinet.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(codeArticle))
                throw new InvalidOperationException("Le code article SAGE est obligatoire pour une exception.");

            if (await _repository.ExisteExceptionActiveAsync(request.OperationCode, request.TypeRobinetCode, codeArticle))
                throw new InvalidOperationException($"Un Plan d'exception ACTIF existe déjà pour l'article {codeArticle}.");

            designationSage = await _repository.GetDesignationArticleSageAsync(codeArticle);
            if (string.IsNullOrWhiteSpace(designationSage))
                throw new InvalidOperationException($"L'article SAGE '{codeArticle}' n'existe pas dans l'ERP.");
        }

        var nextVersion = await _repository.GetDerniereVersionAsync(request.OperationCode, request.TypeRobinetCode, codeArticle) + 1;

        var nouveauPlan = new PlanAssEntete
        {
            Id = Guid.NewGuid(),
            OperationCode = request.OperationCode,
            TypeRobinetCode = request.TypeRobinetCode,
            EstModele = estModele,
            CodeArticleSage = codeArticle,
            Nom = request.Nom,
            Designation = designationSage,
            Version = nextVersion,
            Statut = StatutsPlan.Brouillon,
            CreePar = Truncate20(creePar),
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = request.CommentaireVersion ?? "Création initiale",
            PlanAssSections = new List<PlanAssSection>()
        };

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }

    public async Task<PlanAssResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan is null)
            throw new InvalidOperationException("Plan introuvable.");

        return PlanAssMapper.MapperEntitePlanVersDto(plan);
    }

    public async Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionAssEditDto> sectionsModifiees)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan is null)
            return false;

        foreach (var secDto in sectionsModifiees ?? new List<SectionAssEditDto>())
        {
            var isNewSection = !secDto.Id.HasValue || secDto.Id.Value == Guid.Empty;
            var section = isNewSection ? null : plan.PlanAssSections.FirstOrDefault(s => s.Id == secDto.Id);

            if (section is null)
            {
                section = PlanAssMapper.ConstruireNouvelleSection(planId, secDto);
                plan.PlanAssSections.Add(section);
            }
            else
            {
                section.TypeSectionId = secDto.TypeSectionId;
                section.PeriodiciteId = secDto.PeriodiciteId;
                section.OrdreAffiche = secDto.OrdreAffiche;
                section.LibelleSection = secDto.LibelleSection ?? section.LibelleSection;
                section.Notes = secDto.Notes;
            }

            foreach (var ligDto in secDto.Lignes)
            {
                var isNewLigne = !ligDto.Id.HasValue || ligDto.Id.Value == Guid.Empty;
                var ligne = isNewLigne ? null : section.PlanAssLignes.FirstOrDefault(l => l.Id == ligDto.Id);

                if (ligne is null)
                {
                    var nouvelleLigne = PlanAssMapper.ConstruireNouvelleLigne(planId, section.Id, ligDto);
                    section.PlanAssLignes.Add(nouvelleLigne);
                }
                else
                {
                    PlanAssMapper.MettreAJourEntiteLigne(ligne, ligDto);
                }
            }
        }

        if (plan.Statut == StatutsPlan.Brouillon)
        {
            plan.Statut = StatutsPlan.Actif;
            // .NET 8 support:
            plan.DateApplication = DateOnly.FromDateTime(DateTime.UtcNow);

            var ancienPlanActif = plan.EstModele
                ? await _repository.GetPlanActifMaitreAsync(plan.OperationCode, plan.TypeRobinetCode)
                : await _repository.GetPlanActifExceptionAsync(plan.OperationCode, plan.TypeRobinetCode, plan.CodeArticleSage!);

            if (ancienPlanActif is not null && ancienPlanActif.Id != plan.Id)
            {
                ancienPlanActif.Statut = StatutsPlan.Archive;
                ancienPlanActif.ModifiePar = Truncate20(plan.ModifiePar ?? "SYSTEM");
                ancienPlanActif.ModifieLe = DateTime.UtcNow;
                ancienPlanActif.CommentaireVersion = $"Archivé suite à activation V{plan.Version}";
            }
        }

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangerStatutPlanAsync(Guid planId, ChangePlanAssStatusRequestDto request, string modifiePar)
    {
        var plan = await _repository.GetPlanByIdAsync(planId);
        if (plan is null)
            return false;

        if (plan.Statut == request.NouveauStatut)
            return false;

        plan.Statut = request.NouveauStatut;
        plan.ModifiePar = Truncate20(modifiePar);
        plan.ModifieLe = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Motif))
            plan.CommentaireVersion = request.Motif;

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<Guid> ClonerExceptionDepuisMaitreAsync(CloneExceptionAssRequestDto request)
    {
        var planMaitre = await _repository.GetPlanAvecRelationsAsync(request.PlanMaitreId);
        if (planMaitre is null || !string.IsNullOrWhiteSpace(planMaitre.CodeArticleSage))
            throw new InvalidOperationException("Le plan source est introuvable ou n'est pas un plan maître.");

        if (await _repository.ExisteExceptionActiveAsync(planMaitre.OperationCode, planMaitre.TypeRobinetCode, request.NouveauCodeArticleSage))
            throw new InvalidOperationException($"Une exception ACTIF existe déjà pour l'article {request.NouveauCodeArticleSage}.");

        var designationSage = await _repository.GetDesignationArticleSageAsync(request.NouveauCodeArticleSage);
        if (string.IsNullOrWhiteSpace(designationSage))
            throw new InvalidOperationException($"L'article SAGE '{request.NouveauCodeArticleSage}' n'existe pas dans le référentiel ERP.");

        var planException = PlanAssMapper.DupliquerEntitePlan(
            planMaitre,
            estModele: false,
            nouveauCodeArticle: request.NouveauCodeArticleSage,
            nouvelleDesig: designationSage,
            creePar: Truncate20(request.CreePar),
            motif: "Exception clonée depuis plan maître.");

        await _repository.AddPlanAsync(planException);
        await _repository.SaveChangesAsync();

        return planException.Id;
    }

    public async Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionAssRequestDto request)
    {
        var ancienPlan = await _repository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan is null)
            throw new InvalidOperationException("Plan introuvable.");

        if (ancienPlan.Statut == StatutsPlan.Archive)
            throw new InvalidOperationException("Action impossible : Ce plan est déjà archivé.");

        var nouveauPlan = PlanAssMapper.DupliquerEntitePlan(
            ancienPlan,
            estModele: ancienPlan.EstModele,
            nouveauCodeArticle: ancienPlan.CodeArticleSage,
            nouvelleDesig: ancienPlan.Designation,
            creePar: Truncate20(request.ModifiePar),
            motif: request.MotifModification);

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }

    private static string Truncate20(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "SYSTEM";

        return value.Length > 20 ? value[..20] : value;
    }
}