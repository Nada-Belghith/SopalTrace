#nullable enable

using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanFabricationMapper
{
    public static PlanFabricationEntete ConstruireEntitePlanAPartirDeModele(ModeleFabricationEntete modele, CreatePlanRequestDto dto, string designationSage)
    {
        var plan = new PlanFabricationEntete
        {
            Id = Guid.NewGuid(), ModeleSourceId = modele.Id, CodeArticleSage = dto.CodeArticleSage,
            Designation = designationSage, Nom = dto.Nom ?? $"PC-{dto.CodeArticleSage}",
            Version = 0, Statut = StatutsPlan.Brouillon, CreePar = "Admin", CreeLe = DateTime.UtcNow,
            OperationCode = string.IsNullOrWhiteSpace(dto.OperationCode) ? modele.OperationCode : dto.OperationCode,
            //LegendeMoyens = string.IsNullOrWhiteSpace(dto.LegendeMoyens) ? modele.LegendeMoyens : dto.LegendeMoyens,
            //Remarques = string.IsNullOrWhiteSpace(dto.Remarques) ? modele.Notes : dto.Remarques,
            //FamilleProduitFiniCode = modele.FamilleProduitFiniCode,
            PlanFabricationSections = new List<PlanFabricationSection>()
        };

        foreach (var modeleSection in modele.ModeleFabricationSections)
        {
            var planSection = new PlanFabricationSection
            {
                Id = Guid.NewGuid(), PlanEnteteId = plan.Id, ModeleSectionId = modeleSection.Id,
                OrdreAffiche = modeleSection.OrdreAffiche, 
                TypeSectionId = modeleSection.TypeSectionId,
                PeriodiciteId = modeleSection.PeriodiciteId,
                RegleEchantillonnageId = modeleSection.RegleEchantillonnageId,
                LibelleSection = ReconstruireLibelleComplet(modeleSection.LibelleSection, modeleSection.TypeSection?.Libelle, modeleSection.Periodicite?.Libelle, modeleSection.RegleEchantillonnage?.Libelle),
                //FrequenceLibelle = modeleSection.FrequenceLibelle, 
                PlanFabricationLignes = new List<PlanFabricationLigne>()
            };

            foreach (var modeleLigne in modeleSection.ModeleFabricationLignes)
            {
                var instrumentData = MapperHelper.NormalizeInstrumentCode(modeleLigne.InstrumentCode, modeleLigne.MoyenTexteLibre);

                planSection.PlanFabricationLignes.Add(new PlanFabricationLigne
                {
                    Id = Guid.NewGuid(), PlanEnteteId = plan.Id, SectionId = planSection.Id, ModeleLigneSourceId = modeleLigne.Id,
                    OrdreAffiche = modeleLigne.OrdreAffiche, TypeCaracteristiqueId = modeleLigne.TypeCaracteristiqueId,
                    LibelleAffiche = modeleLigne.LibelleAffiche, TypeControleId = modeleLigne.TypeControleId,
                    MoyenControleId = MapperHelper.NullIfEmpty(modeleLigne.MoyenControleId),
                    InstrumentCode = instrumentData.InstrumentCode,
                    MoyenTexteLibre = instrumentData.MoyenTexteLibre,
                    LimiteSpecTexte = string.IsNullOrWhiteSpace(modeleLigne.LimiteSpecTexte) ? null : modeleLigne.LimiteSpecTexte
                });
            }
            plan.PlanFabricationSections.Add(planSection);
        }

        return plan;
    }

    public static PlanFabricationEntete DupliquerEntitePlan(PlanFabricationEntete source, string nouveauCode, string nouvelleDesig, string? creePar, string? comm = null)
    {
        int nouvelleVersion = comm == null ? 1 : source.Version + 1;
        var plan = new PlanFabricationEntete
        {
            Id = Guid.NewGuid(), ModeleSourceId = source.ModeleSourceId, CodeArticleSage = nouveauCode,
            Designation = nouvelleDesig, Nom = !string.IsNullOrWhiteSpace(source.Nom) ? source.Nom : $"Plan de contrôle {nouvelleDesig}",
            Version = nouvelleVersion, Statut = StatutsPlan.Brouillon, MachineDefautCode = source.MachineDefautCode,
            OperationCode = source.OperationCode,
            //LegendeMoyens = source.LegendeMoyens,
            //Remarques = source.Remarques,
            //CommentaireVersion = comm,
            CreePar = creePar ?? "SYSTEM", CreeLe = DateTime.UtcNow,
            PlanFabricationSections = new List<PlanFabricationSection>()
        };

        foreach (var sourceSection in source.PlanFabricationSections)
        {
            var planSection = new PlanFabricationSection
            {
                Id = Guid.NewGuid(), PlanEnteteId = plan.Id, ModeleSectionId = sourceSection.ModeleSectionId,
                OrdreAffiche = sourceSection.OrdreAffiche, LibelleSection = sourceSection.LibelleSection,
                //FrequenceLibelle = sourceSection.FrequenceLibelle, 
                RegleEchantillonnageId = sourceSection.RegleEchantillonnageId, 
                RegleEchantillonnageLibelle = sourceSection.RegleEchantillonnageLibelle,
                PlanFabricationLignes = new List<PlanFabricationLigne>()
            };

            foreach (var sourceLigne in sourceSection.PlanFabricationLignes)
            {
                planSection.PlanFabricationLignes.Add(new PlanFabricationLigne
                {
                    Id = Guid.NewGuid(), PlanEnteteId = plan.Id, SectionId = planSection.Id, ModeleLigneSourceId = sourceLigne.ModeleLigneSourceId,
                    OrdreAffiche = sourceLigne.OrdreAffiche, TypeCaracteristiqueId = sourceLigne.TypeCaracteristiqueId,
                    LibelleAffiche = sourceLigne.LibelleAffiche, TypeControleId = sourceLigne.TypeControleId,
                    MoyenControleId = MapperHelper.NullIfEmpty(sourceLigne.MoyenControleId), PeriodiciteId = MapperHelper.NullIfEmpty(sourceLigne.PeriodiciteId),
                    InstrumentCode = sourceLigne.InstrumentCode,
                    MoyenTexteLibre = sourceLigne.MoyenTexteLibre,
                    LimiteSpecTexte = sourceLigne.LimiteSpecTexte,
                    Observations = sourceLigne.Observations, Instruction = sourceLigne.Instruction, EstCritique = sourceLigne.EstCritique
                });
            }
            plan.PlanFabricationSections.Add(planSection);
        }
        return plan;
    }

    public static PlanFabricationSection ConstruireNouvelleSectionPlan(Guid planId, SectionEditDto dto)
    {
        var libelle = string.IsNullOrWhiteSpace(dto.LibelleSection) ? "NOUVELLE SECTION" : dto.LibelleSection?.Trim();

        // If the DTO contains an "aperçu" or preview label, preserve it as-is.
        // For FAB we do not prepend any standard prefix to the saved label so that
        // the UI shows exactly what was imported by the user (same behaviour as PF).
        var persistedLabel = libelle;

        return new PlanFabricationSection
        {
            PlanEnteteId = planId, ModeleSectionId = MapperHelper.NullIfEmpty(dto.ModeleSectionId), OrdreAffiche = dto.OrdreAffiche,
            TypeSectionId = dto.TypeSectionId,
            PeriodiciteId = dto.PeriodiciteId,
            RegleEchantillonnageId = MapperHelper.NullIfEmpty(dto.RegleEchantillonnageId),
            LibelleSection = ReconstruireLibelleComplet(dto.LibelleSection, null, dto.FrequenceLibelle, dto.RegleEchantillonnageLibelle),
            //FrequenceLibelle = string.IsNullOrWhiteSpace(dto.FrequenceLibelle) ? null : dto.FrequenceLibelle,
            PlanFabricationLignes = new List<PlanFabricationLigne>()
        };
    }

    public static string ReconstruireLibelleComplet(string libelleBase, string? natureLibelle, string? freqLibelle, string? regleLibelle = null)
    {
        string baseStr = natureLibelle ?? libelleBase ?? "Section sans nom";
        
        // Nettoyage radical des préfixes existants
        baseStr = baseStr.Replace("Caractéristiques à contrôler ", "");

        // ✅ Nettoyer le titre : On ne split QUE si ce n'est pas un "(OF)" 
        // car le (OF) fait partie intégrante du nom de la section dans votre base.
        if (baseStr.Contains(" (") && !baseStr.ToLower().Contains("(of)"))
        {
            baseStr = baseStr.Split(" (")[0];
        }
        
        string prefix = baseStr.ToLower().StartsWith("caract") ? "" : "Caractéristiques à contrôler ";
        string full = $"{prefix}{baseStr}".Trim();

        // On accumule les compléments (Fréquence + Règle)
        List<string> complements = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(freqLibelle)) complements.Add(freqLibelle.Trim());
        if (!string.IsNullOrWhiteSpace(regleLibelle)) complements.Add(regleLibelle.Trim());

        if (complements.Any())
        {
            string detail = string.Join(" - ", complements.Distinct());
            if (!full.Contains(detail))
            {
                full += $" ({detail})";
            }
        }

        return full;
    }

    public static PlanFabricationLigne ConstruireNouvelleLignePlan(Guid planId, Guid sectionId, LigneEditDto dto)
    {
        var instrumentData = MapperHelper.NormalizeInstrumentCode(dto.InstrumentCode);

        return new PlanFabricationLigne
        {
            PlanEnteteId = planId, SectionId = sectionId, ModeleLigneSourceId = MapperHelper.NullIfEmpty(dto.ModeleLigneSourceId),
            OrdreAffiche = dto.OrdreAffiche, 
            TypeCaracteristiqueId = MapperHelper.NullIfEmpty(dto.TypeCaracteristiqueId) ?? Guid.Empty, 
            TypeControleId = MapperHelper.NullIfEmpty(dto.TypeControleId) ?? Guid.Empty,
            LibelleAffiche = string.IsNullOrWhiteSpace(dto.LibelleAffiche) ? null : dto.LibelleAffiche, EstCritique = dto.EstCritique,
            MoyenControleId = MapperHelper.NullIfEmpty(dto.MoyenControleId), PeriodiciteId = MapperHelper.NullIfEmpty(dto.PeriodiciteId),
            InstrumentCode = instrumentData.InstrumentCode, MoyenTexteLibre = instrumentData.MoyenTexteLibre,
            LimiteSpecTexte = string.IsNullOrWhiteSpace(dto.LimiteSpecTexte) ? null : dto.LimiteSpecTexte, Observations = string.IsNullOrWhiteSpace(dto.Observations) ? null : dto.Observations,
            Instruction = string.IsNullOrWhiteSpace(dto.Instruction) ? null : dto.Instruction
        };
    }

    public static void MettreAJourEntiteLigne(PlanFabricationLigne ligne, LigneEditDto dto)
    {
        var instrumentData = MapperHelper.NormalizeInstrumentCode(dto.InstrumentCode);

        ligne.OrdreAffiche = dto.OrdreAffiche; 
        ligne.TypeCaracteristiqueId = MapperHelper.NullIfEmpty(dto.TypeCaracteristiqueId) ?? Guid.Empty; 
        ligne.TypeControleId = MapperHelper.NullIfEmpty(dto.TypeControleId) ?? Guid.Empty;
        ligne.LibelleAffiche = string.IsNullOrWhiteSpace(dto.LibelleAffiche) ? null : dto.LibelleAffiche; ligne.EstCritique = dto.EstCritique;
        ligne.MoyenControleId = MapperHelper.NullIfEmpty(dto.MoyenControleId); ligne.PeriodiciteId = MapperHelper.NullIfEmpty(dto.PeriodiciteId);
        ligne.InstrumentCode = instrumentData.InstrumentCode; ligne.MoyenTexteLibre = instrumentData.MoyenTexteLibre; 
        ligne.LimiteSpecTexte = string.IsNullOrWhiteSpace(dto.LimiteSpecTexte) ? null : dto.LimiteSpecTexte; ligne.Observations = string.IsNullOrWhiteSpace(dto.Observations) ? null : dto.Observations; ligne.Instruction = string.IsNullOrWhiteSpace(dto.Instruction) ? null : dto.Instruction;
    }

    public static PlanResponseDto MapperEntitePlanVersDto(PlanFabricationEntete plan)
    {
        return new PlanResponseDto
        {
            Id = plan.Id,
            ModeleSourceId = plan.ModeleSourceId ?? Guid.Empty,
            OperationCode = !string.IsNullOrWhiteSpace(plan.OperationCode) ? plan.OperationCode : plan.ModeleSource?.OperationCode ?? string.Empty,
            CodeArticleSage = plan.CodeArticleSage,
            Nom = plan.Nom,
            Designation = plan.Designation ?? string.Empty,
            Version = plan.Version,
            Statut = plan.Statut,
            MachineDefautCode = plan.MachineDefautCode,
            //LegendeMoyens = plan.LegendeMoyens ?? string.Empty,
            //Remarques = plan.Remarques ?? string.Empty,
            CreePar = plan.CreePar,
            CreeLe = plan.CreeLe,
            //ModifiePar = plan.ModifiePar ?? string.Empty,
            //ModifieLe = plan.ModifieLe,
            Sections = plan.PlanFabricationSections?.Select(s => new PlanSectionResponseDto
            {
                Id = s.Id,
                ModeleSectionId = s.ModeleSectionId,
                OrdreAffiche = s.OrdreAffiche,
                LibelleSection = s.LibelleSection,
                //FrequenceLibelle = s.FrequenceLibelle ?? string.Empty,
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,
                RegleEchantillonnageId = s.RegleEchantillonnageId,
                RegleEchantillonnageLibelle = s.RegleEchantillonnage?.Libelle ?? string.Empty,
                Lignes = s.PlanFabricationLignes?.Select(l => new PlanLigneResponseDto
                {
                    Id = l.Id,
                    ModeleLigneSourceId = l.ModeleLigneSourceId,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId ?? Guid.Empty,
                    LibelleAffiche = l.LibelleAffiche ?? string.Empty,
                    NomCategorie = l.TypeCaracteristique?.Libelle ?? string.Empty,
                    TypeControleId = l.TypeControleId ?? Guid.Empty,
                    MoyenControleId = l.MoyenControleId,
                    PeriodiciteId = l.PeriodiciteId,
                    InstrumentCode = l.InstrumentCode ?? l.MoyenTexteLibre ?? string.Empty,
                    LimiteSpecTexte = l.LimiteSpecTexte ?? string.Empty,
                    Observations = l.Observations ?? string.Empty,
                    Instruction = l.Instruction ?? string.Empty,
                    EstCritique = l.EstCritique
                }).ToList() ?? new List<PlanLigneResponseDto>()
            }).ToList() ?? new List<PlanSectionResponseDto>()
        };
    }

    public static PlanFabricationEntete ConstruireEntitePlanVierge(CreatePlanRequestDto dto, string designationSage)
    {
        return new PlanFabricationEntete
        {
            Id = Guid.NewGuid(),
            ModeleSourceId = null,
            CodeArticleSage = dto.CodeArticleSage,
            Designation = designationSage,
            OperationCode = dto.OperationCode,
            Nom = string.IsNullOrWhiteSpace(dto.Nom) ? $"PC-{dto.CodeArticleSage}" : dto.Nom,
            Version = 0,
            Statut = StatutsPlan.Brouillon,
            CreePar = "Admin",
            CreeLe = DateTime.UtcNow,
            //LegendeMoyens = string.IsNullOrWhiteSpace(dto.LegendeMoyens) ? null : dto.LegendeMoyens,
            //Remarques = dto.Remarques,
            PlanFabricationSections = new List<PlanFabricationSection>()
        };
    }
}
