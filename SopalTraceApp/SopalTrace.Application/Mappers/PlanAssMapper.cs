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
    public static PlanAssResponseDto MapperEntitePlanVersDto(PlanAssemblageEntete plan)
    {
        return new PlanAssResponseDto
        {
            Id = plan.Id,
            OperationCode = plan.OperationCode ?? string.Empty,
            NatureComposantCode = plan.NatureArticleCode ?? string.Empty,
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
            Remarques = string.Empty,
            Sections = plan.PlanAssemblageSections?.Select(s => new SectionAssResponseDto
            {
                Id = s.Id,
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,

                LibelleSection = s.LibelleSection ?? string.Empty,
                OrdreAffiche = s.OrdreAffiche,
                NormeReference = s.NormeReference,
                NqaId = s.NqaId,
                Notes = s.Notes,
                RegleEchantillonnageId = s.RegleEchantillonnageId,
                RegleEchantillonnageLibelle = s.RegleEchantillonnage?.Libelle ?? s.RegleEchantillonnageLibelle ?? string.Empty,
                FrequenceLibelle = s.Periodicite?.Libelle ?? string.Empty,
                ModeFreq = s.PeriodiciteId != null ? "VARIABLE" : (s.RegleEchantillonnageId != null ? "FIXE" : "SANS"),
                FreqNum = s.Periodicite?.FrequenceNum ?? 1,
                TypeVariable = s.Periodicite?.FrequenceUnite ?? "HEURE",
                FreqHours = (s.Periodicite?.FrequenceUnite == "1_HEURE" || s.Periodicite?.FrequenceUnite == "PCT_HEURE") ? 1 : 1,
                Lignes = s.PlanAssemblageLignes?.Select(l => new LigneAssResponseDto
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

    public static ModeleResponseDto MapperEntiteVersModeleDto(PlanAssemblageEntete plan)
    {
        return new ModeleResponseDto
        {
            Id = plan.Id,
            OperationCode = plan.OperationCode,
            Code = plan.Designation ?? "ASS",
            Libelle = plan.Designation ?? "Plan Assemblage",
            TypeRobinetCode = plan.FamilleProduitFiniCode ?? string.Empty,
            NatureComposantCode = plan.NatureArticleCode ?? "PF",
            PosteCode = plan.PosteCode,
            FamilleProduitCode = plan.FamilleProduitFiniCode,
            Version = plan.Version,
            Statut = plan.Statut,
            CreePar = plan.CreePar,
            CreeLe = plan.CreeLe,
            ModifiePar = plan.ModifiePar,
            ModifieLe = plan.ModifieLe,
            LegendeMoyens = plan.LegendeMoyens,
            Notes = string.Empty,
            Sections = plan.PlanAssemblageSections?.Select(s => new ModeleSectionResponseDto
            {
                Id = s.Id,
                OrdreAffiche = s.OrdreAffiche,
                LibelleSection = s.LibelleSection ?? "Section",
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,

                RegleEchantillonnageId = s.RegleEchantillonnageId,
                RegleEchantillonnageLibelle = s.RegleEchantillonnageLibelle,
                Lignes = s.PlanAssemblageLignes?.Select(l => new ModeleLigneResponseDto
                {
                    Id = l.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId ,
                    LibelleAffiche = l.LibelleAffiche,
                    TypeControleId = l.TypeControleId ,
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

    public static PlanAssemblageEntete MapperModeleVersEntite(CreateModeleRequestDto request, string user)
    {
        var planId = Guid.NewGuid();
        var entete = new PlanAssemblageEntete
        {
            Id = planId,
            OperationCode = request.OperationCode,
            FamilleProduitFiniCode = MapperHelper.NullIfEmpty(request.FamilleProduitCode),
            NatureArticleCode = MapperHelper.NullIfEmpty(request.NatureComposantCode),
            PosteCode = MapperHelper.NullIfEmpty(request.PosteCode),
            // Nom = request.Code, // On peut utiliser Nom pour stocker le Code si c'est l'intention
            Designation = request.Libelle,
            Statut = StatutsPlan.Actif,
            CreePar = user,
            CreeLe = DateTime.UtcNow,
            //Remarques = request.Notes,
            LegendeMoyens = request.LegendeMoyens,
            PlanAssemblageSections = new List<PlanAssemblageSection>()
        };

        foreach (var s in request.Sections)
        {
            var sectionId = Guid.NewGuid();
            var section = new PlanAssemblageSection
            {
                Id = sectionId,
                PlanEnteteId = planId,
                TypeSectionId = (s.TypeSectionId == null || s.TypeSectionId == Guid.Empty) ? null : s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,

                LibelleSection = s.LibelleSection,
                RegleEchantillonnageId = s.RegleEchantillonnageId,
                RegleEchantillonnageLibelle = s.RegleEchantillonnageLibelle,
                OrdreAffiche = s.OrdreAffiche,
                Notes = s.Notes,
                PlanAssemblageLignes = new List<PlanAssemblageLigne>()
            };

            foreach (var l in s.Lignes)
            {
                section.PlanAssemblageLignes.Add(new PlanAssemblageLigne
                {
                    Id = Guid.NewGuid(),
                    PlanEnteteId = planId,
                    SectionId = sectionId,
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
                });
            }
            entete.PlanAssemblageSections.Add(section);
        }

        return entete;
    }

    public static PlanAssemblageSection ConstruireNouvelleSection(Guid planId, SectionAssEditDto dto)
    {
        return new PlanAssemblageSection
        {
            PlanEnteteId = planId,
            TypeSectionId = (dto.TypeSectionId == null || dto.TypeSectionId == Guid.Empty) ? null : dto.TypeSectionId,
            PeriodiciteId = dto.PeriodiciteId,
            OrdreAffiche = dto.OrdreAffiche,
            LibelleSection = string.IsNullOrWhiteSpace(dto.LibelleSection) ? "NOUVELLE SECTION" : dto.LibelleSection,
            NormeReference = dto.NormeReference,
            NqaId = dto.NqaId,
            Notes = dto.Notes,
            RegleEchantillonnageId = dto.RegleEchantillonnageId,
            RegleEchantillonnageLibelle = dto.RegleEchantillonnageLibelle,
            PlanAssemblageLignes = new List<PlanAssemblageLigne>()
        };
    }

    public static PlanAssemblageLigne ConstruireNouvelleLigne(Guid planId, Guid sectionId, LigneAssEditDto dto)
    {
        return new PlanAssemblageLigne
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

    public static void MettreAJourEntiteLigne(PlanAssemblageLigne ligne, LigneAssEditDto dto)
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

    public static PlanAssemblageEntete DupliquerEntitePlan(PlanAssemblageEntete source, bool estModele, string? nouveauCodeArticle, string? nouvelleDesig, string creePar, string? motif)
    {
        var planId = Guid.NewGuid();
        var plan = new PlanAssemblageEntete
        {
            Id = planId,
            OperationCode = source.OperationCode,
            FamilleProduitFiniCode = source.FamilleProduitFiniCode,
            NatureArticleCode = source.NatureArticleCode,
            PosteCode = source.PosteCode,
            Designation = !string.IsNullOrWhiteSpace(nouvelleDesig) ? nouvelleDesig : source.Designation,
            Version = estModele ? source.Version + 1 : 0,
            Statut = StatutsPlan.Brouillon,
            CreePar = creePar,
            CreeLe = DateTime.UtcNow,
            LegendeMoyens = source.LegendeMoyens,
            //Remarques = source.Remarques,
            PlanAssemblageSections = new List<PlanAssemblageSection>()
        };

        foreach (var sourceSection in source.PlanAssemblageSections ?? Enumerable.Empty<PlanAssemblageSection>())
        {
            var sectionId = Guid.NewGuid();
            var section = new PlanAssemblageSection
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
                PlanAssemblageLignes = new List<PlanAssemblageLigne>()
            };

            foreach (var sourceLigne in sourceSection.PlanAssemblageLignes ?? Enumerable.Empty<PlanAssemblageLigne>())
            {
                section.PlanAssemblageLignes.Add(new PlanAssemblageLigne
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
            plan.PlanAssemblageSections.Add(section);
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

    public static PlanAssemblageEntete ConstruireNouvelleVersionModele(PlanAssemblageEntete ancienModele, NouvelleVersionModeleRequestDto request, string auteur, int nouvelleVersion)
    {
        var planId = Guid.NewGuid();
        var nouveauPlan = new PlanAssemblageEntete
        {
            Id = planId,
            OperationCode = request.OperationCode ?? ancienModele.OperationCode,
            FamilleProduitFiniCode = request.FamilleProduitCode ?? ancienModele.FamilleProduitFiniCode,
            NatureArticleCode = request.NatureComposantCode ?? ancienModele.NatureArticleCode,
            PosteCode = request.PosteCode ?? ancienModele.PosteCode,
            Designation = IncrementerSuffixeVersion(request.Libelle ?? ancienModele.Designation ?? "Plan Assemblage", nouvelleVersion),
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            CreePar = auteur,
            CreeLe = DateTime.UtcNow,
            //Remarques = request.Notes ?? ancienModele.Remarques,
            LegendeMoyens = request.LegendeMoyens ?? ancienModele.LegendeMoyens,
            PlanAssemblageSections = new List<PlanAssemblageSection>()
        };

        if (request.Sections?.Any() == true)
        {
            foreach (var s in request.Sections)
            {
                var sectionId = Guid.NewGuid();
                var section = new PlanAssemblageSection
                {
                    Id = sectionId,
                    PlanEnteteId = planId,
                    TypeSectionId = (s.TypeSectionId == null || s.TypeSectionId == Guid.Empty) ? null : s.TypeSectionId,
                    PeriodiciteId = s.PeriodiciteId,

                    LibelleSection = s.LibelleSection,
                    RegleEchantillonnageId = s.RegleEchantillonnageId,
                    RegleEchantillonnageLibelle = s.RegleEchantillonnageLibelle,
                    OrdreAffiche = s.OrdreAffiche,
                    Notes = s.Notes,
                    PlanAssemblageLignes = new List<PlanAssemblageLigne>()
                };

                foreach (var l in s.Lignes)
                {
                    section.PlanAssemblageLignes.Add(new PlanAssemblageLigne
                    {
                        Id = Guid.NewGuid(),
                        PlanEnteteId = planId,
                        SectionId = sectionId,
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
                    });
                }
                nouveauPlan.PlanAssemblageSections.Add(section);
            }
        }
        else
        {
            // Si pas de sections dans la requête, on duplique celles de l'ancien (cas rare mais possible)
            foreach (var sourceSection in ancienModele.PlanAssemblageSections ?? Enumerable.Empty<PlanAssemblageSection>())
            {
                var sectionId = Guid.NewGuid();
                var section = new PlanAssemblageSection
                {
                    Id = sectionId,
                    PlanEnteteId = planId,
                    TypeSectionId = sourceSection.TypeSectionId,
                    LibelleSection = sourceSection.LibelleSection,
                    OrdreAffiche = sourceSection.OrdreAffiche,
                    Notes = sourceSection.Notes,
                    PlanAssemblageLignes = new List<PlanAssemblageLigne>()
                };

                foreach (var sourceLigne in sourceSection.PlanAssemblageLignes ?? Enumerable.Empty<PlanAssemblageLigne>())
                {
                    section.PlanAssemblageLignes.Add(new PlanAssemblageLigne
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
                nouveauPlan.PlanAssemblageSections.Add(section);
            }
        }

        return nouveauPlan;
    }
}
