using SopalTrace.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;using SopalTrace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SopalTrace.Infrastructure.Services;

public class FormulairePrcService : IFormulairePrcService
{
    private readonly IRefFormulaireRepository _formulaireRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SopalTraceDbContext _context;

    public FormulairePrcService(IRefFormulaireRepository formulaireRepository, IUnitOfWork unitOfWork, SopalTraceDbContext context)
    {
        _formulaireRepository = formulaireRepository;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<FormulaireStructureDto?> GetFormulaireByRoleAsync(string role)
    {
        var formulaire = await _formulaireRepository.GetFormulaireActifByRoleAsync(role);

        if (formulaire == null) return null;

        return new FormulaireStructureDto(
            formulaire.Id,
            formulaire.CodeReference,
            formulaire.Designation,
            formulaire.ConfigurationStructureJson,
            formulaire.Role ?? string.Empty,
            formulaire.Version
        );
    }

    public async Task<FormulaireStructureDto?> GetFormulaireByIdAsync(Guid id)
    {
        var formulaire = await _formulaireRepository.GetByIdAsync(id);

        if (formulaire == null) return null;

        return new FormulaireStructureDto(
            formulaire.Id,
            formulaire.CodeReference,
            formulaire.Designation,
            formulaire.ConfigurationStructureJson,
            formulaire.Role ?? string.Empty,
            formulaire.Version
        );
    }

    public async Task<FormulaireStructureDto?> GetFormulaireActifParCodeReferenceAsync(string codeReference)
    {
        var formulaire = await _formulaireRepository.GetFormulaireActifByCodeReferenceAsync(codeReference);

        if (formulaire == null) return null;

        return new FormulaireStructureDto(
            formulaire.Id,
            formulaire.CodeReference,
            formulaire.Designation,
            formulaire.ConfigurationStructureJson,
            formulaire.Role ?? string.Empty,
            formulaire.Version
        );
    }

    public async Task<IEnumerable<FormulaireReferenceItemDto>> GetFormulairesListByRoleAsync(string role)
    {
        var formulaires = await _formulaireRepository.GetFormulairesByRoleAsync(role);
        
        return formulaires.Select(f => new FormulaireReferenceItemDto(
            f.Id,
            f.CodeReference?.Trim() ?? string.Empty,
            f.Designation?.Trim() ?? string.Empty,
            f.Role?.Trim() ?? string.Empty,
            f.Version,
            f.ConfigurationStructureJson,
            f.Statut?.Trim() ?? string.Empty
        ));
    }

    public async Task<(Guid Id, int Version)?> UpdateFormulaireStructureAsync(string role, string? configurationStructureJson, string? codeReference = null, int? versionInitiale = null)
    {
        Guid? nouveauFormulaireId = null;
        int versionFinale = 1;

        RefFormulaire? formulaireActuel = null;
        if (!string.IsNullOrWhiteSpace(codeReference))
        {
            formulaireActuel = await _formulaireRepository.GetFormulaireActifByCodeReferenceAsync(codeReference);
        }
        else
        {
            formulaireActuel = await _formulaireRepository.GetFormulaireActifByRoleAsync(role);
        }

        if (formulaireActuel != null)
        {
            bool forceArchive = versionInitiale.HasValue && versionInitiale.Value != formulaireActuel.Version;

            if (formulaireActuel.Statut?.Trim() == StatutsPlan.Brouillon && !forceArchive)
            {
                formulaireActuel.Statut = StatutsPlan.Actif;
                formulaireActuel.ConfigurationStructureJson = configurationStructureJson;
                await _formulaireRepository.UpdateAsync(formulaireActuel);
                await _unitOfWork.CommitAsync();
                nouveauFormulaireId = formulaireActuel.Id;
                versionFinale = formulaireActuel.Version;
            }
            else
            {
                formulaireActuel.Statut = StatutsPlan.Archive;
                await _formulaireRepository.UpdateAsync(formulaireActuel);

                // Auto-archiver tous les modèles et plans de fabrication qui utilisent cette version
                var modelesAArchiver = await _context.ModeleFabricationEntetes
                    .Where(m => m.FormulaireId == formulaireActuel.Id && m.Statut == StatutsPlan.Actif)
                    .ToListAsync();
                foreach (var m in modelesAArchiver)
                {
                    m.Statut = StatutsPlan.Archive;
                }

                var plansAArchiver = await _context.PlanFabricationEntetes
                    .Where(p => p.FormulaireId == formulaireActuel.Id && p.Statut == StatutsPlan.Actif)
                    .ToListAsync();
                foreach (var p in plansAArchiver)
                {
                    p.Statut = StatutsPlan.Archive;
                }

                var maxVersion = await _formulaireRepository.GetMaxVersionByCodeReferenceAsync(formulaireActuel.CodeReference);

                var newVersion = (versionInitiale.HasValue && versionInitiale.Value > maxVersion) 
                    ? versionInitiale.Value 
                    : (maxVersion + 1);
                    
                var nouveauFormulaire = new RefFormulaire
                {
                    Id = Guid.NewGuid(),
                    CodeReference = formulaireActuel.CodeReference,
                    Designation = formulaireActuel.Designation,
                    Version = newVersion,
                    Statut = StatutsPlan.Actif,
                    CreeLe = DateTime.UtcNow,
                    Role = role,
                    ConfigurationStructureJson = configurationStructureJson
                };
                await _formulaireRepository.AddAsync(nouveauFormulaire);
                await _unitOfWork.CommitAsync();
                nouveauFormulaireId = nouveauFormulaire.Id;
                versionFinale = newVersion;
            }
        }
        else
        {
            // Aucun formulaire existant : c'est la toute première création, on démarre à V=0
            var code = !string.IsNullOrWhiteSpace(codeReference) ? codeReference : role;
            var nouveauFormulaire = new RefFormulaire
            {
                Id = Guid.NewGuid(),
                CodeReference = code,
                Designation = $"Formulaire {code}",
                Version = 0,
                Statut = StatutsPlan.Actif,
                CreeLe = DateTime.UtcNow,
                Role = role,
                ConfigurationStructureJson = configurationStructureJson
            };
            await _formulaireRepository.AddAsync(nouveauFormulaire);
            await _unitOfWork.CommitAsync();
            nouveauFormulaireId = nouveauFormulaire.Id;
            versionFinale = nouveauFormulaire.Version;
        }
        
        return (nouveauFormulaireId.Value, versionFinale);
    }

    public async Task<bool> ActiverFormulaireAsync(Guid id)
    {
        var formulaire = await _formulaireRepository.GetByIdAsync(id);
        if (formulaire == null) return false;

        formulaire.Statut = StatutsPlan.Actif;
        await _formulaireRepository.UpdateAsync(formulaire);
        await _unitOfWork.CommitAsync();
        return true;
    }
}
