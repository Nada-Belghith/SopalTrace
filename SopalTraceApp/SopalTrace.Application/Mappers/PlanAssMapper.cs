using SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanAssMapper
{
    public static PlanAssResponseDto MapperEntitePlanVersDto(PlanAssEntete plan)
    {
        return new PlanAssResponseDto
        {
            Id = plan.Id,
            OperationCode = plan.OperationCode,
            TypeRobinetCode = plan.TypeRobinetCode,
            EstModele = plan.EstModele,
            CodeArticleSage = plan.CodeArticleSage,
            Designation = plan.Designation,
            Nom = plan.Nom,
            Version = plan.Version,
            Statut = plan.Statut,
            DateApplication = plan.DateApplication?.ToDateTime(TimeOnly.MinValue),
            CreePar = plan.CreePar,
            CreeLe = plan.CreeLe,
            ModifiePar = plan.ModifiePar,
            ModifieLe = plan.ModifieLe,
            CommentaireVersion = plan.CommentaireVersion,
            Sections = plan.PlanAssSections?.Select(s => new SectionAssResponseDto
            {
                Id = s.Id,
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,
                LibelleSection = s.LibelleSection,
                OrdreAffiche = s.OrdreAffiche,
                NormeReference = s.NormeReference,
                NqaId = s.NqaId,
                Notes = s.Notes,
                Lignes = s.PlanAssLignes?.Select(l => new LigneAssResponseDto
                {
                    Id = l.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                    LibelleAffiche = l.LibelleAffiche,
                    TypeControleId = l.TypeControleId,
                    MoyenControleId = l.MoyenControleId,
                    GroupeInstrumentId = l.GroupeInstrumentId,
                    ValeurNominale = l.ValeurNominale,
                    ToleranceSuperieure = l.ToleranceSuperieure,
                    ToleranceInferieure = l.ToleranceInferieure,
                    Unite = l.Unite,
                    LimiteSpecTexte = l.LimiteSpecTexte,
                    Observations = l.Observations,
                    Instruction = l.Instruction,
                    EstCritique = l.EstCritique
                }).ToList() ?? new List<LigneAssResponseDto>()
            }).ToList() ?? new List<SectionAssResponseDto>()
        };
    }

    public static PlanAssSection ConstruireNouvelleSection(Guid planId, SectionAssEditDto dto)
    {
        return new PlanAssSection
        {
            PlanEnteteId = planId,
            TypeSectionId = dto.TypeSectionId,
            PeriodiciteId = dto.PeriodiciteId,
            OrdreAffiche = dto.OrdreAffiche,
            LibelleSection = string.IsNullOrWhiteSpace(dto.LibelleSection) ? "NOUVELLE SECTION" : dto.LibelleSection,
            NormeReference = dto.NormeReference,
            NqaId = dto.NqaId,
            Notes = dto.Notes,
            PlanAssLignes = new List<PlanAssLigne>()
        };
    }

    public static PlanAssLigne ConstruireNouvelleLigne(Guid planId, Guid sectionId, LigneAssEditDto dto)
    {
        return new PlanAssLigne
        {
            PlanEnteteId = planId,
            SectionId = sectionId,
            OrdreAffiche = dto.OrdreAffiche,
            TypeCaracteristiqueId = dto.TypeCaracteristiqueId,
            LibelleAffiche = dto.LibelleAffiche,
            TypeControleId = dto.TypeControleId,
            MoyenControleId = dto.MoyenControleId,
            GroupeInstrumentId = dto.GroupeInstrumentId,
            ValeurNominale = dto.ValeurNominale,
            ToleranceSuperieure = dto.ToleranceSuperieure,
            ToleranceInferieure = dto.ToleranceInferieure,
            Unite = string.IsNullOrWhiteSpace(dto.Unite) ? null : dto.Unite,
            LimiteSpecTexte = string.IsNullOrWhiteSpace(dto.LimiteSpecTexte) ? null : dto.LimiteSpecTexte,
            Observations = string.IsNullOrWhiteSpace(dto.Observations) ? null : dto.Observations,
            Instruction = string.IsNullOrWhiteSpace(dto.Instruction) ? null : dto.Instruction,
            EstCritique = dto.EstCritique
        };
    }

    public static void MettreAJourEntiteLigne(PlanAssLigne ligne, LigneAssEditDto dto)
    {
        ligne.OrdreAffiche = dto.OrdreAffiche;
        ligne.TypeCaracteristiqueId = dto.TypeCaracteristiqueId;
        ligne.LibelleAffiche = dto.LibelleAffiche;
        ligne.TypeControleId = dto.TypeControleId;
        ligne.MoyenControleId = dto.MoyenControleId;
        ligne.GroupeInstrumentId = dto.GroupeInstrumentId;
        ligne.ValeurNominale = dto.ValeurNominale;
        ligne.ToleranceSuperieure = dto.ToleranceSuperieure;
        ligne.ToleranceInferieure = dto.ToleranceInferieure;
        ligne.Unite = string.IsNullOrWhiteSpace(dto.Unite) ? null : dto.Unite;
        ligne.LimiteSpecTexte = string.IsNullOrWhiteSpace(dto.LimiteSpecTexte) ? null : dto.LimiteSpecTexte;
        ligne.Observations = string.IsNullOrWhiteSpace(dto.Observations) ? null : dto.Observations;
        ligne.Instruction = string.IsNullOrWhiteSpace(dto.Instruction) ? null : dto.Instruction;
        ligne.EstCritique = dto.EstCritique;
    }

    public static PlanAssEntete DupliquerEntitePlan(PlanAssEntete source, bool estModele, string? nouveauCodeArticle, string? nouvelleDesig, string creePar, string? motif)
    {
        var planId = Guid.NewGuid();
        var plan = new PlanAssEntete
        {
            Id = planId,
            OperationCode = source.OperationCode,
            TypeRobinetCode = source.TypeRobinetCode,
            EstModele = estModele,
            CodeArticleSage = nouveauCodeArticle,
            Designation = nouvelleDesig,
            Nom = estModele ? source.Nom : $"PC-{nouveauCodeArticle}-v1",
            Version = estModele ? source.Version + 1 : 1,
            Statut = StatutsPlan.Brouillon,
            CreePar = creePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = motif,
            PlanAssSections = new List<PlanAssSection>()
        };

        foreach (var sourceSection in source.PlanAssSections ?? Enumerable.Empty<PlanAssSection>())
        {
            var sectionId = Guid.NewGuid();
            var section = new PlanAssSection
            {
                Id = sectionId,
                PlanEnteteId = planId,
                TypeSectionId = sourceSection.TypeSectionId,
                PeriodiciteId = sourceSection.PeriodiciteId,
                OrdreAffiche = sourceSection.OrdreAffiche,
                LibelleSection = sourceSection.LibelleSection,
                NormeReference = sourceSection.NormeReference,
                NqaId = sourceSection.NqaId,
                Notes = sourceSection.Notes,
                PlanAssLignes = new List<PlanAssLigne>()
            };

            foreach (var sourceLigne in sourceSection.PlanAssLignes ?? Enumerable.Empty<PlanAssLigne>())
            {
                section.PlanAssLignes.Add(new PlanAssLigne
                {
                    Id = Guid.NewGuid(),
                    PlanEnteteId = planId,
                    SectionId = sectionId,
                    OrdreAffiche = sourceLigne.OrdreAffiche,
                    TypeCaracteristiqueId = sourceLigne.TypeCaracteristiqueId,
                    LibelleAffiche = sourceLigne.LibelleAffiche,
                    TypeControleId = sourceLigne.TypeControleId,
                    MoyenControleId = sourceLigne.MoyenControleId,
                    GroupeInstrumentId = sourceLigne.GroupeInstrumentId,
                    ValeurNominale = sourceLigne.ValeurNominale,
                    ToleranceSuperieure = sourceLigne.ToleranceSuperieure,
                    ToleranceInferieure = sourceLigne.ToleranceInferieure,
                    Unite = sourceLigne.Unite,
                    LimiteSpecTexte = sourceLigne.LimiteSpecTexte,
                    Observations = sourceLigne.Observations,
                    Instruction = sourceLigne.Instruction,
                    EstCritique = sourceLigne.EstCritique
                });
            }
            plan.PlanAssSections.Add(section);
        }
        return plan;
    }
}