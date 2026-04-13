using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.PlansNC;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class PlanNcService : IPlanNcService
{
    private readonly IUnitOfWork _unitOfWork; 
    private readonly ILogger<PlanNcService> _logger;
    private readonly IPlanNcRepository _repository;
    private readonly IValidator<CreatePlanNcRequestDto> _createValidator;

    public PlanNcService(
        IUnitOfWork unitOfWork,
        IPlanNcRepository repository,
        ILogger<PlanNcService> logger,
        IValidator<CreatePlanNcRequestDto> createValidator)
    {
        _unitOfWork = unitOfWork;
        _repository = repository;
        _logger = logger;
        _createValidator = createValidator;
    }

    public async Task<Guid> CreerPlanAsync(CreatePlanNcRequestDto request, string creePar)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(_createValidator);

        var validationResult = await _createValidator.ValidateAsync(request);
        if (validationResult is null)
            throw new InvalidOperationException("Le validateur a retourné un résultat null.");

        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        if (await _repository.ExistePlanActifAsync(request.TypeRobinetCode, request.OperationCode, request.PosteCode))
            throw new InvalidOperationException($"Un plan ACTIF existe déjà pour ce poste ({request.PosteCode}).");

        var nouveauPlan = new PlanNcEntete
        {
            Id = Guid.NewGuid(),
            TypeRobinetCode = request.TypeRobinetCode,
            OperationCode = request.OperationCode,
            PosteCode = request.PosteCode,
            FormulaireId = request.FormulaireId,
            Nom = request.Nom,
            Version = 1,
            Statut = StatutsPlan.Brouillon,
            CreePar = creePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = request.CommentaireVersion ?? "Création initiale",
            PlanNcColonnes = new List<PlanNcColonne>()
        };

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }

    public async Task<PlanNcResponseDto> GetPlanByIdAsync(Guid planId)
    {
        // On passe par _unitOfWork.PlanNcRepository
        var plan = await _unitOfWork.PlanNcRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new InvalidOperationException("Plan introuvable.");
        return PlanNcMapper.MapperEntiteVersDto(plan);
    }

    // Le "Tree Update" simplifié (pas de sections, juste des colonnes)
    public async Task<bool> MettreAJourColonnesAsync(Guid planId, List<ColonneNcEditDto> colonnesModifiees)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) return false;

        // 1. Supprimer les colonnes qui ne sont plus dans la liste (Liberté Totale)
        // Utilisation explicite de .Value pour éviter l'erreur de conversion implicite Guid? vers Guid
        var dtoIds = colonnesModifiees
            .Select(c => c.Id)
            .OfType<Guid>()
            .ToList();

        var colonnesASupprimer = plan.PlanNcColonnes.Where(c => !dtoIds.Contains(c.Id)).ToList();

        foreach (var col in colonnesASupprimer)
        {
            _repository.RemoveColonne(col);
        }

        // 2. Mettre à jour ou Ajouter
        foreach (var colDto in colonnesModifiees)
        {
            var isNew = !colDto.Id.HasValue || colDto.Id.Value == Guid.Empty;

            var colonneEnBase = !isNew && colDto.Id is Guid colId
                ? plan.PlanNcColonnes.FirstOrDefault(c => c.Id == colId)
                : null;

            if (colonneEnBase != null)
            {
                PlanNcMapper.MettreAJourColonne(colonneEnBase, colDto);
            }
            else
            {
                var nouvelleColonne = PlanNcMapper.ConstruireNouvelleColonne(planId, colDto);
                _repository.AddColonne(nouvelleColonne);
            }
        }

        // 3. Gestion des statuts et ISO 9001 (Archivage automatique de l'ancienne version)
        if (plan.Statut == StatutsPlan.Brouillon)
        {
            plan.Statut = StatutsPlan.Actif;

            var ancienPlanActif = await _repository.GetPlanActifAsync(plan.TypeRobinetCode, plan.OperationCode, plan.PosteCode);
            if (ancienPlanActif != null && ancienPlanActif.Id != plan.Id)
            {
                ancienPlanActif.Statut = StatutsPlan.Archive;
                ancienPlanActif.ModifiePar = plan.ModifiePar ?? "SYSTEM";
                ancienPlanActif.ModifieLe = DateTime.UtcNow;
                ancienPlanActif.CommentaireVersion = $"Archivé automatiquement suite à l'activation de la version {plan.Version}";
            }
        }

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionNcRequestDto request)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var ancienPlan = await _unitOfWork.PlanNcRepository.GetPlanAvecRelationsAsync(request.AncienId);
            if (ancienPlan == null) throw new InvalidOperationException("Plan introuvable.");
            if (ancienPlan.Statut == StatutsPlan.Archive) throw new PlanArchiveException();

            var nouveauPlan = PlanNcMapper.DupliquerEntitePlan(ancienPlan, request.ModifiePar, request.MotifModification);

            await _unitOfWork.PlanNcRepository.AddPlanAsync(nouveauPlan);

            return nouveauPlan.Id;
        });
    }
}
