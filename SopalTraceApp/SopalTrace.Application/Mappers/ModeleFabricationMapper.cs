using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SopalTrace.Application.Mappers;

public static class ModeleFabricationMapper
{
    public static ModeleResponseDto ToDto(ModeleFabricationEntete entite)
    {
        if (entite == null) return null!;

        return new ModeleResponseDto
        {
            Id = entite.Id,
            Code = entite.Code,
            Libelle = entite.Libelle,
            TypeRobinetCode = "",
            NatureComposantCode = entite.NatureArticleCode ?? "",
            OperationCode = entite.OperationCode ?? "",
            FamilleProduitCode = entite.FamilleProduitFiniCode,
            Version = entite.Version,
            Statut = entite.Statut,
            Notes = entite.Notes,
            LegendeMoyens = entite.LegendeMoyens,
            CreePar = entite.CreePar,
            CreeLe = entite.CreeLe,
            CodeReferenceFormulaire = entite.Formulaire?.CodeReference,
            FormulaireVersion = entite.Formulaire?.Version,
            Sections = entite.ModeleFabricationSections?.Select(s => new ModeleSectionResponseDto
            {
                Id = s.Id,
                OrdreAffiche = s.OrdreAffiche,
                LibelleSection = s.LibelleSection,
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,
                FrequenceLibelle = s.Periodicite?.Libelle,
                RegleEchantillonnageId = s.RegleEchantillonnageId,
                RegleEchantillonnageLibelle = s.RegleEchantillonnage?.Libelle,
                Lignes = s.ModeleFabricationLignes?.Select(l => new ModeleLigneResponseDto
                {
                    Id = l.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                    LibelleAffiche = l.LibelleAffiche,
                    TypeControleId = l.TypeControleId,
                    MoyenControleId = l.MoyenControleId,
                    InstrumentCode = l.InstrumentCode,
                    PeriodiciteId = l.PeriodiciteId,
                    Instruction = l.Instruction,
                    Observations = l.Observations,
                    EstCritique = l.EstCritique,
                    LimiteSpecTexte = l.LimiteSpecTexte,
                    MoyenTexteLibre = l.MoyenTexteLibre,
                    ImageBase64 = l.ImageBase64,
                    ColonnesSupplementaires = l.ModeleFabricationLigneExtraColonnes != null && l.ModeleFabricationLigneExtraColonnes.Any()
                        ? JsonSerializer.Serialize(l.ModeleFabricationLigneExtraColonnes.ToDictionary(c => c.CleColonne, c => c.ValeurColonne))
                        : null
                }).ToList() ?? new()
            }).ToList() ?? new()
        };
    }

    public static ModeleFabricationEntete ToEntity(CreateModeleRequestDto dto, string user, Guid? formulaireId = null)
    {
        var entite = new ModeleFabricationEntete
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Libelle = dto.Libelle,
            NatureArticleCode = string.IsNullOrWhiteSpace(dto.NatureComposantCode) ? null! : dto.NatureComposantCode,
            OperationCode = string.IsNullOrWhiteSpace(dto.OperationCode) ? null! : dto.OperationCode,
            FamilleProduitFiniCode = string.IsNullOrWhiteSpace(dto.FamilleProduitCode) ? null! : dto.FamilleProduitCode,
            Notes = dto.Notes,
            LegendeMoyens = dto.LegendeMoyens,
            Version = dto.VersionInitiale ?? 0,
            Statut = "ACTIF",
            FormulaireId = formulaireId,
            CreePar = user,
            CreeLe = DateTime.Now
        };

        if (dto.Sections != null)
        {
            foreach (var secDto in dto.Sections)
            {
                var secEntite = new ModeleFabricationSection
                {
                    Id = Guid.NewGuid(),
                    ModeleEnteteId = entite.Id,
                    OrdreAffiche = secDto.OrdreAffiche,
                    LibelleSection = secDto.LibelleSection,
                    TypeSectionId = secDto.TypeSectionId,
                    PeriodiciteId = secDto.PeriodiciteId,
                    RegleEchantillonnageId = secDto.RegleEchantillonnageId,
                };

                if (secDto.Lignes != null)
                {
                    foreach (var ligDto in secDto.Lignes)
                    {
                        var ligEntite = new ModeleFabricationLigne
                        {
                            Id = Guid.NewGuid(),
                            SectionId = secEntite.Id,
                            OrdreAffiche = ligDto.OrdreAffiche,
                            TypeCaracteristiqueId = ligDto.TypeCaracteristiqueId,
                            LibelleAffiche = ligDto.LibelleAffiche,
                            TypeControleId = ligDto.TypeControleId,
                            MoyenControleId = ligDto.MoyenControleId,
                            InstrumentCode = string.IsNullOrWhiteSpace(ligDto.InstrumentCode) ? null : ligDto.InstrumentCode,
                            PeriodiciteId = ligDto.PeriodiciteId,
                            Instruction = ligDto.Instruction,
                            Observations = ligDto.Observations,
                            EstCritique = ligDto.EstCritique,
                            LimiteSpecTexte = ligDto.LimiteSpecTexte,
                            MoyenTexteLibre = string.IsNullOrWhiteSpace(ligDto.MoyenTexteLibre) ? null : ligDto.MoyenTexteLibre,
                            ImageBase64 = ligDto.ImageBase64
                        };

                        if (ligDto.ExtraColonnes != null && ligDto.ExtraColonnes.Any())
                        {
                            foreach (var ext in ligDto.ExtraColonnes)
                            {
                                ligEntite.ModeleFabricationLigneExtraColonnes.Add(new ModeleFabricationLigneExtraColonne
                                {
                                    Id = Guid.NewGuid(),
                                    LigneId = ligEntite.Id,
                                    Ligne = ligEntite,
                                    CleColonne = ext.CleColonne,
                                    ValeurColonne = ext.ValeurColonne,
                                    OrdreAffiche = ext.OrdreAffiche
                                });
                            }
                        }

                        secEntite.ModeleFabricationLignes.Add(ligEntite);
                    }
                }

                entite.ModeleFabricationSections.Add(secEntite);
            }
        }

        return entite;
    }

    public static List<SectionModeleEditDto> BuildSectionsFromExistingModel(ModeleFabricationEntete modele)
    {
        if (modele?.ModeleFabricationSections == null) return new List<SectionModeleEditDto>();

        return modele.ModeleFabricationSections.Select(s => new SectionModeleEditDto
        {
            LibelleSection = s.LibelleSection,
            OrdreAffiche = s.OrdreAffiche,
            Lignes = s.ModeleFabricationLignes?.Select(l => new LigneModeleEditDto
            {
                OrdreAffiche = l.OrdreAffiche,
                TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                LibelleAffiche = l.LibelleAffiche,
                TypeControleId = l.TypeControleId,
                MoyenControleId = l.MoyenControleId,
                MoyenTexteLibre = string.IsNullOrWhiteSpace(l.MoyenTexteLibre) ? null : l.MoyenTexteLibre,
                InstrumentCode = l.InstrumentCode,
                PeriodiciteId = l.PeriodiciteId,
                LimiteSpecTexte = l.LimiteSpecTexte,
                EstCritique = l.EstCritique,
                Instruction = l.Instruction,
                Observations = l.Observations,
                ImageBase64 = l.ImageBase64,
                ExtraColonnes = l.ModeleFabricationLigneExtraColonnes?.Select(c => new CreateModeleExtraColonneDto
                {
                    CleColonne = c.CleColonne,
                    ValeurColonne = c.ValeurColonne,
                    OrdreAffiche = c.OrdreAffiche
                }).ToList() ?? new List<CreateModeleExtraColonneDto>()
            }).ToList() ?? new List<LigneModeleEditDto>()
        }).ToList();
    }

    public static ModeleFabricationSection CreateSection(SectionModeleEditDto dto, Guid modeleId)
    {
        return new ModeleFabricationSection
        {
            Id = Guid.NewGuid(),
            ModeleEnteteId = modeleId,
            LibelleSection = dto.LibelleSection ?? "",
            OrdreAffiche = dto.OrdreAffiche,
            TypeSectionId = dto.TypeSectionId,
            PeriodiciteId = dto.PeriodiciteId,
            RegleEchantillonnageId = dto.RegleEchantillonnageId,
            ModeleFabricationLignes = new List<ModeleFabricationLigne>()
        };
    }

    public static void UpdateSection(ModeleFabricationSection sec, SectionModeleEditDto dto)
    {
        sec.OrdreAffiche = dto.OrdreAffiche;
        sec.LibelleSection = dto.LibelleSection ?? "";
        sec.TypeSectionId = dto.TypeSectionId;
        sec.PeriodiciteId = dto.PeriodiciteId;
        sec.RegleEchantillonnageId = dto.RegleEchantillonnageId;
    }

    public static ModeleFabricationLigne CreateLigne(LigneModeleEditDto dto, Guid modeleId, Guid sectionId)
    {
        var modeleLigne = new ModeleFabricationLigne
        {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            OrdreAffiche = dto.OrdreAffiche,
            LibelleAffiche = dto.LibelleAffiche,
            TypeCaracteristiqueId = dto.TypeCaracteristiqueId,
            TypeControleId = dto.TypeControleId,
            MoyenControleId = dto.MoyenControleId,
            MoyenTexteLibre = string.IsNullOrWhiteSpace(dto.MoyenTexteLibre) ? null : dto.MoyenTexteLibre,
            InstrumentCode = dto.InstrumentCode,
            PeriodiciteId = dto.PeriodiciteId,
            LimiteSpecTexte = dto.LimiteSpecTexte,
            EstCritique = dto.EstCritique,
            Instruction = dto.Instruction,
            Observations = dto.Observations,
            ImageBase64 = dto.ImageBase64,
            ModeleFabricationLigneExtraColonnes = new List<ModeleFabricationLigneExtraColonne>()
        };

        if (dto.ExtraColonnes != null)
        {
            foreach (var ec in dto.ExtraColonnes)
            {
                modeleLigne.ModeleFabricationLigneExtraColonnes.Add(new ModeleFabricationLigneExtraColonne
                {
                    Id = Guid.NewGuid(),
                    LigneId = modeleLigne.Id,
                    CleColonne = ec.CleColonne,
                    ValeurColonne = ec.ValeurColonne,
                    OrdreAffiche = ec.OrdreAffiche
                });
            }
        }

        return modeleLigne;
    }

    public static void UpdateLigne(ModeleFabricationLigne lig, LigneModeleEditDto dto)
    {
        lig.OrdreAffiche = dto.OrdreAffiche;
        lig.LibelleAffiche = dto.LibelleAffiche;
        lig.TypeCaracteristiqueId = dto.TypeCaracteristiqueId;
        lig.TypeControleId = dto.TypeControleId;
        lig.MoyenControleId = dto.MoyenControleId;
        lig.MoyenTexteLibre = string.IsNullOrWhiteSpace(dto.MoyenTexteLibre) ? null : dto.MoyenTexteLibre;
        lig.InstrumentCode = dto.InstrumentCode;
        lig.PeriodiciteId = dto.PeriodiciteId;
        lig.LimiteSpecTexte = dto.LimiteSpecTexte;
        lig.EstCritique = dto.EstCritique;
        lig.Instruction = dto.Instruction;
        lig.Observations = dto.Observations;
        lig.ImageBase64 = dto.ImageBase64;
    }

    public static void MergerExtraColonnes(ModeleFabricationLigne lig, List<CreateModeleExtraColonneDto> incoming, SopalTrace.Application.Interfaces.IUnitOfWork unitOfWork)
    {
        var existing = lig.ModeleFabricationLigneExtraColonnes.ToList();

        foreach (var ec in existing.Where(e => !incoming.Any(i => i.CleColonne == e.CleColonne)))
        {
            unitOfWork.ModeleFabricationEnteteRepository.RemoveExtraColonne(ec);
            lig.ModeleFabricationLigneExtraColonnes.Remove(ec);
        }

        foreach (var inc in incoming)
        {
            var found = existing.FirstOrDefault(e => e.CleColonne == inc.CleColonne);
            if (found != null)
            {
                found.ValeurColonne = inc.ValeurColonne;
                found.OrdreAffiche = inc.OrdreAffiche;
            }
            else
            {
                lig.ModeleFabricationLigneExtraColonnes.Add(new ModeleFabricationLigneExtraColonne
                {
                    Id = Guid.NewGuid(),
                    LigneId = lig.Id,
                    Ligne = lig,
                    CleColonne = inc.CleColonne,
                    ValeurColonne = inc.ValeurColonne,
                    OrdreAffiche = inc.OrdreAffiche
                });
            }
        }
    }
}
