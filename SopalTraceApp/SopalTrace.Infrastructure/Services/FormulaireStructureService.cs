using SopalTrace.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Application.Helpers;
using Microsoft.EntityFrameworkCore;

namespace SopalTrace.Infrastructure.Services;

public class FormulaireStructureService : IFormulaireStructureService
{
    private readonly IRefFormulaireRepository _formulaireRepository;
    private readonly IUnitOfWork _unitOfWork;
    public FormulaireStructureService(IRefFormulaireRepository formulaireRepository, IUnitOfWork unitOfWork)
    {
        _formulaireRepository = formulaireRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FormulaireStructureDto?> GetFormulaireByRoleAsync(string role)
    {
        var formulaire = await _formulaireRepository.GetFormulaireActifByRoleAsync(role);

        if (formulaire == null) return null;
        var cols = await _formulaireRepository.GetColonnesActivesByCodeReferenceAsync(formulaire.CodeReference);
        var equipes = await _formulaireRepository.GetEquipesActivesByCodeReferenceAsync(formulaire.CodeReference);

        return new FormulaireStructureDto(
            formulaire.Id,
            formulaire.CodeReference,
            formulaire.Designation,
            ColonneJsonMapper.Serialize(cols, equipes),
            formulaire.Role ?? string.Empty,
            formulaire.Version
        );
    }

    public async Task<FormulaireStructureDto?> GetFormulaireByIdAsync(Guid id)
    {
        var formulaire = await _formulaireRepository.GetByIdAsync(id);

        if (formulaire == null) return null;
        var cols = await _formulaireRepository.GetColonnesActivesByCodeReferenceAsync(formulaire.CodeReference);
        var equipes = await _formulaireRepository.GetEquipesActivesByCodeReferenceAsync(formulaire.CodeReference);

        return new FormulaireStructureDto(
            formulaire.Id,
            formulaire.CodeReference,
            formulaire.Designation,
            ColonneJsonMapper.Serialize(cols, equipes),
            formulaire.Role ?? string.Empty,
            formulaire.Version
        );
    }

    public async Task<FormulaireStructureDto?> GetFormulaireActifParCodeReferenceAsync(string codeReference)
    {
        var formulaire = await _formulaireRepository.GetFormulaireActifByCodeReferenceAsync(codeReference);

        if (formulaire == null) return null;
        var cols = await _formulaireRepository.GetColonnesActivesByCodeReferenceAsync(formulaire.CodeReference);
        var equipes = await _formulaireRepository.GetEquipesActivesByCodeReferenceAsync(formulaire.CodeReference);

        return new FormulaireStructureDto(
            formulaire.Id,
            formulaire.CodeReference,
            formulaire.Designation,
            ColonneJsonMapper.Serialize(cols, equipes),
            formulaire.Role ?? string.Empty,
            formulaire.Version
        );
    }

    public async Task<IEnumerable<FormulaireReferenceItemDto>> GetFormulairesListByRoleAsync(string role)
    {
        var formulaires = await _formulaireRepository.GetFormulairesByRoleAsync(role);
        var dtos = new List<FormulaireReferenceItemDto>();
        foreach (var f in formulaires)
        {
            var cols = await _formulaireRepository.GetColonnesActivesByCodeReferenceAsync(f.CodeReference ?? string.Empty);
            var equipes = await _formulaireRepository.GetEquipesActivesByCodeReferenceAsync(f.CodeReference ?? string.Empty);
            dtos.Add(new FormulaireReferenceItemDto(
                f.Id,
                f.CodeReference?.Trim() ?? string.Empty,
                f.Designation?.Trim() ?? string.Empty,
                f.Role?.Trim() ?? string.Empty,
                f.Version,
                ColonneJsonMapper.Serialize(cols, equipes),
                f.Statut?.Trim() ?? string.Empty
            ));
        }
        return dtos;
    }

    /// <summary>
    /// Synchronise la structure d'un Ref_Formulaire (colonnes extra) en utilisant
    /// AsNoTracking + ExecuteUpdateAsync pour éviter tout conflit de change tracking EF Core.
    /// </summary>
    public async Task<(Guid Id, int Version)?> UpdateFormulaireStructureAsync(
        string role,
        string? configurationStructureJson,
        string? codeReference = null,
        int? versionInitiale = null)
    {
        // ── Chargement SANS tracking pour éviter les conflits EF Core ──
        var codeRefTrimmed = codeReference?.Trim();
        var roleTrimmed = role?.Trim();

        RefFormulaire? formulaireActuel = null;
        if (!string.IsNullOrWhiteSpace(codeRefTrimmed))
        {
            formulaireActuel = await _formulaireRepository.GetFormulaireActifByCodeReferenceAsync(codeRefTrimmed);
        }
        else
        {
            formulaireActuel = await _formulaireRepository.GetFormulaireActifByRoleAsync(roleTrimmed ?? string.Empty);
        }

        var parsedRoot = ColonneJsonMapper.DeserializeRoot(configurationStructureJson);

        if (formulaireActuel != null)
        {
            bool forceArchive = versionInitiale.HasValue && versionInitiale.Value != formulaireActuel.Version;

            if (formulaireActuel.Statut?.Trim() == StatutsPlan.Brouillon && !forceArchive)
            {
                // ── Cas BROUILLON → ACTIF ──
                await _formulaireRepository.UpdateStatutAsync(formulaireActuel.Id, StatutsPlan.Actif);

                // Synchronisation des colonnes directement en DB
                await _formulaireRepository.SyncColonnesAsync(formulaireActuel.CodeReference ?? string.Empty, parsedRoot.CustomCols);
                await _formulaireRepository.SyncEquipesAsync(formulaireActuel.CodeReference ?? string.Empty, parsedRoot.Equipes);

                return (formulaireActuel.Id, formulaireActuel.Version);
            }
            else
            {
                // ── Check if structure actually changed before archiving ──
                if (!forceArchive)
                {
                    var colsActuelles = await _formulaireRepository.GetColonnesActivesByCodeReferenceAsync(formulaireActuel.CodeReference);
                    var equipesActuelles = await _formulaireRepository.GetEquipesActivesByCodeReferenceAsync(formulaireActuel.CodeReference);
                    var structureActuelleJson = ColonneJsonMapper.Serialize(colsActuelles, equipesActuelles);

                    var newStructureJson = (!parsedRoot.Equipes.Any())
                        ? System.Text.Json.JsonSerializer.Serialize(parsedRoot.CustomCols, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                        : System.Text.Json.JsonSerializer.Serialize(parsedRoot, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

                    if (string.Equals(structureActuelleJson, newStructureJson, StringComparison.OrdinalIgnoreCase))
                    {
                        // No structural changes, keep the current ACTIF form
                        return (formulaireActuel.Id, formulaireActuel.Version);
                    }
                }

                // ── Cas ACTIF → ARCHIVE + nouvelle version ──
                await _formulaireRepository.UpdateStatutAsync(formulaireActuel.Id, StatutsPlan.Archive);

                // Auto-archive the linked generic documents
                var linkedDocs = await _unitOfWork.DocumentEnteteRepository.GetByFormulaireIdAsync(formulaireActuel.Id);
                foreach(var doc in linkedDocs)
                {
                    if (doc.Statut != StatutsPlan.Archive)
                    {
                        doc.Statut = StatutsPlan.Archive;
                        await _unitOfWork.DocumentEnteteRepository.UpdateAsync(doc);
                    }
                }

                // Auto-archive the linked ModeleFabrication documents
                var linkedModels = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFormulaireIdAsync(formulaireActuel.Id);
                foreach(var model in linkedModels)
                {
                    if (model.Statut != StatutsPlan.Archive)
                    {
                        model.Statut = StatutsPlan.Archive;
                        await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(model);
                    }
                }

                // Auto-archive the linked PlanFabrication documents
                var linkedPlans = await _unitOfWork.PlanFabricationEnteteRepository.GetByFormulaireIdAsync(formulaireActuel.Id);
                foreach(var plan in linkedPlans)
                {
                    if (plan.Statut != StatutsPlan.Archive)
                    {
                        plan.Statut = StatutsPlan.Archive;
                        await _unitOfWork.PlanFabricationEnteteRepository.UpdateAsync(plan);
                    }
                }

                var maxVersion = await _formulaireRepository.GetMaxVersionByCodeReferenceAsync(formulaireActuel.CodeReference);

                var newVersion = (versionInitiale.HasValue && versionInitiale.Value > maxVersion)
                    ? versionInitiale.Value
                    : (maxVersion + 1);

                // Créer le nouveau formulaire (entité pure, sans colonnes pour l'instant)
                var nouveauFormulaire = new RefFormulaire
                {
                    Id = Guid.NewGuid(),
                    CodeReference = formulaireActuel.CodeReference,
                    Designation = formulaireActuel.Designation,
                    Version = newVersion,
                    Statut = StatutsPlan.Actif,
                    CreeLe = DateTime.UtcNow,
                    Role = role,
                };

                await _formulaireRepository.AddAsync(nouveauFormulaire);
                await _unitOfWork.CommitAsync();

                // Ajouter les colonnes au nouveau formulaire
                await _formulaireRepository.SyncColonnesAsync(nouveauFormulaire.CodeReference ?? string.Empty, parsedRoot.CustomCols);
                await _formulaireRepository.SyncEquipesAsync(nouveauFormulaire.CodeReference ?? string.Empty, parsedRoot.Equipes);

                return (nouveauFormulaire.Id, newVersion);
            }
        }
        else
        {
            // ── Aucun formulaire existant : toute première création ──
            var code = !string.IsNullOrWhiteSpace(codeRefTrimmed) ? codeRefTrimmed : (roleTrimmed ?? role ?? string.Empty);
            var nouveauFormulaire = new RefFormulaire
            {
                Id = Guid.NewGuid(),
                CodeReference = code,
                Designation = $"Formulaire {code}",
                Version = 0,
                Statut = StatutsPlan.Brouillon,
                CreeLe = DateTime.UtcNow,
                Role = role
            };

            await _formulaireRepository.AddAsync(nouveauFormulaire);
            await _unitOfWork.CommitAsync();

            await _formulaireRepository.SyncColonnesAsync(nouveauFormulaire.CodeReference ?? string.Empty, parsedRoot.CustomCols);
            await _formulaireRepository.SyncEquipesAsync(nouveauFormulaire.CodeReference ?? string.Empty, parsedRoot.Equipes);

            return (nouveauFormulaire.Id, 0);
        }
    }

    public async Task<bool> ActiverFormulaireAsync(Guid id)
    {
        await _formulaireRepository.UpdateStatutAsync(id, StatutsPlan.Actif);
        return true;
    }
}
