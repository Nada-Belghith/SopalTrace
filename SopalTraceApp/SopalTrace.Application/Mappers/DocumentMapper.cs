using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Domain.Constants;
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
            Libre1 = entite.Libre1,
            Libre2 = entite.Libre2,
            Libre3 = entite.Libre3,
            // ConfigurationColonnesJson will be populated directly in DocumentService
            ConfigurationColonnesJson = null,
            LignesControlePoste = entite.TypeDocumentCode == "CTRL_POSTE"
                ? entite.DocumentSections
                    .SelectMany(s => s.DocumentLignes)
                    .OrderBy(l => l.OrdreAffiche)
                    .Select(l => new ControlePosteLigneDto
                    {
                        Id = l.Id,
                        // MachineCodeCtrlPoste stores the machine for CTRL_POSTE lines
                        MachineCode = l.MachineCodeCtrlPoste ?? l.MachineCode,
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
                InsertAfter = c.InsertAfter
            }).ToList() ?? new List<DocumentColonneDefDto>(),
            Sections = entite.DocumentSections?.Select(s => new DocumentSectionDto
            {
                Id = s.Id,
                EnteteId = s.EnteteId,
                OrdreAffiche = s.OrdreAffiche,
                LibelleSection = s.LibelleSection,
                TypeSectionId = s.TypeSectionId,
                PeriodiciteId = s.PeriodiciteId,
                RegleEchantillonnageId = s.RegleEchantillonnageId,
                Notes = s.Notes,
                NormeReference = s.NormeReference,
                NqaId = s.NqaId,
                Lignes = s.DocumentLignes?.Select(l => new DocumentLigneDto
                {
                    Id = l.Id,
                    EnteteId = l.EnteteId,
                    SectionId = l.SectionId,
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
                    ImageBase64 = l.ImageBase64,
                    MachineCode = l.MachineCode,
                    EstVerifPresence = l.EstVerifPresence,
                    DefauthequeId = l.DefauthequeId,
                    RefPlanProduit = l.RefPlanProduit,
                    MachineCodeCtrlPoste = l.MachineCodeCtrlPoste,
                    RisqueDefautId = l.RisqueDefautId,
                    Libre1 = l.Libre1,
                    Libre2 = l.Libre2,
                    Libre3 = l.Libre3,
                    Libre4 = l.Libre4,
                    Libre5 = l.Libre5,
                    ExtraColonnes = l.DocumentLigneExtraColonnes?.Select(ec => new DocumentExtraColonneDto
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
            Version = dto.VersionInitiale ?? 0,
            Statut = "ACTIF", // Default
            OperationCode = string.IsNullOrWhiteSpace(dto.OperationCode) ? null : dto.OperationCode,
            FormulaireId = formulaireId,
            LegendeMoyens = dto.LegendeMoyens,
            Remarques = dto.Remarques,
            NatureArticleCode = string.IsNullOrWhiteSpace(dto.NatureArticleCode) ? null : dto.NatureArticleCode,
            FamilleProduitFiniCode = string.IsNullOrWhiteSpace(dto.FamilleProduitFiniCode) ? null : dto.FamilleProduitFiniCode,
            PosteCode = string.IsNullOrWhiteSpace(dto.PosteCode) ? null : dto.PosteCode,
            Libre1 = dto.Libre1,
            Libre2 = (dto.Libre2 != null && dto.Libre2.TrimStart().StartsWith("[")) ? null : dto.Libre2,
            Libre3 = dto.Libre3,
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
                    RegleEchantillonnageId = secDto.RegleEchantillonnageId,
                    Notes = secDto.Notes,
                    NormeReference = secDto.NormeReference,
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
                            ImageBase64 = ligDto.ImageBase64,
                            MachineCode = string.IsNullOrWhiteSpace(ligDto.MachineCode) ? null : ligDto.MachineCode,
                            EstVerifPresence = ligDto.EstVerifPresence,
                            DefauthequeId = ligDto.DefauthequeId,
                            RefPlanProduit = string.IsNullOrWhiteSpace(ligDto.RefPlanProduit) ? null : ligDto.RefPlanProduit,
                            MachineCodeCtrlPoste = string.IsNullOrWhiteSpace(ligDto.MachineCodeCtrlPoste) ? null : ligDto.MachineCodeCtrlPoste,
                            RisqueDefautId = ligDto.RisqueDefautId,
                            Libre1 = ligDto.Libre1,
                            Libre2 = ligDto.Libre2,
                            Libre3 = ligDto.Libre3,
                            Libre4 = ligDto.Libre4,
                            Libre5 = ligDto.Libre5
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
}
