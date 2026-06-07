using SopalTrace.Application.DTOs.QualityPlans.ControlePoste;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IControlePosteService
{
    Task<Guid> CreerPlanAsync(CreateControlePosteRequestDto request, string creePar);
    Task<ControlePosteResponseDto> GetPlanByIdAsync(Guid planId);
    Task<List<ControlePosteResponseDto>> GetTousLesPlansAsync();
    Task<Guid> MettreAJourPlanAsync(Guid planId, SaveControlePosteDto request, string modifiePar);
    Task<bool> MettreAJourLignesAsync(Guid planId, List<LigneNcEditDto> lignesModifiees);
    Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionNcRequestDto request);
    Task<Guid> RestaurerPlanAsync(Guid planId, string restaurePar, string motif);
}
