using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
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

    public async Task<Guid> CreerDocumentVerifMachineAsync(CreateDocumentVerifMachineRequestDto request)
    {
        return await CreerOuMettreAJourPlanAsync(request, null);
    }

    public async Task MettreAJourDocumentVerifMachineAsync(Guid id, UpdateDocumentVerifMachineRequestDto request)
    {
        var existing = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByIdAsync(id);
        if (existing == null) throw new Exception("Plan introuvable");

        await CreerOuMettreAJourPlanAsync(request, id);
    }

    private async Task<Guid> CreerOuMettreAJourPlanAsync(CreateDocumentVerifMachineRequestDto request, Guid? id)
    {
        var user = _currentUserService.UserInfo;

        DocumentVerifMachineEntete? existingDoc = null;

        if (id.HasValue)
        {
            existingDoc = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByIdAsync(id.Value, includeRelations: true);
        }
        else
        {
            var existingDocs = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByMachineCodeAsync(request.MachineCode);
            string baseNom = RemoveVersionSuffix(request.Nom).TrimEnd('-').Trim();
            existingDoc = existingDocs.Where(d => RemoveVersionSuffix(d.Nom).TrimEnd('-').Trim() == baseNom)
                                      .OrderByDescending(d => d.Version)
                                      .FirstOrDefault();
            
            if (existingDoc != null)
            {
                existingDoc = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByIdAsync(existingDoc.Id, includeRelations: true);
            }
        }

        int finalVersion = existingDoc?.Version ?? 1;
        string finalStatut = existingDoc?.Statut ?? "BROUILLON";
        Guid? formulaireId = existingDoc?.FormulaireId;

        // Mise à jour de la structure (Ref_Formulaire)
        if (!string.IsNullOrWhiteSpace(request.RefFormulaireCodeReference))
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(request.RefFormulaireCodeReference);
            string role = form?.Role ?? "UNKNOWN";
            
            var colsJson = request.ColonneDefs != null && request.ColonneDefs.Any()
                ? System.Text.Json.JsonSerializer.Serialize(request.ColonneDefs, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                : null;
                
            var result = await _formulaireStructureService.UpdateFormulaireStructureAsync(
                role, colsJson, request.RefFormulaireCodeReference, request.VersionInitiale
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

        // Récupération de la version finale depuis la structure (Source de Vérité)
        if (formulaireId.HasValue)
        {
            var finalForm = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId.Value);
            if (finalForm != null)
            {
                finalVersion = finalForm.Version;
                finalStatut = finalForm.Statut ?? "BROUILLON";
            }
        }

        var entite = DocumentVerifMachineMapper.ToEntity(request, user, formulaireId);
        entite.Version = finalVersion;
        entite.Statut = finalStatut;
        entite.Nom = UpdateVersionInString(entite.Nom, entite.Version ?? 1);

        // Archiver tous les plans existants actifs pour cette machine
        var allExistingDocs = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByMachineCodeAsync(request.MachineCode);
        string baseNomToArchive = RemoveVersionSuffix(request.Nom).TrimEnd('-').Trim();
        var activeDocs = allExistingDocs.Where(d => RemoveVersionSuffix(d.Nom).TrimEnd('-').Trim() == baseNomToArchive && d.Statut == "ACTIF").ToList();
        
        foreach (var act in activeDocs)
        {
            act.Statut = "ARCHIVE";
            await _unitOfWork.DocumentVerifMachineEnteteRepository.UpdateAsync(act);
        }
        
        await _unitOfWork.DocumentVerifMachineEnteteRepository.AddAsync(entite);
        await _unitOfWork.CommitAsync();

        return entite.Id;
    }

    public async Task<DocumentVerifMachineEnteteDto> GetDocumentVerifMachineByIdAsync(Guid id)
    {
        var entite = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (entite == null) throw new Exception("Plan Vérification Machine introuvable.");

        var dto = DocumentVerifMachineMapper.ToDto(entite);
        if (entite.Formulaire != null)
        {
            var cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByCodeReferenceAsync(entite.Formulaire.CodeReference);
            dto.ConfigurationColonnesJson = SopalTrace.Application.Helpers.ColonneJsonMapper.Serialize(cols);
        }

        return dto;
    }

    public async Task<IEnumerable<DocumentVerifMachineEnteteDto>> GetAllPlansAsync()
    {
        var plans = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetAllWithRelationsAsync();
        var dtos = new List<DocumentVerifMachineEnteteDto>();
        foreach(var entite in plans)
        {
            var dto = DocumentVerifMachineMapper.ToDto(entite);
            if (entite.Formulaire != null)
            {
                var cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByCodeReferenceAsync(entite.Formulaire.CodeReference);
                dto.ConfigurationColonnesJson = SopalTrace.Application.Helpers.ColonneJsonMapper.Serialize(cols);
            }
            dtos.Add(dto);
        }
        return dtos;
    }

    public async Task<IEnumerable<DocumentVerifMachineEnteteDto>> GetPlansByMachineCodeAsync(string machineCode)
    {
        var plans = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByMachineCodeAsync(machineCode);
        var dtos = new List<DocumentVerifMachineEnteteDto>();
        foreach(var entite in plans)
        {
            var dto = DocumentVerifMachineMapper.ToDto(entite);
            if (entite.Formulaire != null)
            {
                var cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByCodeReferenceAsync(entite.Formulaire.CodeReference);
                dto.ConfigurationColonnesJson = SopalTrace.Application.Helpers.ColonneJsonMapper.Serialize(cols);
            }
            dtos.Add(dto);
        }
        return dtos;
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

    public async Task ArchiverPlansByFormulaireAsync(Guid formulaireId)
    {
        var plans = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByFormulaireIdAsync(formulaireId);
        foreach (var plan in plans.Where(p => p.Statut == "ACTIF"))
        {
            plan.Statut = "ARCHIVE";
            await _unitOfWork.DocumentVerifMachineEnteteRepository.UpdateAsync(plan);
        }
    }
}
