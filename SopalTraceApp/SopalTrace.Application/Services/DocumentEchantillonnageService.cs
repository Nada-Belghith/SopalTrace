using SopalTrace.Application.DTOs.QualityPlans.Echantillonnage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class DocumentEchantillonnageService : IDocumentEchantillonnageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIso2859Service _iso2859Service;
    private readonly IFormulaireStructureService _formulaireStructureService;

    public DocumentEchantillonnageService(
        IUnitOfWork unitOfWork, 
        IIso2859Service iso2859Service,
        IFormulaireStructureService formulaireStructureService)
    {
        _unitOfWork = unitOfWork;
        _iso2859Service = iso2859Service;
        _formulaireStructureService = formulaireStructureService;
    }

    public async Task<DocumentEchantillonnageResponseDto?> GetActiveDocumentAsync()
    {
        var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync();
        return plan?.ToResponseDto();
    }

    public async Task<DocumentEchantillonnageResponseDto?> GetDocumentByIdAsync(Guid id)
    {
        var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetByIdAsync(id);
        return plan?.ToResponseDto();
    }

    /// <summary>
    /// Crée le premier plan d'échantillonnage (v0 ACTIF).
    ///
    /// Cycle de vie complet :
    ///   Seed   : FE-ECHAN-01  Version=0  Statut=BROUILLON  (1 seule ligne, jamais dupliquée)
    ///
    ///   1ère création :
    ///     → FE-ECHAN-01 : Version=0, Statut=BROUILLON  ──update──►  Version=0, Statut=ACTIF
    ///     → Document_Echantillonnage_Entete : Version=0, Statut=ACTIF
    ///
    ///   Nouvelle version (modification) :
    ///     → Ancien plan  : Statut=ARCHIVE
    ///     → FE-ECHAN-01 : Version=0 ──update──►  Version=1, Statut=ACTIF
    ///     → Nouveau plan : Version=1, Statut=ACTIF
    ///
    ///   Et ainsi de suite : v1→ARCHIVE, FE-ECHAN-01 v2 ACTIF, plan v2 ACTIF ...
    /// </summary>
    public async Task<Guid> CreateDocumentAsync(CreateDocumentEchantillonnageRequestDto request, string creePar)
    {
        // Vérifier s'il y a déjà un plan actif pour forcer une nouvelle version
        var planActif = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync();
        bool forceNouvelleVersion = planActif != null;

        var formResult = await _formulaireStructureService.UpdateFormulaireStructureAsync(
            "ECHANTILLONNAGE", 
            null, 
            request.RefFormulaireCodeReference ?? "FE-ECHAN-01", 
            0, 
            isCorrectionMineure: false, 
            forceNouvelleVersion: forceNouvelleVersion);

        if (!formResult.HasValue)
            throw new InvalidOperationException("Erreur lors de la résolution du formulaire d'échantillonnage.");

        var formulaire = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formResult.Value.Id);
        if (formulaire == null) throw new InvalidOperationException("Formulaire introuvable après résolution.");

        var entity = request.ToEntity();
        if (entity == null) throw new Exception("Requête invalide ou entité nulle.");
        
        entity.NqaId = await ResolveNqaId(request.NqaId, request.ValeurNqa);

        entity.Id = Guid.NewGuid();
        entity.FormulaireId = formulaire.Id;
        entity.Version = formulaire.Version;  // prend la version du formulaire
        entity.Statut = StatutsPlan.Actif;    // directement ACTIF
        entity.CreePar = creePar;
        entity.CreeLe = DateTime.Now;

        if (planActif != null)
        {
            planActif.Statut = StatutsPlan.Archive;
            planActif.ModifiePar = creePar;
            planActif.ModifieLe = DateTime.Now;
            await _unitOfWork.DocumentEchantillonnageEnteteRepository.UpdateAsync(planActif);
        }

        await _unitOfWork.DocumentEchantillonnageEnteteRepository.AddAsync(entity);
        await _unitOfWork.CommitAsync();
        return entity.Id;
    }

    /// <summary>
    /// Active manuellement un plan BROUILLON (endpoint de secours, rarement utilisé).
    /// </summary>
    public async Task ActivateDocumentAsync(Guid id, string modifiePar)
    {
        var plan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetByIdAsync(id);
        if (plan == null)
            throw new InvalidOperationException("Plan introuvable.");
        if (plan.Statut != StatutsPlan.Brouillon)
            throw new InvalidOperationException($"Seul un plan BROUILLON peut être activé (statut actuel : {plan.Statut}).");

        var planActif = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetPlanActifAsync();
        if (planActif != null && planActif.Id != id)
        {
            planActif.Statut = StatutsPlan.Archive;
            planActif.ModifiePar = modifiePar;
            planActif.ModifieLe = DateTime.Now;
            await _unitOfWork.DocumentEchantillonnageEnteteRepository.UpdateAsync(planActif);
        }

        plan.Statut = StatutsPlan.Actif;
        plan.ModifiePar = modifiePar;
        plan.ModifieLe = DateTime.Now;
        await _unitOfWork.DocumentEchantillonnageEnteteRepository.UpdateAsync(plan);

        var formulaire = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByRoleAsync("ECHANTILLONNAGE");
        if (formulaire != null && formulaire.Statut != StatutsPlan.Actif)
        {
            formulaire.Statut = StatutsPlan.Actif;
            await _unitOfWork.RefFormulaireRepository.UpdateAsync(formulaire);
        }

        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Met à jour les données du plan (sans changer la version ni le statut).
    /// </summary>
    public async Task UpdateDocumentAsync(Guid id, UpdateDocumentEchantillonnageRequestDto request)
    {
        var entity = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetByIdAsync(id);
        if (entity == null) throw new InvalidOperationException("Plan introuvable.");
        if (entity.Statut == StatutsPlan.Archive)
            throw new InvalidOperationException("Impossible de modifier un plan archivé.");

        entity.UpdateEntity(request);
        entity.NqaId = await ResolveNqaId(request.NqaId, request.ValeurNqa);
        entity.ModifieLe = DateTime.Now;

        // Gérer la correction mineure du formulaire
        await _formulaireStructureService.UpdateFormulaireStructureAsync(
            "ECHANTILLONNAGE", 
            null, 
            request.RefFormulaireCodeReference ?? "FE-ECHAN-01", 
            null, 
            isCorrectionMineure: true, 
            forceNouvelleVersion: false);

        await _unitOfWork.DocumentEchantillonnageEnteteRepository.UpdateAsync(entity);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Crée une nouvelle version du plan (vN+1 ACTIF) :
    ///   1. Archive l'ancien plan (vN → ARCHIVE)
    ///   2. Incrémente FE-ECHAN-01 : Version vN → vN+1, Statut reste ACTIF
    ///   3. Crée le nouveau plan : Version=vN+1, Statut=ACTIF
    /// </summary>
    public async Task<Guid> CreateNewVersionAsync(CreateNewVersionDocumentEchantillonnageRequestDto request)
    {
        var ancienPlan = await _unitOfWork.DocumentEchantillonnageEnteteRepository.GetByIdAsync(request.AncienId);
        if (ancienPlan == null)
            throw new InvalidOperationException("L'ancien plan est introuvable.");
        if (ancienPlan.Statut == StatutsPlan.Archive)
            throw new InvalidOperationException("Ce plan est déjà archivé.");

        // 1. Gérer le formulaire (nouvelle version) via le service partagé
        var formResult = await _formulaireStructureService.UpdateFormulaireStructureAsync(
            "ECHANTILLONNAGE", 
            null, 
            request.Donnees.RefFormulaireCodeReference ?? "FE-ECHAN-01", 
            null, 
            isCorrectionMineure: false, 
            forceNouvelleVersion: true);

        if (!formResult.HasValue)
            throw new InvalidOperationException("Erreur lors de la création de la nouvelle version du formulaire.");

        var formulaire = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formResult.Value.Id);
        if (formulaire == null) throw new InvalidOperationException("Nouveau formulaire introuvable.");

        // 2. Archiver l'ancien plan (le FormulaireStructureService a déjà archivé les plans liés génériques,
        // mais pour l'échantillonnage qui a sa propre table, on doit le faire manuellement ici si on veut
        // garder le contrôle, ou bien s'appuyer sur la logique partagée. Dans le doute, on le fait ici.)
        ancienPlan.Statut = StatutsPlan.Archive;
        ancienPlan.ModifiePar = request.ModifiePar;
        ancienPlan.ModifieLe = DateTime.Now;

        // 3. Créer le nouveau plan directement ACTIF
        int finalNqaId = await ResolveNqaId(request.Donnees.NqaId, request.Donnees.ValeurNqa);

        var nouveauPlan = new DocumentEchantillonnageEntete
        {
            Id = Guid.NewGuid(),
            FormulaireId = formulaire.Id,
            Version = formulaire.Version,     // vN+1
            Statut = StatutsPlan.Actif,       // directement ACTIF
            CreePar = request.ModifiePar,
            CreeLe = DateTime.Now,
            CommentaireVersion = request.MotifModification,

            NiveauControle = request.Donnees.NiveauControle,
            TypePlan = request.Donnees.TypePlan,
            ModeControle = request.Donnees.ModeControle,
            NqaId = finalNqaId,
            Remarques = request.Donnees.Remarques,
            LegendeMoyens = request.Donnees.LegendeMoyens,
            CritereAcceptationAc = request.Donnees.CritereAcceptationAc,
            CritereRejetRe = request.Donnees.CritereRejetRe
        };


        await _unitOfWork.DocumentEchantillonnageEnteteRepository.UpdateAsync(ancienPlan);
        await _unitOfWork.DocumentEchantillonnageEnteteRepository.AddAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();
        return nouveauPlan.Id;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<int> ResolveNqaId(int? nqaId, double? valeurNqa)
    {
        if (nqaId != null && nqaId != 0)
            return nqaId.Value;

        if (!valeurNqa.HasValue)
            throw new ArgumentException("Le champ NQA est obligatoire.");

        var nqas = await _unitOfWork.DictionnaireQualiteRepository.GetActiveNqasAsync();
        var matchingNqa = nqas.FirstOrDefault(n => Math.Abs(n.ValeurNqa - valeurNqa.Value) < 0.0001);
        if (matchingNqa != null)
            return matchingNqa.Id;

        var newNqa = new Nqa { ValeurNqa = valeurNqa.Value };
        await _unitOfWork.DictionnaireQualiteRepository.AddNqaAsync(newNqa);
        await _unitOfWork.CommitAsync();
        return newNqa.Id;
    }

}
