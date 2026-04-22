using SopalTrace.Application.DTOs.QualityPlans.PlanProduitFini;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class PlanPfService : IPlanPfService
{
    private readonly IPlanPfRepository _repository;

    public PlanPfService(IPlanPfRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PlanPfEnteteDto>> GetGenericPlansAsync()
    {
        var plans = await _repository.GetGenericPlansAsync();
        return plans.Select(PlanPfMapper.MapVersDto).ToList();
    }

    public async Task<PlanPfEnteteDto?> GetPlanByIdAsync(Guid id)
    {
        var plan = await _repository.GetPlanByIdAsync(id);
        if (plan == null) return null;
        return PlanPfMapper.MapVersDto(plan);
    }

    public async Task<Guid> CreateGenericPlanAsync(CreatePlanPfRequestDto dto, string creePar)
    {
        var existing = await _repository.ExistsActiveOrDraftPlanAsync(dto.TypeRobinetCode);

        if (existing)
        {
            throw new InvalidOperationException($"Un plan générique existe déjà pour le Type de Robinet {dto.TypeRobinetCode}.");
        }

        var auteurSecure = SecuriserNomAuteur(creePar);
        var nouvelleVersion = await CalculerNouvelleVersionAsync(dto.TypeRobinetCode);

        var plan = new PlanPfEntete
        {
            Id = Guid.NewGuid(),
            TypeRobinetCode = dto.TypeRobinetCode,
            Designation = dto.Designation,
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            DateApplication = DateOnly.FromDateTime(DateTime.UtcNow),
            CreePar = auteurSecure,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = dto.CommentaireVersion,
            PlanPfSections = new List<PlanPfSection>()
        };

        if (dto.Sections != null && dto.Sections.Any())
        {
            PlanPfMapper.MettreAJourArchitectureComplete(plan, dto.Sections);
        }

        await _repository.AddPlanAsync(plan);
        await _repository.SaveChangesAsync();

        return plan.Id;
    }

    public async Task UpdatePlanAsync(Guid id, List<SectionPfEditDto> sectionsDto, string modifiePar)
    {
        var plan = await _repository.GetPlanByIdAsync(id);
        if (plan == null) throw new KeyNotFoundException("Plan introuvable.");

        if (plan.Statut == StatutsPlan.Actif)
            throw new InvalidOperationException("Impossible de modifier un plan actif.");
        if (plan.Statut == StatutsPlan.Archive)
            throw new InvalidOperationException("Impossible de modifier un plan archivé.");

        PlanPfMapper.MettreAJourArchitectureComplete(plan, sectionsDto);

        plan.ModifiePar = modifiePar;
        plan.ModifieLe = DateTime.UtcNow;
        plan.Statut = StatutsPlan.Actif;

        await _repository.SaveChangesAsync();
    }

    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionPfRequestDto request)
    {
        var ancienPlan = await _repository.GetPlanByIdAsync(request.AncienId);
        if (ancienPlan == null) throw new KeyNotFoundException("Plan source introuvable.");
        if (ancienPlan.Statut == StatutsPlan.Archive) throw new InvalidOperationException("Impossible de versionner un plan archivé.");

        var auteurSecure = SecuriserNomAuteur(!string.IsNullOrWhiteSpace(request.ModifiePar) ? request.ModifiePar : "SYSTEM");

        await ArchiverAncienPlanAsync(request.AncienId, auteurSecure);
        await _repository.SaveChangesAsync();
        _repository.ClearTracking(); // <-- Forcer EF à "oublier" l'ancien plan (contournement du 1:1 Scaffold)

        var nouvelleVersion = await CalculerNouvelleVersionAsync(ancienPlan.TypeRobinetCode);

        var nouveauPlan = PlanPfMapper.CreerNouvelleVersionEntite(ancienPlan, request, auteurSecure, nouvelleVersion);

        if (request.Sections != null && request.Sections.Any())
        {
            PlanPfMapper.MettreAJourArchitectureComplete(nouveauPlan, request.Sections, forceNewIds: true);
        }

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }


    public async Task ArchiverPlanAsync(Guid id, string modifiePar)
    {
        var plan = await _repository.GetPlanByIdAsync(id);
        if (plan == null) throw new KeyNotFoundException("Plan introuvable.");

        plan.Statut = StatutsPlan.Archive;
        plan.ModifiePar = modifiePar;
        plan.ModifieLe = DateTime.UtcNow;

        await _repository.SaveChangesAsync();
    }

    public async Task<Guid> RestaurerPlanArchiveAsync(RestaurerPfRequestDto request)
    {
        // Récupérer l'entité en mode tracké afin de la modifier in-place
        var plan = await _repository.GetPlanPourArchivageAsync(request.PlanArchiveId);
        if (plan == null) throw new KeyNotFoundException("Plan introuvable.");
        if (plan.Statut != StatutsPlan.Archive) throw new InvalidOperationException("Seul un plan archivé peut être restauré.");

        var auteurSecure = SecuriserNomAuteur(request.RestaurePar);

        // Archiver l'éventuel plan actif actuel de la même famille
        await ArchiverPlanActifExistantAsync(plan.TypeRobinetCode, auteurSecure);
        await _repository.SaveChangesAsync(); // <-- Crucial pour libérer l'index d'unicité 'ACTIF'

        // Calculer nouvelle version (pour éviter conflit de version)
        var ancienneVersion = plan.Version;
        var nouvelleVersion = await CalculerNouvelleVersionAsync(plan.TypeRobinetCode);

        // Ré-activer l'entité existante plutôt que d'en créer une nouvelle
        plan.Version = nouvelleVersion;
        plan.Statut = StatutsPlan.Actif;
        plan.CommentaireVersion = $"[Restaure depuis V{ancienneVersion}] {request.MotifRestoration}";
        plan.CreePar = auteurSecure;
        plan.CreeLe = DateTime.UtcNow;

        // Les sections/lignes sont déjà présentes sur l'entité trackée et seront préservées
        await _repository.SaveChangesAsync();

        return plan.Id;
    }

    private string SecuriserNomAuteur(string auteur) => string.IsNullOrWhiteSpace(auteur) ? "SYSTEM" : (auteur.Length > 20 ? auteur[..20] : auteur);

    private async Task ArchiverAncienPlanAsync(Guid ancienId, string auteurSecure)
    {
        var plan = await _repository.GetPlanPourArchivageAsync(ancienId);
        if (plan != null && plan.Statut == StatutsPlan.Actif)
        {
            plan.Statut = StatutsPlan.Archive;
            plan.ModifieLe = DateTime.UtcNow;
        }
    }

    private async Task ArchiverPlanActifExistantAsync(string typeRobinetCode, string auteurSecure)
    {
        var plansActifs = await _repository.GetActivePlansByTypeRobinetAsync(typeRobinetCode);
        if (plansActifs != null && plansActifs.Any())
        {
            foreach (var planActif in plansActifs)
            {
                planActif.Statut = StatutsPlan.Archive;
                planActif.ModifieLe = DateTime.UtcNow;
                planActif.ModifiePar = auteurSecure;
                // Pas besoin de appeler Update(), l'entité est déjà trackée par le repository
            }
        }
    }

    private async Task<int> CalculerNouvelleVersionAsync(string typeRobinetCode)
    {
        var derniereVersion = await _repository.GetDerniereVersionPlanAsync(typeRobinetCode);
        return derniereVersion + 1;
    }
}
