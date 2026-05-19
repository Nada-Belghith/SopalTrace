using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;
using SopalTrace.Application.DTOs.QualityPlans.PlanProduitFini;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Mappers;

/// <summary>
/// Extension methods for converting section and line entities to their edit DTOs.
/// Centralizes repetitive manual mapping code especially used in plan restoration/duplication workflows.
/// Reduces code duplication when converting entity hierarchies to DTO hierarchies for editing.
/// </summary>
public static class SectionDtoExtensions
{
    /// <summary>
    /// Converts a PlanFabSection entity (with all its lines) to a SectionEditDto
    /// Used in restoration, duplication, and versioning workflows where entities need to be converted back to editable DTOs
    /// Automatically assigns null IDs to force creation of new entities
    /// </summary>
    public static SectionEditDto ConvertFabricationSectionEntityToEditableDto(this PlanFabSection section)
    {
        if (section == null) throw new ArgumentNullException(nameof(section));

        return new SectionEditDto
        {
            Id = null, // Force new IDs on recreation
            ModeleSectionId = section.ModeleSectionId,
            LibelleSection = section.LibelleSection ?? string.Empty,
            //FrequenceLibelle = section.FrequenceLibelle ?? string.Empty,
            OrdreAffiche = section.OrdreAffiche,
            RegleEchantillonnageId = section.RegleEchantillonnageId,
            RegleEchantillonnageLibelle = section.RegleEchantillonnageLibelle ?? string.Empty,
            Lignes = section.PlanFabLignes
                .OrderBy(l => l.OrdreAffiche)
                .Select(l => l.ConvertFabricationLineEntityToEditableDto())
                .ToList()
        };
    }

    /// <summary>
    /// Converts a PlanFabLigne entity to a LigneEditDto for editing
    /// Used internally by section conversion and in individual line updates
    /// Nullifies empty strings and forces new ID assignment
    /// </summary>
    public static LigneEditDto ConvertFabricationLineEntityToEditableDto(this PlanFabLigne ligne)
    {
        if (ligne == null) throw new ArgumentNullException(nameof(ligne));

        return new LigneEditDto
        {
            Id = null, // Force new ID assignment
            OrdreAffiche = ligne.OrdreAffiche,
            TypeCaracteristiqueId = ligne.TypeCaracteristiqueId,
            LibelleAffiche = ligne.LibelleAffiche ?? string.Empty,
            TypeControleId = ligne.TypeControleId,
            MoyenControleId = ligne.MoyenControleId,
            InstrumentCode = ligne.InstrumentCode ?? string.Empty,
            PeriodiciteId = ligne.PeriodiciteId,
            LimiteSpecTexte = ligne.LimiteSpecTexte ?? string.Empty,
            EstCritique = ligne.EstCritique,
            Observations = ligne.Observations ?? string.Empty,
            Instruction = ligne.Instruction ?? string.Empty
        };
    }

    /// <summary>
    /// Converts a PlanPfSection entity (with all its lines) to a SectionPfEditDto
    /// Used in restoration, duplication, and versioning workflows where entities need to be converted back to editable DTOs
    /// Automatically assigns null IDs to force creation of new entities
    /// </summary>
    public static SectionPfEditDto ConvertProduitFiniSectionEntityToEditableDto(this PlanPfSection section)
    {
        if (section == null) throw new ArgumentNullException(nameof(section));

        return new SectionPfEditDto
        {
            Id = null, // Force new ID assignment
            TypeSectionId = section.TypeSectionId,
            LibelleSection = section.LibelleSection ?? string.Empty,
            Notes = section.Notes ?? string.Empty,
            OrdreAffiche = section.OrdreAffiche,
            Lignes = section.PlanPfLignes
                .OrderBy(l => l.OrdreAffiche)
                .Select(l => l.ConvertProduitFiniLineEntityToEditableDto())
                .ToList()
        };
    }

    /// <summary>
    /// Converts a PlanPfLigne entity to a LignePfEditDto for editing
    /// Used internally by section conversion and in individual line updates
    /// Nullifies empty strings and forces new ID assignment
    /// </summary>
    public static LignePfEditDto ConvertProduitFiniLineEntityToEditableDto(this PlanPfLigne ligne)
    {
        if (ligne == null) throw new ArgumentNullException(nameof(ligne));

        return new LignePfEditDto
        {
            Id = null, // Force new ID assignment
            OrdreAffiche = ligne.OrdreAffiche,
            TypeCaracteristiqueId = ligne.TypeCaracteristiqueId,
            LibelleAffiche = ligne.LibelleAffiche ?? string.Empty,
            TypeControleId = ligne.TypeControleId,
            MoyenControleId = ligne.MoyenControleId,
            InstrumentCode = ligne.InstrumentCode ?? string.Empty,
            MoyenTexteLibre = ligne.MoyenTexteLibre ?? string.Empty,
            LimiteSpecTexte = ligne.LimiteSpecTexte ?? string.Empty,
            DefauthequeId = ligne.DefauthequeId,
            Instruction = ligne.Instruction ?? string.Empty,
            Observations = ligne.Observations ?? string.Empty
        };
    }
}
