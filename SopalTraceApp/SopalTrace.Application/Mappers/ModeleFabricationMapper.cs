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
            NatureArticleCode = dto.NatureComposantCode,
            OperationCode = dto.OperationCode,
            FamilleProduitFiniCode = dto.FamilleProduitCode,
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
}
