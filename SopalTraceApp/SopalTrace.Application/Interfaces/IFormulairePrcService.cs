using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;

namespace SopalTrace.Application.Interfaces;

public interface IFormulairePrcService
{
    Task<FormulaireStructureDto?> GetFormulaireByRoleAsync(string role);
    Task<FormulaireStructureDto?> GetFormulaireByIdAsync(Guid id);
    Task<FormulaireStructureDto?> GetFormulaireActifParCodeReferenceAsync(string codeReference);
    Task<IEnumerable<FormulaireReferenceItemDto>> GetFormulairesListByRoleAsync(string role);
    
    /// <summary>
    /// Archive le formulaire actif identifié par son codeReference et crée une nouvelle version active avec version+1.
    /// Si codeReference est null, utilise le role pour trouver le formulaire actif (comportement générique).
    /// </summary>
    Task<(Guid Id, int Version)?> UpdateFormulaireStructureAsync(string role, string? configurationStructureJson, string? codeReference = null, int? versionInitiale = null);
    
    Task<bool> ActiverFormulaireAsync(Guid id);
}
