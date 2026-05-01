using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
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

/// <summary>
/// Service de gestion des Modèles de Fabrication.
/// Plus de logique de BROUILLON ici (directement ACTIF ou ARCHIVE).
/// </summary>
public class ModeleFabricationService
    : BasePlanLifecycleService<ModeleFabEntete, CreateModeleRequestDto, NouvelleVersionModeleRequestDto>,
      IModeleFabricationService
{
    private readonly IPlanFabricationRepository _repository;
    private readonly IValidator<CreateModeleRequestDto> _modeleValidator;

    public ModeleFabricationService(
        IUnitOfWork unitOfWork,
        IPlanFabricationRepository repository,
        IValidator<CreateModeleRequestDto> modeleValidator)
        : base(unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _modeleValidator = modeleValidator ?? throw new ArgumentNullException(nameof(modeleValidator));
    }

    // ==================== ABSTRACT IMPLEMENTATIONS (Requis par la base) ====================

    protected override async Task<ModeleFabEntete?> ObtenirEntiteAsync(Guid id)
        => await _repository.GetModeleAvecRelationsAsync(id);

    protected override async Task<ModeleFabEntete> CreerEntiteAsync(CreateModeleRequestDto dto, string user)
    {
        var modele = ModeleFabricationMapper.ConstruireEntiteModeleAPartirDeDto(dto);
        modele.CreePar = user;
        modele.CreeLe = DateTime.UtcNow;
        return await Task.FromResult(modele);
    }

    protected override Task ApplierMiseAJourDraftAsync(ModeleFabEntete modele, NouvelleVersionModeleRequestDto dto, string user)
        => throw new NotSupportedException("Les modèles n'utilisent plus la logique de brouillon.");

    protected override async Task PersisterEntiteAsync(ModeleFabEntete modele)
        => await _repository.AddModeleAsync(modele);

    protected override async Task<int> CalculerNouvelleVersionAsync(ModeleFabEntete modele)
    {
        var derniere = await _repository.GetDerniereVersionModeleAsync(
            modele.TypeRobinetCode, modele.NatureComposantCode, modele.OperationCode);
        return derniere + 1;
    }

    protected override async Task<ModeleFabEntete> CreerNouvelleVersionEntiteAsync(
        ModeleFabEntete ancienModele,
        NouvelleVersionModeleRequestDto dto,
        int nouvelleVersion,
        string user)
    {
        return await Task.FromResult(ModeleFabricationMapper.ConstruireNouvelleVersionModele(ancienModele, dto, user, nouvelleVersion));
    }

    protected override Task<ModeleFabEntete?> ObtenirBrouillonExistantAsync(CreateModeleRequestDto dto)
        => Task.FromResult<ModeleFabEntete?>(null); // Plus de brouillon

    // ==================== PUBLIC API (LOGIQUE DIRECTE) ====================

    public async Task<Guid> CreerModeleAsync(CreateModeleRequestDto request)
    {
        var validationResult = await _modeleValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var user = "SYSTEM";

        // 1. Archiver l'ancien actif
        var modeleActif = await _repository.GetModeleActifPourFamilleAsync(
            request.TypeRobinetCode, request.NatureComposantCode, request.OperationCode);
        if (modeleActif != null)
        {
            modeleActif.Statut = StatutsPlan.Archive;
            modeleActif.ArchiveLe = DateTime.UtcNow;
            modeleActif.ArchivePar = user;
        }

        // 2. Créer le nouveau en ACTIF
        var modele = await CreerEntiteAsync(request, user);
        modele.Statut = StatutsPlan.Actif;
        modele.Version = await CalculerNouvelleVersionAsync(modele);

        await PersisterEntiteAsync(modele);
        await _unitOfWork.CommitAsync();

        return modele.Id;
    }

    public Task UpdateModeleBrouillonAsync(Guid id, CreateModeleRequestDto request)
        => throw new InvalidOperationException("La mise à jour de brouillon n'est pas supportée pour les modèles.");

    public Task ActiverModeleAsync(Guid id, string user)
        => Task.CompletedTask; // Déjà actif à la création

    public async Task<bool> SupprimerBrouillonAsync(Guid id)
    {
        var modele = await _repository.GetModeleAvecRelationsAsync(id);
        if (modele == null) return false;
        if (modele.Statut != StatutsPlan.Brouillon) return false;

        _repository.DeleteModele(modele);
        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<ModeleResponseDto> GetModeleByIdAsync(Guid modeleId)
    {
        var modele = await _repository.GetModeleAvecRelationsAsync(modeleId);
        if (modele is null) throw new ModeleIntrouvableException(modeleId);
        return ModeleFabricationMapper.MapperEntiteModeleVersDto(modele);
    }

    public async Task<IReadOnlyList<ModeleResponseDto>> GetModelesByFiltersAsync(string? typeRobinetCode, string? natureComposantCode, string? operationCode)
    {
        var modeles = await _repository.GetModelesParFiltresAsync(typeRobinetCode, natureComposantCode, operationCode);
        return modeles.Select(ModeleFabricationMapper.MapperEntiteModeleVersDto).ToList();
    }

    public async Task<Guid> CreerNouvelleVersionModeleAsync(NouvelleVersionModeleRequestDto request)
    {
        var ancienModele = await _repository.GetModeleAvecRelationsAsync(request.AncienId);
        if (ancienModele == null) throw new ModeleIntrouvableException(request.AncienId);

        var user = SecuriserNomAuteur(request.ModifiePar ?? "SYSTEM");

        // Archiver l'ancien
        if (ancienModele.Statut == StatutsPlan.Actif)
        {
            ancienModele.Statut = StatutsPlan.Archive;
            ancienModele.ArchiveLe = DateTime.UtcNow;
            ancienModele.ArchivePar = user;
        }

        var nouvelleVersion = await CalculerNouvelleVersionAsync(ancienModele);
        var nouveauModele = await CreerNouvelleVersionEntiteAsync(ancienModele, request, nouvelleVersion, user);
        nouveauModele.Statut = StatutsPlan.Actif;

        await PersisterEntiteAsync(nouveauModele);
        await _unitOfWork.CommitAsync();

        return nouveauModele.Id;
    }

    public async Task<Guid> RestaurerModeleArchiveAsync(RestaurerModeleRequestDto request)
    {
        var modeleArchive = await _repository.GetModeleAvecRelationsAsync(request.ModeleArchiveId);
        if (modeleArchive == null) throw new ModeleIntrouvableException(request.ModeleArchiveId);

        var user = SecuriserNomAuteur(request.RestaurePar);

        // Archiver l'actif actuel
        var modeleActif = await _repository.GetModeleActifPourFamilleAsync(
            modeleArchive.TypeRobinetCode, modeleArchive.NatureComposantCode, modeleArchive.OperationCode);
        if (modeleActif != null)
        {
            modeleActif.Statut = StatutsPlan.Archive;
            modeleActif.ArchiveLe = DateTime.UtcNow;
            modeleActif.ArchivePar = user;
        }

        var nouvelleVersion = await CalculerNouvelleVersionAsync(modeleArchive);
        var dto = new NouvelleVersionModeleRequestDto { AncienId = modeleArchive.Id, MotifModification = $"[Restauré] {request.MotifRestoration}" };
        var nouveauModele = await CreerNouvelleVersionEntiteAsync(modeleArchive, dto, nouvelleVersion, user);
        nouveauModele.Statut = StatutsPlan.Actif;

        await PersisterEntiteAsync(nouveauModele);
        await _unitOfWork.CommitAsync();

        return nouveauModele.Id;
    }
}
