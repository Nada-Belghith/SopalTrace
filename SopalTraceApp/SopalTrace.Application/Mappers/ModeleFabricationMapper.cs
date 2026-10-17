using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Domain.Constants;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SopalTrace.Application.Mappers;

public static class ModeleFabricationMapper
{
    public static ModeleResponseDto ToDto(DocumentEntete entite)
    {
        if (entite == null) return null!;

        return new ModeleResponseDto
        {
            Id = entite.Id,
            RowVersion = VersionsDocument.EnTexte(entite.RowVersion),
            Code = entite.Nom,
            Libelle = entite.Designation ?? "",
            TypeRobinetCode = "",
            NatureComposantCode = entite.NatureArticleCode ?? "",
            OperationCode = entite.OperationCode ?? "",
            FamilleProduitCode = entite.FamilleProduitFiniCode,
            Version = entite.Version,
            Statut = entite.Statut,
            Notes = entite.Remarques,
            LegendeMoyens = entite.LegendeMoyens,
            CreePar = entite.CreePar,
            CreeLe = entite.CreeLe,
            CodeReferenceFormulaire = entite.Formulaire?.CodeReference,
            FormulaireVersion = entite.Formulaire?.Version,
            Sections = entite.DocumentSections?.Select(s => new ModeleSectionResponseDto
            {
                Id = s.Id,
                OrdreAffiche = s.OrdreAffiche,
                LibelleSection = s.LibelleSection,
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,
                FrequenceLibelle = s.Periodicite != null 
                    ? (s.Periodicite.Unite == "HEURE" 
                        ? (s.Periodicite.NbrPieces == 100 ? $"100% des pièces / {s.Periodicite.NbrHeures} heure(s)" : $"{s.Periodicite.NbrPieces} pièce(s) / {s.Periodicite.NbrHeures} heure(s)") 
                        : $"une série de {s.Periodicite.NbrPieces} pièce(s)") 
                    : null,
                Lignes = s.DocumentLignes?.Select(l => new ModeleLigneResponseDto
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
                    ImageBase64 = l.EnDataUrl(),
                    ColonnesSupplementaires = l.DocumentLigneExtraColonnes != null && l.DocumentLigneExtraColonnes.Any()
                        ? JsonSerializer.Serialize(l.DocumentLigneExtraColonnes.ToDictionary(c => c.CleColonne, c => c.ValeurColonne))
                        : null
                }).ToList() ?? new()
            }).ToList() ?? new()
        };
    }

    public static DocumentEntete ToEntity(CreateModeleRequestDto dto, string user, Guid? formulaireId = null)
    {
        var entite = new DocumentEntete
        {
            Id = Guid.NewGuid(),
            TypeDocumentCode = TypesDocument.ModeleFabrication,
            Portee = PorteesDocument.Generique,
            Nom = dto.Code,
            Designation = dto.Libelle,
            NatureArticleCode = string.IsNullOrWhiteSpace(dto.NatureComposantCode) ? null : dto.NatureComposantCode,
            OperationCode = string.IsNullOrWhiteSpace(dto.OperationCode) ? null : dto.OperationCode,
            FamilleProduitFiniCode = string.IsNullOrWhiteSpace(dto.FamilleProduitCode) ? null : dto.FamilleProduitCode,
            Remarques = dto.Notes,
            LegendeMoyens = dto.LegendeMoyens,
            // Version du formulaire, posée par le service.
            Version = 0,
            Statut = DocumentStatuts.Actif,
            FormulaireId = formulaireId,
            CreePar = user,
            CreeLe = DateTime.Now
        };

        if (dto.Sections != null)
        {
            foreach (var secDto in dto.Sections)
            {
                var secEntite = new DocumentSection
                {
                    Id = Guid.NewGuid(),
                    EnteteId = entite.Id,
                    OrdreAffiche = secDto.OrdreAffiche,
                    LibelleSection = secDto.LibelleSection,
                    TypeSectionId = secDto.TypeSectionId,
                    PeriodiciteId = secDto.PeriodiciteId,
                };

                if (secDto.Lignes != null)
                {
                    foreach (var ligDto in secDto.Lignes)
                    {
                        var ligEntite = new DocumentLigne
                        {
                            Id = Guid.NewGuid(),
                            EnteteId = entite.Id,
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
                            DocumentLineImage = ImagesLigne.Creer(ligDto.ImageBase64)
                        };

                        if (ligDto.ExtraColonnes != null && ligDto.ExtraColonnes.Any())
                        {
                            foreach (var ext in ligDto.ExtraColonnes)
                            {
                                ligEntite.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
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

                        secEntite.DocumentLignes.Add(ligEntite);
                    }
                }

                entite.DocumentSections.Add(secEntite);
            }
        }

        return entite;
    }

    public static List<SectionModeleEditDto> BuildSectionsFromExistingModel(DocumentEntete modele)
    {
        if (modele?.DocumentSections == null) return new List<SectionModeleEditDto>();

        return modele.DocumentSections.Select(s => new SectionModeleEditDto
        {
            LibelleSection = s.LibelleSection,
            OrdreAffiche = s.OrdreAffiche,
            Lignes = s.DocumentLignes?.Select(l => new LigneModeleEditDto
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
                ImageBase64 = l.EnDataUrl(),
                ExtraColonnes = l.DocumentLigneExtraColonnes?.Select(c => new CreateModeleExtraColonneDto
                {
                    CleColonne = c.CleColonne,
                    ValeurColonne = c.ValeurColonne,
                    OrdreAffiche = c.OrdreAffiche
                }).ToList() ?? new List<CreateModeleExtraColonneDto>()
            }).ToList() ?? new List<LigneModeleEditDto>()
        }).ToList();
    }

    public static DocumentSection CreateSection(SectionModeleEditDto dto, Guid modeleId)
    {
        return new DocumentSection
        {
            Id = Guid.NewGuid(),
            EnteteId = modeleId,
            LibelleSection = dto.LibelleSection ?? "",
            OrdreAffiche = dto.OrdreAffiche,
            TypeSectionId = dto.TypeSectionId,
            PeriodiciteId = dto.PeriodiciteId,
            DocumentLignes = new List<DocumentLigne>()
        };
    }

    public static void UpdateSection(DocumentSection sec, SectionModeleEditDto dto)
    {
        sec.OrdreAffiche = dto.OrdreAffiche;
        sec.LibelleSection = dto.LibelleSection ?? "";
        sec.TypeSectionId = dto.TypeSectionId;
        sec.PeriodiciteId = dto.PeriodiciteId;
    }

    public static DocumentLigne CreateLigne(LigneModeleEditDto dto, Guid modeleId, Guid sectionId)
    {
        var modeleLigne = new DocumentLigne
        {
            Id = Guid.NewGuid(),
            EnteteId = modeleId,
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
            DocumentLineImage = ImagesLigne.Creer(dto.ImageBase64),
            DocumentLigneExtraColonnes = new List<DocumentLigneExtraColonne>()
        };

        if (dto.ExtraColonnes != null)
        {
            foreach (var ec in dto.ExtraColonnes)
            {
                modeleLigne.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
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

    public static void UpdateLigne(DocumentLigne lig, LigneModeleEditDto dto)
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
        lig.DefinirImage(dto.ImageBase64);
    }

    public static void MergerExtraColonnes(DocumentLigne lig, List<CreateModeleExtraColonneDto> incoming, SopalTrace.Application.Interfaces.IUnitOfWork unitOfWork)
    {
        var existing = lig.DocumentLigneExtraColonnes.ToList();

        foreach (var ec in existing.Where(e => !incoming.Any(i => i.CleColonne == e.CleColonne)))
        {
            unitOfWork.ModeleFabricationEnteteRepository.RemoveExtraColonne(ec);
            lig.DocumentLigneExtraColonnes.Remove(ec);
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
                var nouvelle = new DocumentLigneExtraColonne
                {
                    Id = Guid.NewGuid(),
                    LigneId = lig.Id,
                    Ligne = lig,
                    CleColonne = inc.CleColonne,
                    ValeurColonne = inc.ValeurColonne,
                    OrdreAffiche = inc.OrdreAffiche
                };
                lig.DocumentLigneExtraColonnes.Add(nouvelle);
                unitOfWork.Ajouter(nouvelle);
            }
        }
    }
}
