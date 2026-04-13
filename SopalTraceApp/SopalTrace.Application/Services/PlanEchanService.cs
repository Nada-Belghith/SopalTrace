using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class PlanEchanService : IPlanEchanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePlanEchanRequestDto> _createValidator;
    private readonly ILogger<PlanEchanService> _logger;

    public PlanEchanService(IUnitOfWork unitOfWork, IValidator<CreatePlanEchanRequestDto> createValidator, ILogger<PlanEchanService> logger)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _logger = logger;
    }

    public async Task<Guid> CreerPlanAsync(CreatePlanEchanRequestDto request, string creePar)
    {
        var valResult = await _createValidator.ValidateAsync(request);
        if (!valResult.IsValid) throw new ValidationException(valResult.Errors);

        if (await _unitOfWork.PlanEchanRepository.ExistePlanActifAsync(request.CodeReference))
            throw new Exception($"Un profil d'échantillonnage ACTIF existe déjà avec la référence {request.CodeReference}.");

        var nouveauPlan = PlanEchanMapper.ConstruireNouveauPlan(request, creePar);

        await _unitOfWork.PlanEchanRepository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    public async Task<PlanEchanResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new Exception("Plan introuvable.");

        return PlanEchanMapper.MapperEntiteVersDto(plan);
    }

    // NOUVELLE LOGIQUE : L'Admin met à jour son Brouillon (V2) et l'active
    public async Task<bool> MettreAJourPlanAsync(Guid planId, UpdatePlanEchanRequestDto request)
    {
        var plan = await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) return false;

        // Mise à jour des valeurs du plan
        plan.NiveauControle = request.NiveauControle;
        plan.TypePlan = request.TypePlan;
        plan.ModeControle = request.ModeControle;
        plan.NqaId = request.NqaId;
        plan.FormulaireId = request.FormulaireId;

        // ISO 9001 : Activation et archivage de l'ancienne version
        if (plan.Statut == StatutsPlan.Brouillon)
        {
            plan.Statut = StatutsPlan.Actif;

            var ancienPlanActif = await _unitOfWork.PlanEchanRepository.GetPlanActifAsync(plan.CodeReference);
            if (ancienPlanActif != null && ancienPlanActif.Id != plan.Id)
            {
                ancienPlanActif.Statut = StatutsPlan.Archive;
                ancienPlanActif.CreeLe = DateTime.UtcNow;
                ancienPlanActif.CommentaireVersion = $"Archivé automatiquement suite à l'activation V{plan.Version}";
            }
        }

        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionEchanRequestDto request)
    {
        var ancienPlan = await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new Exception("Plan introuvable.");
        if (ancienPlan.Statut == StatutsPlan.Archive) throw new Exception("Impossible de versionner un plan archivé.");

        // CORRECTION MAJEURE : On NE TOUCHE PAS à l'ancienne version ici pour ne pas bloquer la prod.
        // L'archivage se fera au moment du PUT (MettreAJourPlanAsync).

        // Le mapper s'occupe de créer la V2 avec le statut BROUILLON
        var nouveauPlan = PlanEchanMapper.DupliquerEntitePlan(ancienPlan, request.ModifiePar, request.MotifModification);

        await _unitOfWork.PlanEchanRepository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }
}