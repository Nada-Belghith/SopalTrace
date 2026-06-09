using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Application.Utilities;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class ModeleAssemblageService : IModeleAssemblageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlanAssRepository _assRepository;
    private readonly IFormulairePrcService _referentielService;
    private readonly IValidator<CreateModeleRequestDto> _modeleValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ModeleAssemblageService> _logger;

    public ModeleAssemblageService(
        IUnitOfWork unitOfWork,
        IPlanAssRepository assRepository,
        IFormulairePrcService referentielService,
        IValidator<CreateModeleRequestDto> modeleValidator,
        ICurrentUserService currentUserService,
        ILogger<ModeleAssemblageService> logger)
    {
        _unitOfWork = unitOfWork;
        _assRepository = assRepository;
        _referentielService = referentielService;
        _modeleValidator = modeleValidator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Guid> CreerModeleAsync(CreateModeleRequestDto request)
    {
        var validationResult = await _modeleValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var user = _currentUserService.UserInfo;

        // --- LOGIQUE ASSEMBLAGE ---
        // Vérification par Code + Libellé
        if (await _assRepository.ExisteParCodeEtLibelleAsync(request.Code, request.Libelle))
        {
            throw new DoublonCodeModeleException($"{request.Code} avec le libellé '{request.Libelle}'");
        }

        var modele = PlanAssMapper.MapperModeleVersEntite(request, user);

        if (!string.IsNullOrEmpty(request.ConfigurationColonnesJson))
        {
            var formResult = await _referentielService.UpdateFormulaireStructureAsync(
                "EN_COURS_DE_ASSEMBLAGE",
                request.ConfigurationColonnesJson,
                request.RefFormulaireCodeReference,
                request.VersionInitiale);
            if (formResult.HasValue)
            {
                modele.FormulaireId = formResult.Value.Id;
            }
        }

        if (request.VersionInitiale.HasValue && request.VersionInitiale.Value > 0)
        {
            // Créer v0 Archivé
            var modeleV0 = PlanAssMapper.MapperModeleVersEntite(request, user);
            modeleV0.Version = 0;
            modeleV0.Statut = StatutsPlan.Archive;
            if (modele.FormulaireId.HasValue) modeleV0.FormulaireId = modele.FormulaireId;
            await SmartDictionaryPassAssAsync(modeleV0);
            await _assRepository.AddPlanAsync(modeleV0);

            // Créer vInitiale Actif
            modele.Statut = StatutsPlan.Actif;
            modele.Version = request.VersionInitiale.Value;
        }
        else
        {
            modele.Statut = StatutsPlan.Actif;
            modele.Version = 0;
        }

        await SmartDictionaryPassAssAsync(modele);
        await _assRepository.AddPlanAsync(modele);
        await _unitOfWork.CommitAsync();
        return modele.Id;
    }

    private async Task SmartDictionaryPassAssAsync(PlanAssemblageEntete modele)
    {
        var addedCaracs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedInstruments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedMoyens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sec in modele.PlanAssemblageSections)
        {
            foreach (var ligne in sec.PlanAssemblageLignes)
            {
                // Nettoyage des GUIDs et chaînes vides pour éviter les erreurs de clés étrangères
                if (ligne.MoyenControleId == Guid.Empty) ligne.MoyenControleId = null;

                if (string.IsNullOrWhiteSpace(ligne.InstrumentCode)) ligne.InstrumentCode = null;
                if (string.IsNullOrWhiteSpace(ligne.LibelleAffiche)) ligne.LibelleAffiche = null;
                if (string.IsNullOrWhiteSpace(ligne.MoyenTexteLibre)) ligne.MoyenTexteLibre = null;

                // Caractéristique
                if (!string.IsNullOrWhiteSpace(ligne.LibelleAffiche) && ligne.TypeCaracteristiqueId == Guid.Empty)
                {
                    var typeCarac = await _unitOfWork.DictionnaireQualiteRepository.GetTypeCaracteristiqueByLibelleAsync(ligne.LibelleAffiche);
                    if (typeCarac == null && !addedCaracs.Contains(ligne.LibelleAffiche))
                    {
                        typeCarac = new TypeCaracteristique
                        {
                            Id = Guid.NewGuid(),
                            Libelle = ligne.LibelleAffiche.Length > 80 ? ligne.LibelleAffiche.Substring(0, 80) : ligne.LibelleAffiche,
                            Code = $"CAR-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                            Actif = true
                        };
                        await _unitOfWork.DictionnaireQualiteRepository.AddTypeCaracteristiqueAsync(typeCarac);
                        addedCaracs.Add(ligne.LibelleAffiche);
                        ligne.TypeCaracteristiqueId = typeCarac.Id;
                    }
                    else if (typeCarac != null)
                    {
                        ligne.TypeCaracteristiqueId = typeCarac.Id;
                    }
                }

                // Moyen de contrôle
                if (!string.IsNullOrWhiteSpace(ligne.MoyenTexteLibre) && (ligne.MoyenControleId == null || ligne.MoyenControleId == Guid.Empty))
                {
                    var moyen = await _unitOfWork.DictionnaireQualiteRepository.GetMoyenControleByLibelleAsync(ligne.MoyenTexteLibre);
                    if (moyen == null && !addedMoyens.Contains(ligne.MoyenTexteLibre))
                    {
                        moyen = new MoyenControle
                        {
                            Id = Guid.NewGuid(),
                            Libelle = ligne.MoyenTexteLibre.Length > 80 ? ligne.MoyenTexteLibre.Substring(0, 80) : ligne.MoyenTexteLibre,
                            Code = $"MC-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                            Actif = true
                        };
                        await _unitOfWork.DictionnaireQualiteRepository.AddMoyenControleAsync(moyen);
                        addedMoyens.Add(ligne.MoyenTexteLibre);
                        ligne.MoyenControleId = moyen.Id;
                    }
                    else if (moyen != null)
                    {
                        ligne.MoyenControleId = moyen.Id;
                    }
                }

                // Instrument
                if (!string.IsNullOrWhiteSpace(ligne.InstrumentCode))
                {
                    var instrument = await _unitOfWork.DictionnaireQualiteRepository.GetInstrumentByCodeAsync(ligne.InstrumentCode);
                    if (instrument == null && !addedInstruments.Contains(ligne.InstrumentCode))
                    {
                        instrument = new Instrument
                        {
                            CodeInstrument = ligne.InstrumentCode.Length > 50 ? ligne.InstrumentCode.Substring(0, 50) : ligne.InstrumentCode,
                            Designation = ligne.InstrumentCode,
                            Statut = "ACTIF",
                            Actif = true
                        };
                        await _unitOfWork.DictionnaireQualiteRepository.AddInstrumentAsync(instrument);
                        addedInstruments.Add(ligne.InstrumentCode);
                    }
                }

                LineCleanupHelper.CleanupPlanAssLine(ligne);
            }
        }
    }

    public async Task<ModeleResponseDto> GetModeleByIdAsync(Guid modeleId)
    {
        var assModele = await _assRepository.GetPlanAvecRelationsAsync(modeleId);
        if (assModele != null)
        {
            return PlanAssMapper.MapperEntiteVersModeleDto(assModele);
        }

        throw new ModeleIntrouvableException(modeleId);
    }

    public async Task<IReadOnlyList<ModeleResponseDto>> GetModelesByFiltersAsync(string? typeRobinetCode, string? natureComposantCode, string? operationCode, string? posteCode = null, string? familleProduitCode = null)
    {
        var assModeles = await _assRepository.GetModelesParFiltresAsync(natureComposantCode, operationCode, posteCode, familleProduitCode);
        return assModeles.Select(PlanAssMapper.MapperEntiteVersModeleDto).ToList();
    }

    public async Task<Guid> CreerNouvelleVersionModeleAsync(NouvelleVersionModeleRequestDto request)
    {
        var assModele = await _assRepository.GetPlanAvecRelationsAsync(request.AncienId);
        if (assModele != null)
        {
            var user = _currentUserService.UserInfo;
            
            var planActifActuel = await _assRepository.GetPlanActifMaitreAsync(
                assModele.OperationCode, 
                assModele.FamilleProduitFiniCode, 
                assModele.NatureArticleCode, 
                assModele.PosteCode);

            if (planActifActuel != null)
            {
                planActifActuel.Statut = StatutsPlan.Archive;
            }

            var nouvelleVersion = await _assRepository.GetDerniereVersionAsync(assModele.OperationCode ?? string.Empty, assModele.FamilleProduitFiniCode, assModele.NatureArticleCode) + 1;
            Guid? newFormulaireId = null;

            var role = assModele.OperationCode == "ASS" ? "EN_COURS_DE_ASSEMBLAGE" : "EN_COURS_DE_FABRICATION";
            var jsonToUse = request.ConfigurationColonnesJson;
            if (string.IsNullOrEmpty(jsonToUse) && assModele.FormulaireId.HasValue)
            {
                var oldForm = await _referentielService.GetFormulaireByIdAsync(assModele.FormulaireId.Value);
                if (oldForm != null) jsonToUse = oldForm.ConfigurationStructureJson;
            }

            if (!string.IsNullOrEmpty(jsonToUse) || assModele.FormulaireId.HasValue)
            {
                var formResult = await _referentielService.UpdateFormulaireStructureAsync(
                    role,
                    jsonToUse,
                    request.RefFormulaireCodeReference);
                if (formResult.HasValue)
                {
                    newFormulaireId = formResult.Value.Id;
                    nouvelleVersion = formResult.Value.Version;
                }
            }

            var nouveauPlan = PlanAssMapper.ConstruireNouvelleVersionModele(assModele, request, user, nouvelleVersion);
            nouveauPlan.Statut = StatutsPlan.Actif;
            if (newFormulaireId.HasValue) nouveauPlan.FormulaireId = newFormulaireId.Value;

            await SmartDictionaryPassAssAsync(nouveauPlan);

            await _assRepository.AddPlanAsync(nouveauPlan);
            await _unitOfWork.CommitAsync();
            return nouveauPlan.Id;
        }

        throw new ModeleIntrouvableException(request.AncienId);
    }

    public async Task<Guid> RestaurerModeleArchiveAsync(RestaurerModeleRequestDto request)
    {
        var assModele = await _assRepository.GetPlanAvecRelationsAsync(request.ModeleArchiveId);
        if (assModele != null)
        {
            var user = _currentUserService.UserInfo;
            var actif = await _assRepository.GetPlanActifMaitreAsync(assModele.OperationCode, assModele.FamilleProduitFiniCode, assModele.NatureArticleCode, assModele.PosteCode);
            if (actif != null)
            {
                actif.Statut = StatutsPlan.Archive;
            }

            var nouveauPlan = PlanAssMapper.DupliquerEntitePlan(assModele, true, null, null, user, $"[Restauré] {request.MotifRestoration}");
            nouveauPlan.Statut = StatutsPlan.Actif;
            
            nouveauPlan.Version = await _assRepository.GetDerniereVersionAsync(assModele.OperationCode ?? string.Empty, assModele.FamilleProduitFiniCode, assModele.NatureArticleCode) + 1;

            if (assModele.FormulaireId.HasValue)
            {
                nouveauPlan.FormulaireId = assModele.FormulaireId;
            }

            await SmartDictionaryPassAssAsync(nouveauPlan);

            await _assRepository.AddPlanAsync(nouveauPlan);
            await _unitOfWork.CommitAsync();
            return nouveauPlan.Id;
        }

        throw new ModeleIntrouvableException(request.ModeleArchiveId);
    }

    public Task UpdateModeleBrouillonAsync(Guid id, CreateModeleRequestDto request)
        => throw new NotSupportedException("Les modèles d'assemblage n'utilisent pas le statut BROUILLON.");

    public Task ActiverModeleAsync(Guid id, string user)
        => Task.CompletedTask;

    public async Task<bool> SupprimerBrouillonAsync(Guid id)
    {
        return false;
    }
}
