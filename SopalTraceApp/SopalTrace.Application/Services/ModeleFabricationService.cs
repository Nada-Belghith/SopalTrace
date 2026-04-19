using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Exceptions;
using System;
using System.Collections.Generic; // <-- Ajouté pour IReadOnlyList
using System.Linq;                 // <-- Ajouté pour .Select()
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class ModeleFabricationService : IModeleFabricationService
{
    private readonly IPlanFabricationRepository _repository;
    private readonly IValidator<CreateModeleRequestDto> _modeleValidator;

    public ModeleFabricationService(IPlanFabricationRepository repository, IValidator<CreateModeleRequestDto> modeleValidator)
    {
        _repository = repository;
        _modeleValidator = modeleValidator;
    }

    public async Task<Guid> CreerModeleAsync(CreateModeleRequestDto request)
    {
        var validationResult = await _modeleValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        if (await _repository.ExisteModeleActifAsync(request.TypeRobinetCode, request.NatureComposantCode, request.OperationCode))
            throw new DoublonModeleException();

        var nouveauModele = ModeleFabricationMapper.ConstruireEntiteModeleAPartirDeDto(request);

        await _repository.AddModeleAsync(nouveauModele);
        await _repository.SaveChangesAsync();

        return nouveauModele.Id;
    }

    public async Task<ModeleResponseDto> GetModeleByIdAsync(Guid modeleId)
    {
        var modele = await _repository.GetModeleAvecRelationsAsync(modeleId);
        if (modele is null)
            throw new ModeleIntrouvableException(modeleId);

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
        if (ancienModele.Statut == StatutsPlan.Archive) throw new Exception("Impossible de versionner un modèle archivé.");

        var auteurSecure = SecuriserNomAuteur(!string.IsNullOrWhiteSpace(request.CreePar) ? request.CreePar : request.ModifiePar);
        
        await ArchiverAncienModeleAsync(request.AncienId, auteurSecure);

        var type = request.TypeRobinetCode ?? ancienModele.TypeRobinetCode;
        var nature = request.NatureComposantCode ?? ancienModele.NatureComposantCode;
        var op = request.OperationCode ?? ancienModele.OperationCode;

        var nouvelleVersion = await CalculerNouvelleVersionAsync(type, nature, op);

        var nouveauModele = ModeleFabricationMapper.ConstruireNouvelleVersionModele(ancienModele, request, auteurSecure, nouvelleVersion);

        await _repository.AddModeleAsync(nouveauModele);
        await _repository.SaveChangesAsync();

        return nouveauModele.Id;
    }

    public async Task<Guid> RestaurerModeleArchiveAsync(RestaurerModeleRequestDto request)
    {
        var archiveModele = await _repository.GetModeleAvecRelationsAsync(request.ModeleArchiveId);
        if (archiveModele == null) throw new ModeleIntrouvableException(request.ModeleArchiveId);
        if (archiveModele.Statut != StatutsPlan.Archive) throw new Exception("Seul un modèle archivé peut être restauré.");

        var auteurSecure = SecuriserNomAuteur(request.RestaurePar);
        await ArchiverModeleActifExistantAsync(archiveModele.TypeRobinetCode, archiveModele.NatureComposantCode, archiveModele.OperationCode, auteurSecure);

        var nouvelleVersion = await CalculerNouvelleVersionAsync(archiveModele.TypeRobinetCode, archiveModele.NatureComposantCode, archiveModele.OperationCode);

        var nouveauModele = ModeleFabricationMapper.RestaurerEntiteModele(archiveModele, auteurSecure, request.MotifRestoration, nouvelleVersion);

        await _repository.AddModeleAsync(nouveauModele);
        await _repository.SaveChangesAsync();

        return nouveauModele.Id;
    }

    private string SecuriserNomAuteur(string auteur) => string.IsNullOrWhiteSpace(auteur) ? "SYSTEM" : (auteur.Length > 20 ? auteur[..20] : auteur);

    private async Task ArchiverAncienModeleAsync(Guid ancienId, string auteurSecure)
    {
        var modele = await _repository.GetModelePourArchivageAsync(ancienId);
        if (modele != null && modele.Statut == StatutsPlan.Actif)
        {
            modele.Statut = StatutsPlan.Archive;
            modele.ArchiveLe = DateTime.UtcNow;
            modele.ArchivePar = auteurSecure;
        }
    }

    private async Task ArchiverModeleActifExistantAsync(string type, string nature, string operation, string auteurSecure)
    {
        var modeleActif = await _repository.GetModeleActifPourFamilleAsync(type, nature, operation);
        if (modeleActif != null)
        {
            modeleActif.Statut = StatutsPlan.Archive;
            modeleActif.ArchiveLe = DateTime.UtcNow;
            modeleActif.ArchivePar = auteurSecure;
        }
    }

    private async Task<int> CalculerNouvelleVersionAsync(string type, string nature, string operation)
    {
        var derniereVersion = await _repository.GetDerniereVersionModeleAsync(type, nature, operation);
        return derniereVersion + 1;
    }
}
