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

/// <summary>
/// Service de gestion des Modèles de Fabrication.
/// Plus de logique de BROUILLON ici (directement ACTIF ou ARCHIVE).
/// </summary>
public class ModeleFabricationService : IModeleFabricationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlanFabricationRepository _fabRepository;
    private readonly IPlanAssRepository _assRepository;
    private readonly IReferentielService _referentielService;
    private readonly IValidator<CreateModeleRequestDto> _modeleValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ModeleFabricationService> _logger;

    public ModeleFabricationService(
        IUnitOfWork unitOfWork,
        IPlanFabricationRepository fabRepository,
        IPlanAssRepository assRepository,
        IReferentielService referentielService,
        IValidator<CreateModeleRequestDto> modeleValidator,
        ICurrentUserService currentUserService,
        ILogger<ModeleFabricationService> logger)
    {
        _unitOfWork = unitOfWork;
        _fabRepository = fabRepository;
        _assRepository = assRepository;
        _referentielService = referentielService;
        _modeleValidator = modeleValidator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    private bool IsAssemblage(string? operationCode, string? natureComposantCode, string? typeRobinetCode)
    {
        if (operationCode == "ASS") return true;
        
        var nat = natureComposantCode?.Trim().ToUpper();
        if (nat == "PISTON" || nat == "PF") return true;

        var type = typeRobinetCode?.Trim().ToUpper();
        if (type == "PISTON") return true;

        return false;
    }

    public async Task<Guid> CreerModeleAsync(CreateModeleRequestDto request)
    {
        var validationResult = await _modeleValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var user = _currentUserService.UserInfo;

        if (IsAssemblage(request.OperationCode, request.NatureComposantCode, request.TypeRobinetCode))
        {
            // --- LOGIQUE ASSEMBLAGE ---
            // Vérification par Code + Libellé
            if (await _assRepository.ExisteParCodeEtLibelleAsync(request.Code, request.Libelle))
            {
                throw new DoublonCodeModeleException($"{request.Code} avec le libellé '{request.Libelle}'");
            }

            var modele = PlanAssMapper.MapperModeleVersEntite(request, user);
            modele.Statut = StatutsPlan.Actif;
            modele.Version = request.VersionInitiale ?? 0; // Commencer par Version Initiale ou 0

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

            await SmartDictionaryPassAssAsync(modele);

            // Nettoyage final des GUIDs vides pour éviter les erreurs de clés étrangères
            foreach (var s in modele.PlanAssemblageSections)
            {
                foreach (var l in s.PlanAssemblageLignes)
                {
                    LineCleanupHelper.CleanupPlanAssLine(l);
                }
            }

            await _assRepository.AddPlanAsync(modele);
            await _unitOfWork.CommitAsync();
            return modele.Id;
        }
        else
        {
            // --- LOGIQUE FABRICATION ---
            // Validation de la Gamme Opératoire
            if (!await _fabRepository.IsOperationValidePourNatureAsync(request.NatureComposantCode, request.OperationCode))
            {
                throw new ValidationException($"L'opération '{request.OperationCode}' n'est pas autorisée pour un article de nature '{request.NatureComposantCode}'.");
            }

            // Vérification par Code + Libellé
            if (await _fabRepository.ExisteModeleParCodeEtLibelleAsync(request.Code, request.Libelle))
            {
                throw new DoublonCodeModeleException($"{request.Code} avec le libellé '{request.Libelle}'");
            }

            var modele = ModeleFabricationMapper.ConstruireEntiteModeleAPartirDeDto(request);
            modele.CreePar = user; // Forcer l'utilisateur connecté
            modele.Statut = StatutsPlan.Actif;
            modele.Version = request.VersionInitiale ?? 0; // Commencer par Version Initiale ou 0

            if (!string.IsNullOrEmpty(request.ConfigurationColonnesJson))
            {
                var formResult = await _referentielService.UpdateFormulaireStructureAsync(
                    "EN_COURS_DE_FABRICATION",
                    request.ConfigurationColonnesJson,
                    request.RefFormulaireCodeReference,
                    request.VersionInitiale);
                if (formResult.HasValue)
                {
                    modele.FormulaireId = formResult.Value.Id;
                }
            }

            await SmartDictionaryPassAsync(modele);

            // Nettoyage final des GUIDs vides pour éviter les erreurs de clés étrangères
            foreach (var s in modele.ModeleFabricationSections)
            {
                foreach (var l in s.ModeleFabricationLignes)
                {
                    LineCleanupHelper.CleanupModeleFabLine(l);
                }
            }

            await _fabRepository.AddModeleAsync(modele);
            await _unitOfWork.CommitAsync();
            return modele.Id;
        }
    }

    private async Task SmartDictionaryPassAsync(ModeleFabricationEntete modele)
    {
        var addedCaracs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedInstruments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedMoyens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sec in modele.ModeleFabricationSections)
        {
            foreach (var ligne in sec.ModeleFabricationLignes)
            {
                // Nettoyage des GUIDs et chaînes vides pour éviter les erreurs de clés étrangères
                //if (ligne.TypeCaracteristiqueId == Guid.Empty) ligne.TypeCaracteristiqueId = null;
                //if (ligne.TypeControleId == Guid.Empty) ligne.TypeControleId = null;
                if (ligne.MoyenControleId == Guid.Empty) ligne.MoyenControleId = null;
                //if (ligne.PeriodiciteId == Guid.Empty) ligne.PeriodiciteId = null;

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

                // ✅ Garantir la contrainte XOR (MoyenControleId XOR MoyenTexteLibre)
                LineCleanupHelper.CleanupModeleFabLine(ligne);
            }
        }
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
                //if (ligne.TypeCaracteristiqueId == Guid.Empty) ligne.TypeCaracteristiqueId = null;
                //if (ligne.TypeControleId == Guid.Empty) ligne.TypeControleId = null;
                if (ligne.MoyenControleId == Guid.Empty) ligne.MoyenControleId = null;
                //if (ligne.PeriodiciteId == Guid.Empty) ligne.PeriodiciteId = null;

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

                // ✅ Garantir la contrainte XOR (MoyenControleId XOR MoyenTexteLibre)
                LineCleanupHelper.CleanupPlanAssLine(ligne);
            }
        }
    }

    public async Task<ModeleResponseDto> GetModeleByIdAsync(Guid modeleId)
    {
        // On essaie d'abord en Assemblage
        var assModele = await _assRepository.GetPlanAvecRelationsAsync(modeleId);
        if (assModele != null)
        {
            return PlanAssMapper.MapperEntiteVersModeleDto(assModele);
        }

        // Sinon en Fabrication
        var fabModele = await _fabRepository.GetModeleAvecRelationsAsync(modeleId);
        if (fabModele != null)
        {
            return ModeleFabricationMapper.MapperEntiteModeleVersDto(fabModele);
        }

        throw new ModeleIntrouvableException(modeleId);
    }

    public async Task<IReadOnlyList<ModeleResponseDto>> GetModelesByFiltersAsync(string? typeRobinetCode, string? natureComposantCode, string? operationCode, string? posteCode = null, string? familleProduitCode = null)
    {
        var result = new List<ModeleResponseDto>();

        bool isAss = IsAssemblage(operationCode, natureComposantCode, typeRobinetCode);

        // Si l'opération est ASS, PISTON, PF ou non spécifiée, on cherche dans l'assemblage
        if (string.IsNullOrEmpty(operationCode) || isAss)
        {
            var assModeles = await _assRepository.GetModelesParFiltresAsync(natureComposantCode, operationCode, posteCode, familleProduitCode);
            result.AddRange(assModeles.Select(PlanAssMapper.MapperEntiteVersModeleDto));
        }

        // Si ce n'est pas de l'assemblage (ou non spécifié), on cherche dans la fabrication
        if (string.IsNullOrEmpty(operationCode) || !isAss)
        {
            var fabModeles = await _fabRepository.GetModelesParFiltresAsync(natureComposantCode, operationCode);
            result.AddRange(fabModeles.Select(ModeleFabricationMapper.MapperEntiteModeleVersDto));
        }

        return result;
    }

    public async Task<Guid> CreerNouvelleVersionModeleAsync(NouvelleVersionModeleRequestDto request)
    {
        // On doit savoir si c'est un modèle d'assemblage ou de fabrication
        var assModele = await _assRepository.GetPlanAvecRelationsAsync(request.AncienId);
        if (assModele != null)
        {
            var user = _currentUserService.UserInfo;
            
            // ✅ Trouver l'actuel plan actif pour cette combinaison (indépendamment du fait qu'on versionne l'actif ou un archivé)
            var planActifActuel = await _assRepository.GetPlanActifMaitreAsync(
                assModele.OperationCode, 
                assModele.FamilleProduitFiniCode, 
                assModele.NatureArticleCode, 
                assModele.PosteCode);

            if (planActifActuel != null)
            {
                planActifActuel.Statut = StatutsPlan.Archive;
                //planActifActuel.ArchiveLe = DateTime.UtcNow;
                //planActifActuel.ArchivePar = user;
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

        var fabModele = await _fabRepository.GetModeleAvecRelationsAsync(request.AncienId);
        if (fabModele != null)
        {
            var user = _currentUserService.UserInfo;
            if (fabModele.Statut == StatutsPlan.Actif)
            {
                fabModele.Statut = StatutsPlan.Archive;
                //fabModele.ArchiveLe = DateTime.UtcNow;
                //fabModele.ArchivePar = user;
            }

            var nouvelleVersion = await _fabRepository.GetDerniereVersionModeleAsync(fabModele.NatureArticleCode, fabModele.OperationCode) + 1;
            Guid? newFormulaireId = null;

            var jsonToUse = request.ConfigurationColonnesJson;
            if (string.IsNullOrEmpty(jsonToUse) && fabModele.FormulaireId.HasValue)
            {
                var oldForm = await _referentielService.GetFormulaireByIdAsync(fabModele.FormulaireId.Value);
                if (oldForm != null) jsonToUse = oldForm.ConfigurationStructureJson;
            }

            if (!string.IsNullOrEmpty(jsonToUse) || fabModele.FormulaireId.HasValue)
            {
                var formResult = await _referentielService.UpdateFormulaireStructureAsync(
                    "EN_COURS_DE_FABRICATION",
                    jsonToUse,
                    request.RefFormulaireCodeReference);
                if (formResult.HasValue)
                {
                    newFormulaireId = formResult.Value.Id;
                    nouvelleVersion = formResult.Value.Version;
                }
            }

            var nouveauModele = ModeleFabricationMapper.ConstruireNouvelleVersionModele(fabModele, request, user, nouvelleVersion);
            nouveauModele.Statut = StatutsPlan.Actif;
            if (newFormulaireId.HasValue) nouveauModele.FormulaireId = newFormulaireId.Value;

            await SmartDictionaryPassAsync(nouveauModele);

            await _fabRepository.AddModeleAsync(nouveauModele);
            await _unitOfWork.CommitAsync();
            return nouveauModele.Id;
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
                //actif.ArchiveLe = DateTime.UtcNow;
                //actif.ArchivePar = user;
            }

            var nouveauPlan = PlanAssMapper.DupliquerEntitePlan(assModele, true, null, null, user, $"[Restauré] {request.MotifRestoration}");
            nouveauPlan.Statut = StatutsPlan.Actif;
            
            nouveauPlan.Version = await _assRepository.GetDerniereVersionAsync(assModele.OperationCode ?? string.Empty, assModele.FamilleProduitFiniCode, assModele.NatureArticleCode) + 1;

            if (assModele.FormulaireId.HasValue)
            {
                var oldForm = await _referentielService.GetFormulaireByIdAsync(assModele.FormulaireId.Value);
                if (oldForm != null)
                {
                    var role = assModele.OperationCode == "ASS" ? "EN_COURS_DE_ASSEMBLAGE" : "EN_COURS_DE_FABRICATION";
                    var formResult = await _referentielService.UpdateFormulaireStructureAsync(
                        role,
                        oldForm.ConfigurationStructureJson,
                        oldForm.CodeReference);
                    
                    if (formResult.HasValue)
                    {
                        nouveauPlan.FormulaireId = formResult.Value.Id;
                        nouveauPlan.Version = formResult.Value.Version;
                    }
                }
            }

            await SmartDictionaryPassAssAsync(nouveauPlan);

            await _assRepository.AddPlanAsync(nouveauPlan);
            await _unitOfWork.CommitAsync();
            return nouveauPlan.Id;
        }

        var fabModele = await _fabRepository.GetModeleAvecRelationsAsync(request.ModeleArchiveId);
        if (fabModele != null)
        {
            var user = _currentUserService.UserInfo;
            var actif = await _fabRepository.GetModeleActifPourFamilleAsync(fabModele.NatureArticleCode, fabModele.OperationCode);
            if (actif != null)
            {
                actif.Statut = StatutsPlan.Archive;
                //actif.ArchiveLe = DateTime.UtcNow;
                //actif.ArchivePar = user;
            }

            var nouvelleVersion = await _fabRepository.GetDerniereVersionModeleAsync(fabModele.NatureArticleCode, fabModele.OperationCode) + 1;
            Guid? newFormulaireId = null;

            if (fabModele.FormulaireId.HasValue)
            {
                var oldForm = await _referentielService.GetFormulaireByIdAsync(fabModele.FormulaireId.Value);
                if (oldForm != null)
                {
                    var formResult = await _referentielService.UpdateFormulaireStructureAsync(
                        "EN_COURS_DE_FABRICATION",
                        oldForm.ConfigurationStructureJson,
                        oldForm.CodeReference);
                    
                    if (formResult.HasValue)
                    {
                        newFormulaireId = formResult.Value.Id;
                        nouvelleVersion = formResult.Value.Version;
                    }
                }
            }

            var nouveauModele = ModeleFabricationMapper.RestaurerEntiteModele(fabModele, user, request.MotifRestoration, nouvelleVersion);
            if (newFormulaireId.HasValue) nouveauModele.FormulaireId = newFormulaireId.Value;
            
            await SmartDictionaryPassAsync(nouveauModele);

            await _fabRepository.AddModeleAsync(nouveauModele);
            await _unitOfWork.CommitAsync();
            return nouveauModele.Id;
        }

        throw new ModeleIntrouvableException(request.ModeleArchiveId);
    }

    public Task UpdateModeleBrouillonAsync(Guid id, CreateModeleRequestDto request)
        => throw new NotSupportedException("Les modèles sont gérés en direct (pas de brouillon).");

    public Task ActiverModeleAsync(Guid id, string user)
        => Task.CompletedTask;

    public async Task<bool> SupprimerBrouillonAsync(Guid id)
    {
        var assModele = await _assRepository.GetPlanAvecRelationsAsync(id);
        if (assModele != null && assModele.Statut == StatutsPlan.Brouillon)
        {
            // Note: En ASS on ne supprime pas forcément via repo mais via UnitOfWork si besoin
            return false; 
        }

        var fabModele = await _fabRepository.GetModeleAvecRelationsAsync(id);
        if (fabModele != null && fabModele.Statut == StatutsPlan.Brouillon)
        {
            _fabRepository.DeleteModele(fabModele);
            await _unitOfWork.CommitAsync();
            return true;
        }

        return false;
    }
}
