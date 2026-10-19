using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Domain.Constants;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class DocumentMapper
{
    public static DocumentEnteteDto ToDto(DocumentEntete entite, IEnumerable<RefFormulaireColonneDef>? colonnesActives = null)
    {
        if (entite == null) return null!;

        return new DocumentEnteteDto
        {
            Id = entite.Id,
            RowVersion = VersionsDocument.EnTexte(entite.RowVersion),
            TypeDocumentCode = entite.TypeDocumentCode,
            Nom = entite.Nom,
            Designation = entite.Designation,
            Version = entite.Version,
            Statut = entite.Statut,
            OperationCode = entite.OperationCode,
            FormulaireId = entite.FormulaireId,
            FormulaireCodeReference = entite.Formulaire?.CodeReference,
            LegendeMoyens = entite.LegendeMoyens,
            Remarques = entite.Remarques,
            CreePar = entite.CreePar,
            CreeLe = entite.CreeLe,
            ModifiePar = entite.ModifiePar,
            ModifieLe = entite.ModifieLe,
            NatureArticleCode = entite.NatureArticleCode,
            FamilleProduitFiniCode = entite.FamilleProduitFiniCode,
            PosteCode = entite.PosteCode,
            // Le type de robinet n'est pas stocké sur le document : il vient de la famille.
            TypeRobinetCode = entite.FamilleProduitFiniCodeNavigation?.TypeRobinetCode,
            // ConfigurationColonnesJson will be populated directly in DocumentService
            ConfigurationColonnesJson = null,
            LignesControlePoste = entite.TypeDocumentCode == "CTRL_POSTE"
                ? entite.DocumentSections
                    .SelectMany(s => s.DocumentLignes)
                    .OrderBy(l => l.OrdreAffiche)
                    .Select(l => new ControlePosteLigneDto
                    {
                        Id = l.Id,
                        MachineCode = l.MachineCode,
                        RisqueDefautId = l.RisqueDefautId,
                        LibelleDefaut = l.LibelleAffiche,
                        OrdreAffiche = l.OrdreAffiche
                    }).ToList()
                : new List<ControlePosteLigneDto>(),
            ColonneDefs = colonnesActives?.Select(c => new DocumentColonneDefDto
            {
                Id = c.Id,
                EnteteId = entite.Id,
                CleColonne = c.CleColonne,
                LabelAffiche = c.LabelAffiche,
                TypeValeur = c.TypeValeur,
                InsertAfter = c.InsertAfter,
                Options = c.ColumnAllowedValues.Where(v => v.Active).OrderBy(v => v.DisplayOrder).Select(v => v.Value).ToList()
            }).ToList() ?? new List<DocumentColonneDefDto>(),
            Sections = entite.DocumentSections?.OrderBy(s => s.OrdreAffiche).Select(s => new DocumentSectionDto
            {
                Id = s.Id,
                EnteteId = s.EnteteId,
                OrdreAffiche = s.OrdreAffiche,
                LibelleSection = s.LibelleSection,
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,
                Notes = s.Notes,
                NqaId = s.NqaId,
                Lignes = s.DocumentLignes?.OrderBy(l => l.OrdreAffiche).Select(l => new DocumentLigneDto
                {
                    Id = l.Id,
                    EnteteId = l.EnteteId,
                    SectionId = s.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    CaracteristiqueId = l.CaracteristiqueId,
                    LibelleAffiche = l.LibelleAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                    TypeControleId = l.TypeControleId,
                    MoyenControleId = l.MoyenControleId,
                    MoyenTexteLibre = l.MoyenTexteLibre,
                    InstrumentCode = l.InstrumentCode,
                    PeriodiciteId = l.PeriodiciteId,
                    LimiteSpecTexte = l.LimiteSpecTexte,
                    EstCritique = l.EstCritique,
                    Instruction = l.Instruction,
                    Observations = l.Observations,
                    ImageBase64 = l.EnDataUrl(),
                    MachineCode = l.MachineCode,
                    RisqueDefautId = l.RisqueDefautId,
                    ExtraColonnes = l.DocumentLigneExtraColonnes?.OrderBy(ec => ec.OrdreAffiche).Select(ec => new DocumentExtraColonneDto
                    {
                        Id = ec.Id,
                        CleColonne = ec.CleColonne,
                        ValeurColonne = ec.ValeurColonne,
                        OrdreAffiche = ec.OrdreAffiche
                    }).ToList() ?? new()
                }).ToList() ?? new()
            }).ToList() ?? new()
        };
    }

    public static DocumentEntete ToEntity(CreateDocumentRequestDto dto, string user, Guid? formulaireId = null)
    {
        var entite = new DocumentEntete
        {
            Id = Guid.NewGuid(),
            TypeDocumentCode = dto.TypeDocumentCode,
            Nom = dto.Nom,
            Designation = dto.Designation,
            Version = 0, // Set explicitly by the service
            Statut = "ACTIF", // Default
            OperationCode = string.IsNullOrWhiteSpace(dto.OperationCode) ? null : dto.OperationCode,
            FormulaireId = formulaireId ?? dto.FormulaireId,
            LegendeMoyens = dto.LegendeMoyens,
            Remarques = dto.Remarques,
            NatureArticleCode = string.IsNullOrWhiteSpace(dto.NatureArticleCode) ? null : dto.NatureArticleCode,
            FamilleProduitFiniCode = string.IsNullOrWhiteSpace(dto.FamilleProduitFiniCode) ? null : dto.FamilleProduitFiniCode,
            PosteCode = string.IsNullOrWhiteSpace(dto.PosteCode) ? null : dto.PosteCode,
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
                    Notes = secDto.Notes,
                    NqaId = secDto.NqaId
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
                            CaracteristiqueId = ligDto.CaracteristiqueId,
                            LibelleAffiche = ligDto.LibelleAffiche,
                            TypeCaracteristiqueId = ligDto.TypeCaracteristiqueId,
                            TypeControleId = ligDto.TypeControleId,
                            MoyenControleId = ligDto.MoyenControleId,
                            MoyenTexteLibre = ligDto.MoyenTexteLibre,
                            InstrumentCode = string.IsNullOrWhiteSpace(ligDto.InstrumentCode) ? null : ligDto.InstrumentCode,
                            PeriodiciteId = ligDto.PeriodiciteId,
                            LimiteSpecTexte = ligDto.LimiteSpecTexte,
                            EstCritique = ligDto.EstCritique,
                            Instruction = ligDto.Instruction,
                            Observations = ligDto.Observations,
                            DocumentLineImage = ImagesLigne.Creer(ligDto.ImageBase64),
                            MachineCode = string.IsNullOrWhiteSpace(ligDto.MachineCode) ? null : ligDto.MachineCode,
                            RisqueDefautId = ligDto.RisqueDefautId,
                        };

                        if (ligDto.ExtraColonnes != null)
                        {
                            foreach (var ecDto in ligDto.ExtraColonnes)
                            {
                                ligEntite.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                                {
                                    Id = Guid.NewGuid(),
                                    LigneId = ligEntite.Id,
                                    CleColonne = ecDto.CleColonne,
                                    ValeurColonne = ecDto.ValeurColonne,
                                    OrdreAffiche = ecDto.OrdreAffiche
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

    public static void UpdateEntityScalaires(DocumentEntete doc, UpdateDocumentRequestDto request)
    {
        if (request.Nom != null) doc.Nom = request.Nom;
        if (request.LegendeMoyens != null) doc.LegendeMoyens = request.LegendeMoyens;
        if (request.Remarques != null) doc.Remarques = request.Remarques;
    }

    public static DocumentSection CreateSection(CreateDocumentSectionDto dto, Guid entiteId)
    {
        return new DocumentSection
        {
            Id = (dto.Id.HasValue && dto.Id.Value != Guid.Empty) ? dto.Id.Value : Guid.NewGuid(),
            EnteteId = entiteId,
            OrdreAffiche = dto.OrdreAffiche,
            LibelleSection = dto.LibelleSection,
            TypeSectionId = dto.TypeSectionId,
            PeriodiciteId = dto.PeriodiciteId,
            Notes = dto.Notes,
            NqaId = dto.NqaId
        };
    }

    public static void UpdateSection(DocumentSection sec, CreateDocumentSectionDto dto)
    {
        sec.OrdreAffiche = dto.OrdreAffiche;
        sec.LibelleSection = dto.LibelleSection;
        sec.TypeSectionId = dto.TypeSectionId;
        sec.PeriodiciteId = dto.PeriodiciteId;
        sec.Notes = dto.Notes;
        sec.NqaId = dto.NqaId;
    }

    public static DocumentLigne CreateLigne(CreateDocumentLigneDto dto, Guid entiteId, Guid sectionId)
    {
        return new DocumentLigne
        {
            Id = (dto.Id.HasValue && dto.Id.Value != Guid.Empty) ? dto.Id.Value : Guid.NewGuid(),
            EnteteId = entiteId,
            SectionId = sectionId,
            OrdreAffiche = dto.OrdreAffiche,
            CaracteristiqueId = dto.CaracteristiqueId,
            LibelleAffiche = dto.LibelleAffiche,
            TypeCaracteristiqueId = dto.TypeCaracteristiqueId,
            TypeControleId = dto.TypeControleId,
            MoyenControleId = dto.MoyenControleId,
            MoyenTexteLibre = dto.MoyenTexteLibre,
            InstrumentCode = string.IsNullOrWhiteSpace(dto.InstrumentCode) ? null : dto.InstrumentCode,
            PeriodiciteId = dto.PeriodiciteId,
            LimiteSpecTexte = dto.LimiteSpecTexte,
            EstCritique = dto.EstCritique,
            Instruction = dto.Instruction,
            Observations = dto.Observations,
            DocumentLineImage = ImagesLigne.Creer(dto.ImageBase64),
            MachineCode = string.IsNullOrWhiteSpace(dto.MachineCode) ? null : dto.MachineCode,
            RisqueDefautId = dto.RisqueDefautId,
            DocumentLigneExtraColonnes = dto.ExtraColonnes?.Select(ec => new DocumentLigneExtraColonne
            {
                Id = Guid.NewGuid(),
                CleColonne = ec.CleColonne,
                ValeurColonne = ec.ValeurColonne,
                OrdreAffiche = ec.OrdreAffiche
            }).ToList() ?? new List<DocumentLigneExtraColonne>()
        };
    }

    public static void UpdateLigne(DocumentLigne lig, CreateDocumentLigneDto dto)
    {
        lig.OrdreAffiche = dto.OrdreAffiche;
        lig.CaracteristiqueId = dto.CaracteristiqueId;
        lig.LibelleAffiche = dto.LibelleAffiche;
        lig.TypeCaracteristiqueId = dto.TypeCaracteristiqueId;
        lig.TypeControleId = dto.TypeControleId;
        lig.MoyenControleId = dto.MoyenControleId;
        lig.MoyenTexteLibre = dto.MoyenTexteLibre;
        lig.InstrumentCode = string.IsNullOrWhiteSpace(dto.InstrumentCode) ? null : dto.InstrumentCode;
        lig.PeriodiciteId = dto.PeriodiciteId;
        lig.LimiteSpecTexte = dto.LimiteSpecTexte;
        lig.EstCritique = dto.EstCritique;
        lig.Instruction = dto.Instruction;
        lig.Observations = dto.Observations;
        lig.DefinirImage(dto.ImageBase64);
        lig.MachineCode = string.IsNullOrWhiteSpace(dto.MachineCode) ? null : dto.MachineCode;
        lig.RisqueDefautId = dto.RisqueDefautId;
    }
}
