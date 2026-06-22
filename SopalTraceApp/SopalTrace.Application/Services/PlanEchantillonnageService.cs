using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class PlanEchantillonnageService : IPlanEchantillonnageService
{
    private readonly IUnitOfWork _unitOfWork;

    public PlanEchantillonnageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PlanEchanResponseDto?> GetPlanActifAsync()
    {
        var plan = await _unitOfWork.PlanEchantillonnageEnteteRepository.GetPlanActifAsync();
        return plan?.ToResponseDto();
    }

    public async Task<PlanEchanResponseDto?> GetPlanByIdAsync(Guid id)
    {
        var plan = await _unitOfWork.PlanEchantillonnageEnteteRepository.GetByIdAsync(id);
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
    ///     → Plan_Echantillonnage_Entete : Version=0, Statut=ACTIF
    ///
    ///   Nouvelle version (modification) :
    ///     → Ancien plan  : Statut=ARCHIVE
    ///     → FE-ECHAN-01 : Version=0 ──update──►  Version=1, Statut=ACTIF
    ///     → Nouveau plan : Version=1, Statut=ACTIF
    ///
    ///   Et ainsi de suite : v1→ARCHIVE, FE-ECHAN-01 v2 ACTIF, plan v2 ACTIF ...
    /// </summary>
    public async Task<Guid> CreatePlanAsync(CreatePlanEchanRequestDto request, string creePar)
    {
        // Le formulaire de référence existe déjà en seed (v0 BROUILLON), on ne le crée jamais
        var formulaire = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByRoleAsync("ECHANTILLONNAGE");
        if (formulaire == null)
            throw new InvalidOperationException(
                "Le formulaire maître d'échantillonnage (FE-ECHAN-01) est introuvable. " +
                "Veuillez exécuter le script SeedData.");

        // Archiver le plan actif existant (s'il y en a un)
        var planActif = await _unitOfWork.PlanEchantillonnageEnteteRepository.GetPlanActifAsync();
        if (planActif != null)
        {
            planActif.Statut = StatutsPlan.Archive;
            planActif.ModifiePar = creePar;
            planActif.ModifieLe = DateTime.Now;
            await _unitOfWork.PlanEchantillonnageEnteteRepository.UpdateAsync(planActif);
            
            // Archiver l'ancien Ref_Formulaire
            formulaire.Statut = StatutsPlan.Archive;
            formulaire.ModifiePar = creePar;
            formulaire.ModifieLe = DateTime.Now;
            await _unitOfWork.RefFormulaireRepository.UpdateAsync(formulaire);

            // Créer le nouveau Ref_Formulaire (vN+1)
            var nouveauFormulaire = new RefFormulaire
            {
                Id = Guid.NewGuid(),
                CodeReference = formulaire.CodeReference,
                Designation = formulaire.Designation,
                Role = formulaire.Role,
                Version = formulaire.Version + 1,
                Statut = StatutsPlan.Actif,
                CreePar = creePar,
                CreeLe = DateTime.Now
            };
            await _unitOfWork.RefFormulaireRepository.AddAsync(nouveauFormulaire);
            formulaire = nouveauFormulaire;
        }
        else
        {
            // C'est la toute première création, on active juste le v0 BROUILLON existant
            formulaire.Statut = StatutsPlan.Actif;
            formulaire.ModifiePar = creePar;
            formulaire.ModifieLe = DateTime.Now;
            await _unitOfWork.RefFormulaireRepository.UpdateAsync(formulaire);
        }

        var entity = request.ToEntity();
        entity.NqaId = await ResolveNqaId(request.NqaId, request.ValeurNqa);

        entity.Id = Guid.NewGuid();
        entity.FormulaireId = formulaire.Id;
        entity.Version = formulaire.Version;  // prend la version du formulaire
        entity.Statut = StatutsPlan.Actif;    // directement ACTIF
        entity.CreePar = creePar;
        entity.CreeLe = DateTime.Now;

        foreach (var regle in entity.PlanEchantillonnageRegles)
        {
            regle.Id = Guid.NewGuid();
            regle.FicheEnteteId = entity.Id;
        }

        await _unitOfWork.PlanEchantillonnageEnteteRepository.AddAsync(entity);
        await _unitOfWork.CommitAsync();
        return entity.Id;
    }

    /// <summary>
    /// Active manuellement un plan BROUILLON (endpoint de secours, rarement utilisé).
    /// </summary>
    public async Task ActiverPlanAsync(Guid id, string modifiePar)
    {
        var plan = await _unitOfWork.PlanEchantillonnageEnteteRepository.GetByIdAsync(id);
        if (plan == null)
            throw new InvalidOperationException("Plan introuvable.");
        if (plan.Statut != StatutsPlan.Brouillon)
            throw new InvalidOperationException($"Seul un plan BROUILLON peut être activé (statut actuel : {plan.Statut}).");

        var planActif = await _unitOfWork.PlanEchantillonnageEnteteRepository.GetPlanActifAsync();
        if (planActif != null && planActif.Id != id)
        {
            planActif.Statut = StatutsPlan.Archive;
            planActif.ModifiePar = modifiePar;
            planActif.ModifieLe = DateTime.Now;
            await _unitOfWork.PlanEchantillonnageEnteteRepository.UpdateAsync(planActif);
        }

        plan.Statut = StatutsPlan.Actif;
        plan.ModifiePar = modifiePar;
        plan.ModifieLe = DateTime.Now;
        await _unitOfWork.PlanEchantillonnageEnteteRepository.UpdateAsync(plan);

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
    public async Task UpdatePlanAsync(Guid id, UpdatePlanEchanRequestDto request)
    {
        var entity = await _unitOfWork.PlanEchantillonnageEnteteRepository.GetByIdAsync(id);
        if (entity == null) throw new InvalidOperationException("Plan introuvable.");
        if (entity.Statut == StatutsPlan.Archive)
            throw new InvalidOperationException("Impossible de modifier un plan archivé.");

        entity.UpdateEntity(request);
        entity.NqaId = await ResolveNqaId(request.NqaId, request.ValeurNqa);
        entity.ModifieLe = DateTime.Now;

        await _unitOfWork.PlanEchantillonnageEnteteRepository.UpdateAsync(entity);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Crée une nouvelle version du plan (vN+1 ACTIF) :
    ///   1. Archive l'ancien plan (vN → ARCHIVE)
    ///   2. Incrémente FE-ECHAN-01 : Version vN → vN+1, Statut reste ACTIF
    ///   3. Crée le nouveau plan : Version=vN+1, Statut=ACTIF
    /// </summary>
    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionEchanRequestDto request)
    {
        var ancienPlan = await _unitOfWork.PlanEchantillonnageEnteteRepository.GetByIdAsync(request.AncienId);
        if (ancienPlan == null)
            throw new InvalidOperationException("L'ancien plan est introuvable.");
        if (ancienPlan.Statut == StatutsPlan.Archive)
            throw new InvalidOperationException("Ce plan est déjà archivé.");

        // 1. Archiver l'ancien plan
        ancienPlan.Statut = StatutsPlan.Archive;
        ancienPlan.ModifiePar = request.ModifiePar;
        ancienPlan.ModifieLe = DateTime.Now;

        // 2. Archiver l'ancien FE-ECHAN-01 et créer le nouveau
        var formulaire = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByRoleAsync("ECHANTILLONNAGE");
        if (formulaire == null)
            throw new InvalidOperationException("Le formulaire maître d'échantillonnage est introuvable.");

        formulaire.Statut = StatutsPlan.Archive;
        formulaire.ModifiePar = request.ModifiePar;
        formulaire.ModifieLe = DateTime.Now;
        await _unitOfWork.RefFormulaireRepository.UpdateAsync(formulaire);

        var nouveauFormulaire = new RefFormulaire
        {
            Id = Guid.NewGuid(),
            CodeReference = formulaire.CodeReference,
            Designation = formulaire.Designation,
            Role = formulaire.Role,
            Version = formulaire.Version + 1,
            Statut = StatutsPlan.Actif,
            CreePar = request.ModifiePar,
            CreeLe = DateTime.Now
        };
        await _unitOfWork.RefFormulaireRepository.AddAsync(nouveauFormulaire);
        formulaire = nouveauFormulaire;

        // 3. Créer le nouveau plan directement ACTIF
        int finalNqaId = await ResolveNqaId(request.Donnees.NqaId, request.Donnees.ValeurNqa);

        var nouveauPlan = new PlanEchantillonnageEntete
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
            LegendeMoyens = request.Donnees.LegendeMoyens
        };

        if (request.Donnees.Regles != null)
        {
            foreach (var r in request.Donnees.Regles)
            {
                nouveauPlan.PlanEchantillonnageRegles.Add(new PlanEchantillonnageRegle
                {
                    Id = Guid.NewGuid(),
                    FicheEnteteId = nouveauPlan.Id,
                    TailleMinLot = r.TailleMinLot,
                    TailleMaxLot = r.TailleMaxLot,
                    LettreCode = r.LettreCode,
                    EffectifEchantillonA = r.EffectifEchantillonA,
                    NbPostesB = r.NbPostesB,
                    EffectifParPosteAb = r.EffectifParPosteAb,
                    CritereAcceptationAc = r.CritereAcceptationAc,
                    CritereRejetRe = r.CritereRejetRe
                });
            }
        }

        await _unitOfWork.PlanEchantillonnageEnteteRepository.UpdateAsync(ancienPlan);
        await _unitOfWork.PlanEchantillonnageEnteteRepository.AddAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();
        return nouveauPlan.Id;
    }

    /// <summary>
    /// Restaure un plan archivé (vN+1 ACTIF à partir d'une copie d'une archive).
    /// Même comportement que CreerNouvelleVersionAsync.
    /// </summary>
    public async Task<Guid> RestaurerPlanAsync(RestaurerEchanRequestDto request)
    {
        var planARestaurer = await _unitOfWork.PlanEchantillonnageEnteteRepository.GetByIdAsync(request.ArchiveId);
        if (planARestaurer == null)
            throw new InvalidOperationException("Plan archivé introuvable.");

        // Archiver l'actif si présent
        var planActif = await _unitOfWork.PlanEchantillonnageEnteteRepository.GetPlanActifAsync();
        if (planActif != null)
        {
            planActif.Statut = StatutsPlan.Archive;
            planActif.ModifiePar = request.ModifiePar;
            planActif.ModifieLe = DateTime.Now;
            await _unitOfWork.PlanEchantillonnageEnteteRepository.UpdateAsync(planActif);
        }

        // Archiver l'ancien FE-ECHAN-01 et créer le nouveau
        var formulaire = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByRoleAsync("ECHANTILLONNAGE");
        if (formulaire == null)
            throw new InvalidOperationException("Le formulaire maître d'échantillonnage est introuvable.");

        formulaire.Statut = StatutsPlan.Archive;
        formulaire.ModifiePar = request.ModifiePar;
        formulaire.ModifieLe = DateTime.Now;
        await _unitOfWork.RefFormulaireRepository.UpdateAsync(formulaire);

        var nouveauFormulaire = new RefFormulaire
        {
            Id = Guid.NewGuid(),
            CodeReference = formulaire.CodeReference,
            Designation = formulaire.Designation,
            Role = formulaire.Role,
            Version = formulaire.Version + 1,
            Statut = StatutsPlan.Actif,
            CreePar = request.ModifiePar,
            CreeLe = DateTime.Now
        };
        await _unitOfWork.RefFormulaireRepository.AddAsync(nouveauFormulaire);
        formulaire = nouveauFormulaire;

        // Créer le nouveau plan directement ACTIF
        var nouveauPlan = new PlanEchantillonnageEntete
        {
            Id = Guid.NewGuid(),
            FormulaireId = formulaire.Id,
            Version = formulaire.Version,
            Statut = StatutsPlan.Actif,
            CreePar = request.ModifiePar,
            CreeLe = DateTime.Now,
            CommentaireVersion = request.MotifRestauration,

            NiveauControle = planARestaurer.NiveauControle,
            TypePlan = planARestaurer.TypePlan,
            ModeControle = planARestaurer.ModeControle,
            NqaId = planARestaurer.NqaId,
            Remarques = planARestaurer.Remarques,
            LegendeMoyens = planARestaurer.LegendeMoyens
        };

        foreach (var r in planARestaurer.PlanEchantillonnageRegles)
        {
            nouveauPlan.PlanEchantillonnageRegles.Add(new PlanEchantillonnageRegle
            {
                Id = Guid.NewGuid(),
                FicheEnteteId = nouveauPlan.Id,
                TailleMinLot = r.TailleMinLot,
                TailleMaxLot = r.TailleMaxLot,
                LettreCode = r.LettreCode,
                EffectifEchantillonA = r.EffectifEchantillonA,
                NbPostesB = r.NbPostesB,
                EffectifParPosteAb = r.EffectifParPosteAb,
                CritereAcceptationAc = r.CritereAcceptationAc,
                CritereRejetRe = r.CritereRejetRe
            });
        }

        await _unitOfWork.PlanEchantillonnageEnteteRepository.AddAsync(nouveauPlan);
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
