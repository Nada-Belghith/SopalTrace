using SopalTrace.Application.DTOs.QualityPlans.Echantillonnage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

/// <summary>
/// Plan d'échantillonnage : Document_Entete de type PLAN_ECHANTILLONNAGE + paramètres ISO 2859-1 (SamplingPlan).
/// Un seul plan ACTIF à la fois (garanti par TR_DocumentEntete_SingleActive).
/// </summary>
public class DocumentEchantillonnageService : IDocumentEchantillonnageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFormulaireStructureService _formulaireStructureService;
    private readonly ICurrentUserService _currentUserService;

    public DocumentEchantillonnageService(
        IUnitOfWork unitOfWork,
        IFormulaireStructureService formulaireStructureService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _formulaireStructureService = formulaireStructureService;
        _currentUserService = currentUserService;
    }

    public async Task<DocumentEchantillonnageResponseDto?> GetActiveDocumentAsync()
    {
        var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync();
        return plan.ToResponseDto();
    }

    public async Task<DocumentEchantillonnageResponseDto?> GetDocumentByIdAsync(Guid id)
    {
        var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetByIdAsync(id);
        return plan.ToResponseDto();
    }

    /// <summary>
    /// Crée un plan directement ACTIF, sur le formulaire d'échantillonnage (celui indiqué, sinon le formulaire en cours).
    /// S'il existe déjà un plan actif, le formulaire passe à la version suivante et l'ancien plan est archivé.
    /// </summary>
    public async Task<Guid> CreateDocumentAsync(CreateDocumentEchantillonnageRequestDto request)
    {
        var planActif = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync();
        var formulaire = await ResoudreFormulaireAsync(request.RefFormulaireCodeReference, forceNouvelleVersion: planActif != null);
        var niveauQualiteAcceptableId = await ResoudreNiveauQualiteAcceptableAsync(request.NqaId, request.ValeurNqa);

        var plan = DocumentEchantillonnageMapper.NouveauPlan(request, niveauQualiteAcceptableId);
        return await RemplacerPlanActifAsync(planActif, plan, formulaire);
    }

    /// <summary>Active un plan BROUILLON (endpoint de secours) en archivant le plan actif.</summary>
    public async Task ActivateDocumentAsync(Guid id)
    {
        var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetByIdAsync(id)
            ?? throw new RessourceIntrouvableException("Plan d'échantillonnage introuvable.");
        if (plan.Statut != StatutsPlan.Brouillon)
            throw new RegleMetierException($"Seul un plan BROUILLON peut être activé (statut actuel : {plan.Statut}).");

        var planActif = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (planActif != null && planActif.Id != id)
            {
                planActif.Statut = StatutsPlan.Archive;
                await _unitOfWork.FlushDeletesAsync();
            }

            plan.Statut = StatutsPlan.Actif;

            var formulaire = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByRoleAsync(RolesFormulaire.Echantillonnage);
            if (formulaire != null && formulaire.Statut != StatutsPlan.Actif)
            {
                formulaire.Statut = StatutsPlan.Actif;
                await _unitOfWork.RefFormulaireRepository.UpdateAsync(formulaire);
            }
            return plan.Id;
        });
    }

    /// <summary>Correction du plan en place (sans changer la version ni le statut).</summary>
    public async Task<string> UpdateDocumentAsync(Guid id, UpdateDocumentEchantillonnageRequestDto request)
    {
        var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetByIdAsync(id)
            ?? throw new RessourceIntrouvableException("Plan d'échantillonnage introuvable.");
        if (plan.Statut == StatutsPlan.Archive)
            throw new RegleMetierException("Impossible de modifier un plan archivé.");
        _unitOfWork.VerifierVersionDocument(plan, request.RowVersion, obligatoire: true);

        plan.UpdateEntity(request, await ResoudreNiveauQualiteAcceptableAsync(request.NqaId, request.ValeurNqa));

        await _formulaireStructureService.UpdateFormulaireStructureAsync(
            RolesFormulaire.Echantillonnage, null, await CodeFormulaireAsync(request.RefFormulaireCodeReference),
            isCorrectionMineure: true, forceNouvelleVersion: false);

        await _unitOfWork.CommitAsync();
        return VersionsDocument.EnTexte(plan.RowVersion) ?? string.Empty;
    }

    /// <summary>Nouvelle version : le formulaire passe en vN+1, l'ancien plan est archivé, le nouveau est ACTIF.</summary>
    public async Task<Guid> CreateNewVersionAsync(CreateNewVersionDocumentEchantillonnageRequestDto request)
    {
        var ancienPlan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetByIdAsync(request.AncienId)
            ?? throw new RessourceIntrouvableException("L'ancien plan d'échantillonnage est introuvable.");
        if (ancienPlan.Statut == StatutsPlan.Archive)
            throw new RegleMetierException("Ce plan est déjà archivé.");
        _unitOfWork.VerifierVersionDocument(ancienPlan, request.RowVersion, obligatoire: false);

        var formulaire = await ResoudreFormulaireAsync(request.Donnees.RefFormulaireCodeReference, forceNouvelleVersion: true);
        var niveauQualiteAcceptableId = await ResoudreNiveauQualiteAcceptableAsync(request.Donnees.NqaId, request.Donnees.ValeurNqa);

        var plan = DocumentEchantillonnageMapper.NouveauPlan(request.Donnees, niveauQualiteAcceptableId, request.MotifModification);
        return await RemplacerPlanActifAsync(ancienPlan, plan, formulaire);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Archive l'ancien plan puis enregistre le nouveau, dans une transaction : l'archivage est écrit avant l'insertion
    /// pour respecter la règle « un seul plan actif », et un échec annule l'ensemble.
    /// </summary>
    private async Task<Guid> RemplacerPlanActifAsync(DocumentEntete? ancienPlan, DocumentEntete nouveauPlan, RefFormulaire formulaire)
    {
        nouveauPlan.FormulaireId = formulaire.Id;
        nouveauPlan.Version = formulaire.Version;
        nouveauPlan.Nom = DocumentEchantillonnageMapper.Nom(formulaire.Version);
        nouveauPlan.Statut = StatutsPlan.Actif;
        nouveauPlan.CreePar = _currentUserService.MatriculeRequis;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (ancienPlan != null && ancienPlan.Statut == StatutsPlan.Actif)
            {
                ancienPlan.Statut = StatutsPlan.Archive;
                await _unitOfWork.FlushDeletesAsync();
            }

            await _unitOfWork.DocumentEchantillonnageEnteteRepository.AddAsync(nouveauPlan);
            return true;
        });

        // L'identifiant est attribué à l'enregistrement.
        return nouveauPlan.Id;
    }

    /// <summary>Formulaire d'échantillonnage : celui indiqué, sinon le formulaire en cours (jamais de code fabriqué).</summary>
    private Task<string> CodeFormulaireAsync(string? codeReference) =>
        _formulaireStructureService.CodeFormulaireAsync(ChampsFormulaire.PourDocument(TypesDocument.PlanEchantillonnage, null, null, null, null)!, codeReference);

    private async Task<RefFormulaire> ResoudreFormulaireAsync(string? codeReference, bool forceNouvelleVersion)
    {
        var resultat = await _formulaireStructureService.UpdateFormulaireStructureAsync(
            RolesFormulaire.Echantillonnage, null, await CodeFormulaireAsync(codeReference),
            isCorrectionMineure: false, forceNouvelleVersion: forceNouvelleVersion);

        if (!resultat.HasValue)
            throw new RegleMetierException("Le formulaire d'échantillonnage n'a pas pu être résolu.");

        return await _unitOfWork.RefFormulaireRepository.GetByIdAsync(resultat.Value.Id)
            ?? throw new RessourceIntrouvableException("Formulaire d'échantillonnage introuvable.");
    }

    /// <summary>Retrouve le niveau de qualité acceptable (NQA) par identifiant ou par valeur ; le crée s'il n'existe pas.</summary>
    private async Task<int> ResoudreNiveauQualiteAcceptableAsync(int? id, decimal? valeur)
    {
        if (id is > 0)
            return id.Value;

        if (!valeur.HasValue)
            throw new RegleMetierException("Le niveau de qualité acceptable (NQA) est obligatoire.");

        var existants = await _unitOfWork.DictionnaireQualiteRepository.GetActiveNqasAsync();
        var existant = existants.FirstOrDefault(n => n.ValeurNqa == valeur.Value);
        if (existant != null)
            return existant.Id;

        var nouveau = new Nqa { ValeurNqa = valeur.Value };
        await _unitOfWork.DictionnaireQualiteRepository.AddNqaAsync(nouveau);
        await _unitOfWork.CommitAsync();
        return nouveau.Id;
    }
}
