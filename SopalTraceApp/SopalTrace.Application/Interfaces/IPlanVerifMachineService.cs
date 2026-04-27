using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanVerifMachineService
{
    /// <summary>
    /// Création unifiée d'un plan de vérification machine.
    /// Reçoit le payload complet (flags + familles + lignes + échéances + pièces).
    /// </summary>
    Task<Guid> CreerPlanVerifAsync(CreateVerifMachineModeleDto request, string creePar);

    /// <summary>Récupère un plan complet avec toutes ses relations.</summary>
    Task<PlanVerifMachineResponseDto> GetPlanVerifByIdAsync(Guid planId);

    /// <summary>Récupère tous les plans de vérification.</summary>
    Task<List<PlanVerifMachineResponseDto>> GetTousLesPlansVerifAsync();

    /// <summary>
    /// Met à jour un plan existant en remplaçant tout son arbre de données.
    /// </summary>
    Task<Guid> MettreAJourPlanVerifAsync(Guid planIdActuel, CreateVerifMachineModeleDto request, string modifiePar);
    /// <summary>Crée une nouvelle version (clone brouillon) d'un plan existant.</summary>
    Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionVerifMachineDto request);

    /// <summary>Archive un plan existant.</summary>
    Task ArchiverPlanAsync(Guid planId, string modifiePar);

    /// <summary>Restaure un plan archivé.</summary>
    Task<Guid> RestaurerPlanAsync(Guid planId, string restaurePar, string motif);

    // Méthodes conservées pour compatibilité
    Task<Guid> CreerPlanAsync(CreatePlanVerifMachineDto request, string creePar);
    Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<VerifMachineLigneEditDto> lignesModifiees);
}
