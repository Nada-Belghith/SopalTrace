using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
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
        var user = _currentUserService.MatriculeRequis;

        var docsExistants = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByMachineCodeAsync(request.MachineCode);

        bool forceNouvelleVersion = docsExistants.Any(d => d.Statut == DocumentStatuts.Actif);

        // 1. Gérer le formulaire
        var (formulaireId, formVersion, formStatut) = await ResolveFormulaireAsync(
            request.RefFormulaireCodeReference, request.MachineCode, request.ColonneDefs,
            isCorrectionMineure: false, forceNouvelleVersion: forceNouvelleVersion);

        // 2. Construire l'entité
        var entite = DocumentVerifMachineMapper.ToEntity(request, user, formulaireId);
        entite.Statut = formStatut;
        entite.Version = formVersion ?? 1;
        entite.Nom = UpdateVersionInString(entite.Nom, entite.Version);

        // 3. Sauvegarder
        await _unitOfWork.DocumentVerifMachineEnteteRepository.AddAsync(entite);
        await _unitOfWork.CommitAsync();

        return entite.Id;
    }

    public async Task<Guid> CreateNewVersionAsync(NouvelleVersionVerifMachineRequestDto request)
    {
        var ancienDoc = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByIdAsync(request.AncienId, includeRelations: true);
        if (ancienDoc == null) throw new RessourceIntrouvableException("Ancien plan introuvable.");
        _unitOfWork.VerifierVersionDocument(ancienDoc, request.RowVersion, obligatoire: false);

        var user = _currentUserService.MatriculeRequis;

        // Le FormulaireStructureService gère l'archivage automatique des documents liés
        // lorsque le formulaire passe à une nouvelle version.

        // 1. Gérer le formulaire (incrémentation de version du formulaire)
        var (formulaireId, formVersion, formStatut) = await ResolveFormulaireAsync(
            request.Donnees.RefFormulaireCodeReference ?? ancienDoc.Formulaire?.CodeReference, request.Donnees.MachineCode, request.Donnees.ColonneDefs,
            isCorrectionMineure: false, forceNouvelleVersion: true);

        // 2. Construire
        var entite = DocumentVerifMachineMapper.ToEntity(request.Donnees, user, formulaireId);
        entite.Statut = formStatut;
        entite.Version = formVersion ?? 1;
        entite.Nom = UpdateVersionInString(entite.Nom, entite.Version);

        // 3. Sauvegarder
        await _unitOfWork.DocumentVerifMachineEnteteRepository.AddAsync(entite);
        await _unitOfWork.CommitAsync();

        return entite.Id;
    }

    public async Task<string> UpdateDocumentAsync(Guid id, UpdateDocumentVerifMachineRequestDto request)
    {
        var existingDoc = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (existingDoc == null) throw new RessourceIntrouvableException("Plan introuvable");
        _unitOfWork.VerifierVersionDocument(existingDoc, request.RowVersion, obligatoire: true);

        var user = _currentUserService.MatriculeRequis;

        // 1. Gérer le formulaire (correction mineure)
        var (formulaireId, formVersion, formStatut) = await ResolveFormulaireAsync(
            request.RefFormulaireCodeReference ?? existingDoc.Formulaire?.CodeReference, request.MachineCode, request.ColonneDefs, isCorrectionMineure: true);

        // 2. Mettre à jour l'en-tête (ne modifie ni la version ni le statut)
        existingDoc.MachineCode = request.MachineCode;
        existingDoc.Nom = UpdateVersionInString(request.Nom, existingDoc.Version);
        existingDoc.Remarques = request.Remarques;
        existingDoc.LegendeMoyens = request.LegendeMoyens;
        existingDoc.ModifiePar = user;
        existingDoc.ModifieLe = DateTime.Now;
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
            f.EnteteId = existingDoc.Id;
            _unitOfWork.DocumentVerifMachineEnteteRepository.AddFamille(f);
        }

        // 4. Vider et recréer les relations (Lignes)
        foreach (var l in existingDoc.DocumentLignes.ToList())
        {
            _unitOfWork.DocumentVerifMachineEnteteRepository.RemoveLigne(l);
        }
        existingDoc.DocumentLignes.Clear();

        foreach (var l in newEntity.DocumentLignes)
        {
            l.EnteteId = existingDoc.Id;
            _unitOfWork.DocumentVerifMachineEnteteRepository.AddLigne(l);
        }

        // 5. Sauvegarder
        await _unitOfWork.DocumentVerifMachineEnteteRepository.UpdateAsync(existingDoc);
        await _unitOfWork.CommitAsync();
        return VersionsDocument.EnTexte(existingDoc.RowVersion) ?? string.Empty;
    }

    /// <summary>
    /// Formulaire du plan : celui indiqué, sinon celui de la machine (champ d'application) ; jamais de code fabriqué.
    /// </summary>
    private async Task<(Guid? FormulaireId, int? Version, string Statut)> ResolveFormulaireAsync(
        string? refFormulaireCodeReference,
        string? machineCode,
        IEnumerable<object>? colonneDefs,
        bool isCorrectionMineure = false,
        bool forceNouvelleVersion = false)
    {
        Guid? formulaireId = null;
        int version = 0;
        string statut = DocumentStatuts.Brouillon;

        var champ = ChampsFormulaire.PourDocument(TypesDocument.VerifMachine, null, null, machineCode, null)!;
        refFormulaireCodeReference = await _formulaireStructureService.CodeFormulaireAsync(champ, refFormulaireCodeReference);
        if (!string.IsNullOrWhiteSpace(refFormulaireCodeReference))
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(refFormulaireCodeReference);
            string role = form?.Role ?? RolesFormulaire.VerifMachine;

            var colsJson = colonneDefs != null && colonneDefs.Any()
                ? System.Text.Json.JsonSerializer.Serialize(colonneDefs, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                : null;

            var result = await _formulaireStructureService.UpdateFormulaireStructureAsync(
                role, colsJson, refFormulaireCodeReference, isCorrectionMineure: isCorrectionMineure, forceNouvelleVersion: forceNouvelleVersion
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
                statut = finalForm.Statut ?? DocumentStatuts.Brouillon;
            }
        }

        // Un plan de vérification machine est générique (une machine, pas un article) : pas de brouillon,
        // actif dès sa création puis archivé par une nouvelle version.
        if (statut == DocumentStatuts.Brouillon)
        {
            if (formulaireId.HasValue) await _formulaireStructureService.ActiverFormulaireAsync(formulaireId.Value);
            statut = DocumentStatuts.Actif;
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
        if (entite == null) throw new RessourceIntrouvableException("Plan Vérification Machine introuvable.");

        var dto = DocumentVerifMachineMapper.ToDto(entite);
        await CompleterConfigurationColonnesAsync(entite, dto);

        return dto;
    }

    public async Task<IEnumerable<DocumentVerifMachineEnteteDto>> GetAllDocumentsAsync()
    {
        var plans = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetAllWithRelationsAsync();
        var dtos = new List<DocumentVerifMachineEnteteDto>();
        foreach (var entite in plans)
        {
            var dto = DocumentVerifMachineMapper.ToDto(entite);
            await CompleterConfigurationColonnesAsync(entite, dto);
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
            await CompleterConfigurationColonnesAsync(entite, dto);
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

    /// <summary>Ajoute au DTO la configuration des colonnes actives du formulaire associé au plan.</summary>
    private async Task CompleterConfigurationColonnesAsync(DocumentEntete entite, DocumentVerifMachineEnteteDto dto)
    {
        if (entite.FormulaireId is not Guid formulaireId) return;

        var cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(formulaireId);
        dto.ConfigurationColonnesJson = SopalTrace.Application.Helpers.ColonneJsonMapper.Serialize(cols);
    }
}
