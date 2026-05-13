using SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
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
            OperationCode = plan.OperationCode ?? string.Empty,
            NatureComposantCode = plan.NatureComposantCode ?? string.Empty,
            FamilleCode = plan.FamilleProduitFiniCode ?? string.Empty,
            Code = $"ASS-{plan.OperationCode}-{plan.FamilleProduitFiniCode}-{plan.Version}", // Fallback
            EstModele = true,
            CodeArticleSage = null,
            Designation = plan.Designation,
            Nom = plan.Designation ?? "Plan Assemblage", // Utilise Designation au lieu de Nom
            Version = plan.Version,
            Statut = plan.Statut,
            CreePar = plan.CreePar,
            CreeLe = plan.CreeLe,
            ModifiePar = plan.ModifiePar,
            ModifieLe = plan.ModifieLe,
            LegendeMoyens = plan.LegendeMoyens,
            Remarques = plan.Remarques,
            Sections = plan.PlanAssSections?.Select(s => new SectionAssResponseDto
            {
                Id = s.Id,
                TypeSectionId = s.TypeSectionId ?? Guid.Empty,
                PeriodiciteId = s.PeriodiciteId ?? Guid.Empty,
                LibelleSection = s.LibelleSection ?? string.Empty,
                OrdreAffiche = s.OrdreAffiche,
                NormeReference = s.NormeReference,
                NqaId = s.NqaId,
                Notes = s.Notes,
                RegleEchantillonnageId = s.RegleEchantillonnageId,
                RegleEchantillonnageLibelle = s.RegleEchantillonnage?.Libelle ?? string.Empty,
                Lignes = s.PlanAssLignes?.Select(l => new LigneAssResponseDto
                {
                    Id = l.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId ?? Guid.Empty,
                    LibelleAffiche = l.LibelleAffiche ?? string.Empty,
                    TypeControleId = l.TypeControleId ?? Guid.Empty,
                    MoyenControleId = l.MoyenControleId,
                    InstrumentCode = l.InstrumentCode,
                    LimiteSpecTexte = l.LimiteSpecTexte,
                    Observations = l.Observations,
                    Instruction = l.Instruction,
                    EstCritique = l.EstCritique
                }).ToList() ?? new List<LigneAssResponseDto>()
            }).ToList() ?? new List<SectionAssResponseDto>()
        };
    }

    public static ModeleResponseDto MapperEntiteVersModeleDto(PlanAssEntete plan)
    {
        return new ModeleResponseDto
        {
            Id = plan.Id,
            OperationCode = plan.OperationCode,
            Code = plan.Designation ?? "ASS",
            Libelle = plan.Designation ?? "Plan Assemblage",
            TypeRobinetCode = plan.FamilleProduitFiniCode ?? string.Empty,
            NatureComposantCode = plan.NatureComposantCode ?? "PF",
            PosteCode = plan.PosteCode,
            FamilleProduitCode = plan.FamilleProduitFiniCode,
            Version = plan.Version,
            Statut = plan.Statut,
            CreePar = plan.CreePar,
            CreeLe = plan.CreeLe,
            ModifiePar = plan.ModifiePar,
            ModifieLe = plan.ModifieLe,
            LegendeMoyens = plan.LegendeMoyens,
            Notes = plan.Remarques,
            Sections = plan.PlanAssSections?.Select(s => new ModeleSectionResponseDto
            {
                Id = s.Id,
                OrdreAffiche = s.OrdreAffiche,
                LibelleSection = s.LibelleSection ?? "Section",
                Lignes = s.PlanAssLignes?.Select(l => new ModeleLigneResponseDto
                {
                    Id = l.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId ?? Guid.Empty,
                    LibelleAffiche = l.LibelleAffiche,
                    TypeControleId = l.TypeControleId ?? Guid.Empty,
                    MoyenControleId = l.MoyenControleId,
                    InstrumentCode = l.InstrumentCode,
                    LimiteSpecTexte = l.LimiteSpecTexte,
                    Observations = l.Observations,
                    Instruction = l.Instruction,
                    EstCritique = l.EstCritique,
                    MoyenTexteLibre = l.MoyenTexteLibre
                }).ToList() ?? new List<ModeleLigneResponseDto>()
            }).ToList() ?? new List<ModeleSectionResponseDto>()
        };
    }

    public static PlanAssEntete MapperModeleVersEntite(CreateModeleRequestDto request, string user)
    {
        var planId = Guid.NewGuid();
        var entete = new PlanAssEntete
        {
            Id = planId,
            OperationCode = request.OperationCode,
            FamilleProduitFiniCode = MapperHelper.NullIfEmpty(request.FamilleProduitCode),
            NatureComposantCode = MapperHelper.NullIfEmpty(request.NatureComposantCode),
            PosteCode = MapperHelper.NullIfEmpty(request.PosteCode),
            // Nom = request.Code, // On peut utiliser Nom pour stocker le Code si c'est l'intention
            Designation = request.Libelle,
            Statut = StatutsPlan.Actif,
            CreePar = user,
            CreeLe = DateTime.UtcNow,
            Remarques = request.Notes,
            LegendeMoyens = request.LegendeMoyens,
            PlanAssSections = new List<PlanAssSection>()
        };

        foreach (var s in request.Sections)
        {
            var sectionId = Guid.NewGuid();
            var section = new PlanAssSection
            {
                Id = sectionId,
                PlanEnteteId = planId,
                TypeSectionId = s.TypeSectionId,
                LibelleSection = s.LibelleSection,
                OrdreAffiche = s.OrdreAffiche,
                Notes = s.Notes,
                PlanAssLignes = new List<PlanAssLigne>()
            };

            foreach (var l in s.Lignes)
            {
                section.PlanAssLignes.Add(new PlanAssLigne
                {
                    Id = Guid.NewGuid(),
                    PlanEnteteId = planId,
                    SectionId = sectionId,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                    LibelleAffiche = l.LibelleAffiche,
                    TypeControleId = l.TypeControleId,
                    MoyenControleId = l.MoyenControleId,
                    InstrumentCode = l.InstrumentCode,
                    LimiteSpecTexte = l.LimiteSpecTexte,
                    Observations = l.Observations,
                    Instruction = l.Instruction,
                    EstCritique = l.EstCritique,
                    MoyenTexteLibre = l.MoyenTexteLibre
                });
            }
            entete.PlanAssSections.Add(section);
        }

        return entete;
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
            RegleEchantillonnageId = dto.RegleEchantillonnageId,
            RegleEchantillonnageLibelle = dto.RegleEchantillonnageLibelle,
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
            InstrumentCode = string.IsNullOrWhiteSpace(dto.InstrumentCode) ? null : dto.InstrumentCode,
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
        ligne.InstrumentCode = string.IsNullOrWhiteSpace(dto.InstrumentCode) ? null : dto.InstrumentCode;
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
            FamilleProduitFiniCode = source.FamilleProduitFiniCode,
            NatureComposantCode = source.NatureComposantCode,
            PosteCode = source.PosteCode,
            Designation = nouvelleDesig,
            Version = estModele ? source.Version + 1 : 0,
            Statut = StatutsPlan.Brouillon,
            CreePar = creePar,
            CreeLe = DateTime.UtcNow,
            LegendeMoyens = source.LegendeMoyens,
            Remarques = source.Remarques,
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
                RegleEchantillonnageId = sourceSection.RegleEchantillonnageId,
                RegleEchantillonnageLibelle = sourceSection.RegleEchantillonnageLibelle,
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
                    InstrumentCode = sourceLigne.InstrumentCode,
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

    public static string IncrementerSuffixeVersion(string original, int nouvelleVersion)
    {
        if (string.IsNullOrWhiteSpace(original)) return original;
        var regex = new System.Text.RegularExpressions.Regex(@"-[Vv]\d+$");

        if (nouvelleVersion == 1)
        {
            return regex.IsMatch(original) ? regex.Replace(original, "") : original;
        }

        if (regex.IsMatch(original)) return regex.Replace(original, $"-V{nouvelleVersion}");
        return $"{original}-V{nouvelleVersion}";
    }

    public static PlanAssEntete ConstruireNouvelleVersionModele(PlanAssEntete ancienModele, NouvelleVersionModeleRequestDto request, string auteur, int nouvelleVersion)
    {
        var planId = Guid.NewGuid();
        var nouveauPlan = new PlanAssEntete
        {
            Id = planId,
            OperationCode = request.OperationCode ?? ancienModele.OperationCode,
            FamilleProduitFiniCode = request.FamilleProduitCode ?? ancienModele.FamilleProduitFiniCode,
            Designation = IncrementerSuffixeVersion(request.Libelle ?? ancienModele.Designation ?? "Plan Assemblage", nouvelleVersion),
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            CreePar = auteur,
            CreeLe = DateTime.UtcNow,
            Remarques = request.Notes ?? ancienModele.Remarques,
            LegendeMoyens = request.LegendeMoyens ?? ancienModele.LegendeMoyens,
            PlanAssSections = new List<PlanAssSection>()
        };

        if (request.Sections?.Any() == true)
        {
            foreach (var s in request.Sections)
            {
                var sectionId = Guid.NewGuid();
                var section = new PlanAssSection
                {
                    Id = sectionId,
                    PlanEnteteId = planId,
                    TypeSectionId = s.TypeSectionId,
                    LibelleSection = s.LibelleSection,
                    OrdreAffiche = s.OrdreAffiche,
                    Notes = s.Notes,
                    PlanAssLignes = new List<PlanAssLigne>()
                };

                foreach (var l in s.Lignes)
                {
                    section.PlanAssLignes.Add(new PlanAssLigne
                    {
                        Id = Guid.NewGuid(),
                        PlanEnteteId = planId,
                        SectionId = sectionId,
                        OrdreAffiche = l.OrdreAffiche,
                        TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                        LibelleAffiche = l.LibelleAffiche,
                        TypeControleId = l.TypeControleId,
                        MoyenControleId = l.MoyenControleId,
                        InstrumentCode = l.InstrumentCode,
                        LimiteSpecTexte = l.LimiteSpecTexte,
                        Observations = l.Observations,
                        Instruction = l.Instruction,
                        EstCritique = l.EstCritique,
                        MoyenTexteLibre = l.MoyenTexteLibre
                    });
                }
                nouveauPlan.PlanAssSections.Add(section);
            }
        }
        else
        {
            // Si pas de sections dans la requête, on duplique celles de l'ancien (cas rare mais possible)
            foreach (var sourceSection in ancienModele.PlanAssSections ?? Enumerable.Empty<PlanAssSection>())
            {
                var sectionId = Guid.NewGuid();
                var section = new PlanAssSection
                {
                    Id = sectionId,
                    PlanEnteteId = planId,
                    TypeSectionId = sourceSection.TypeSectionId,
                    LibelleSection = sourceSection.LibelleSection,
                    OrdreAffiche = sourceSection.OrdreAffiche,
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
                        InstrumentCode = sourceLigne.InstrumentCode,
                        LimiteSpecTexte = sourceLigne.LimiteSpecTexte,
                        Observations = sourceLigne.Observations,
                        Instruction = sourceLigne.Instruction,
                        EstCritique = sourceLigne.EstCritique
                    });
                }
                nouveauPlan.PlanAssSections.Add(section);
            }
        }

        return nouveauPlan;
    }
}
