using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class DocumentVerifMachineService : IDocumentVerifMachineService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DocumentVerifMachineService> _logger;
    private readonly IFormulaireStructureService _formulaireStructureService;

    public DocumentVerifMachineService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DocumentVerifMachineService> logger,
        IFormulaireStructureService formulaireStructureService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
        _formulaireStructureService = formulaireStructureService;
    }

    public async Task<Guid> CreateDocumentAsync(CreateDocumentVerifMachineRequestDto request)
    {
        var user = _currentUserService.UserInfo ?? "";
        
        var docsExistants = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByMachineCodeAsync(request.MachineCode);
        
        bool forceNouvelleVersion = docsExistants.Any(d => d.Statut == DocumentStatuts.Actif);

        // 1. Gérer le formulaire
        var (formulaireId, formVersion, formStatut) = await ResolveFormulaireAsync(
            request.RefFormulaireCodeReference, request.ColonneDefs, 
            isCorrectionMineure: false, forceNouvelleVersion: forceNouvelleVersion);
        
        // 2. Construire l'entité
        var entite = DocumentVerifMachineMapper.ToEntity(request, user, formulaireId);
        entite.Statut = formStatut;
        entite.Version = formVersion;
        entite.Nom = UpdateVersionInString(entite.Nom, entite.Version.Value);

        // 3. Sauvegarder
        await _unitOfWork.DocumentVerifMachineEnteteRepository.AddAsync(entite);
        await _unitOfWork.CommitAsync();

        return entite.Id;
    }

    public async Task<Guid> CreateNewVersionAsync(NouvelleVersionVerifMachineRequestDto request)
    {
        var ancienDoc = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByIdAsync(request.AncienId, includeRelations: true);
        if (ancienDoc == null) throw new Exception("Ancien plan introuvable.");

        var user = _currentUserService.UserInfo ?? "";

        // Le FormulaireStructureService gère l'archivage automatique des documents liés
        // lorsque le formulaire passe à une nouvelle version.

        // 1. Gérer le formulaire (incrémentation de version du formulaire)
        var (formulaireId, formVersion, formStatut) = await ResolveFormulaireAsync(
            request.Donnees.RefFormulaireCodeReference, request.Donnees.ColonneDefs, 
            versionInitiale: 0, isCorrectionMineure: false, forceNouvelleVersion: true);

        // 2. Construire
        var entite = DocumentVerifMachineMapper.ToEntity(request.Donnees, user, formulaireId);
        entite.Statut = formStatut;
        entite.Version = formVersion;
        entite.Nom = UpdateVersionInString(entite.Nom, entite.Version.Value);

        // 3. Sauvegarder
        await _unitOfWork.DocumentVerifMachineEnteteRepository.AddAsync(entite);
        await _unitOfWork.CommitAsync();

        return entite.Id;
    }

    public async Task UpdateDocumentAsync(Guid id, UpdateDocumentVerifMachineRequestDto request)
    {
        var existingDoc = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (existingDoc == null) throw new Exception("Plan introuvable");

        var user = _currentUserService.UserInfo ?? "";

        // 1. Gérer le formulaire (correction mineure)
        var (formulaireId, formVersion, formStatut) = await ResolveFormulaireAsync(request.RefFormulaireCodeReference, request.ColonneDefs, null, isCorrectionMineure: true);

        // 2. Mettre à jour l'en-tête (ne modifie ni la version ni le statut)
        existingDoc.MachineCode = request.MachineCode;
        existingDoc.Nom = UpdateVersionInString(request.Nom, existingDoc.Version ?? 1);
        existingDoc.Remarques = request.Remarques;
        existingDoc.LegendeMoyens = request.LegendeMoyens;
        existingDoc.ModifiePar = user;
        existingDoc.ModifieLe = DateTime.UtcNow;
        existingDoc.FormulaireId = formulaireId;

        var newEntity = DocumentVerifMachineMapper.ToEntity(request, user, formulaireId);

        // 3. Vider et recréer les relations (Familles)
        foreach (var f in existingDoc.DocumentVerifMachineFamilles.ToList())
        {
            _unitOfWork.DocumentVerifMachineEnteteRepository.RemoveFamille(f);
        }
        existingDoc.DocumentVerifMachineFamilles.Clear();

        foreach (var f in newEntity.DocumentVerifMachineFamilles)
        {
            f.PlanEnteteId = existingDoc.Id;
            _unitOfWork.DocumentVerifMachineEnteteRepository.AddFamille(f);
        }

        // 4. Vider et recréer les relations (Lignes)
        foreach (var l in existingDoc.DocumentVerifMachineLignes.ToList())
        {
            _unitOfWork.DocumentVerifMachineEnteteRepository.RemoveLigne(l);
        }
        existingDoc.DocumentVerifMachineLignes.Clear();

        foreach (var l in newEntity.DocumentVerifMachineLignes)
        {
            l.PlanEnteteId = existingDoc.Id;
            _unitOfWork.DocumentVerifMachineEnteteRepository.AddLigne(l);
        }

        // 5. Sauvegarder
        await _unitOfWork.DocumentVerifMachineEnteteRepository.UpdateAsync(existingDoc);
        await _unitOfWork.CommitAsync();
    }

    private async Task<(Guid? FormulaireId, int? Version, string Statut)> ResolveFormulaireAsync(
        string? refFormulaireCodeReference, 
        IEnumerable<object>? colonneDefs, 
        int? versionInitiale = null, 
        bool isCorrectionMineure = false,
        bool forceNouvelleVersion = false)
    {
        Guid? formulaireId = null;
        int version = 0;
        string statut = "BROUILLON";

        if (!string.IsNullOrWhiteSpace(refFormulaireCodeReference))
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(refFormulaireCodeReference);
            string role = form?.Role ?? "UNKNOWN";
            
            var colsJson = colonneDefs != null && colonneDefs.Any()
                ? System.Text.Json.JsonSerializer.Serialize(colonneDefs, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                : null;
                
            var result = await _formulaireStructureService.UpdateFormulaireStructureAsync(
                role, colsJson, refFormulaireCodeReference, versionInitiale, isCorrectionMineure: isCorrectionMineure, forceNouvelleVersion: forceNouvelleVersion
            );

            if (result.HasValue)
            {
                formulaireId = result.Value.Id;
            }
            else if (form != null)
            {
                formulaireId = form.Id;
            }
        }

        if (formulaireId.HasValue)
        {
            var finalForm = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId.Value);
            if (finalForm != null)
            {
                version = finalForm.Version;
                statut = finalForm.Statut ?? "BROUILLON";
            }
        }

        return (formulaireId, version, statut);
    }

    private string RemoveVersionSuffix(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        int indexV = text.LastIndexOf("- V", StringComparison.OrdinalIgnoreCase);
        if (indexV >= 0)
        {
            return text.Substring(0, indexV).Trim();
        }
        return text;
    }

    private string UpdateVersionInString(string text, int newVersion)
    {
        string baseText = RemoveVersionSuffix(text);
        return $"{baseText} - V{newVersion}";
    }

    public async Task<DocumentVerifMachineEnteteDto> GetDocumentByIdAsync(Guid id)
    {
        var entite = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (entite == null) throw new Exception("Plan Vérification Machine introuvable.");

        var dto = DocumentVerifMachineMapper.ToDto(entite);
        if (entite.Formulaire != null)
        {
            var cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(entite.FormulaireId.Value);
            dto.ConfigurationColonnesJson = SopalTrace.Application.Helpers.ColonneJsonMapper.Serialize(cols);
        }

        return dto;
    }

    public async Task<IEnumerable<DocumentVerifMachineEnteteDto>> GetAllDocumentsAsync()
    {
        var plans = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetAllWithRelationsAsync();
        var dtos = new List<DocumentVerifMachineEnteteDto>();
        foreach (var entite in plans)
        {
            var dto = DocumentVerifMachineMapper.ToDto(entite);
            if (entite.Formulaire != null)
            {
                var cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(entite.FormulaireId.Value);
                dto.ConfigurationColonnesJson = SopalTrace.Application.Helpers.ColonneJsonMapper.Serialize(cols);
            }
            dtos.Add(dto);
        }
        return dtos;
    }

    public async Task<IEnumerable<DocumentVerifMachineEnteteDto>> GetDocumentsByMachineCodeAsync(string machineCode)
    {
        var plans = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByMachineCodeAsync(machineCode);
        var dtos = new List<DocumentVerifMachineEnteteDto>();
        foreach (var entite in plans)
        {
            var dto = DocumentVerifMachineMapper.ToDto(entite);
            if (entite.Formulaire != null)
            {
                var cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(entite.FormulaireId.Value);
                dto.ConfigurationColonnesJson = SopalTrace.Application.Helpers.ColonneJsonMapper.Serialize(cols);
            }
            dtos.Add(dto);
        }
        return dtos;
    }

    public async Task ArchiveDocumentsByFormulaireAsync(Guid formulaireId)
    {
        var plans = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByFormulaireIdAsync(formulaireId);
        foreach (var plan in plans.Where(p => p.Statut == DocumentStatuts.Actif))
        {
            plan.Statut = DocumentStatuts.Archive;
            await _unitOfWork.DocumentVerifMachineEnteteRepository.UpdateAsync(plan);
        }
    }
}
