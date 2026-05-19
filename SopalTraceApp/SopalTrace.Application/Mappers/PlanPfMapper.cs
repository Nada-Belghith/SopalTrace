#nullable enable

using SopalTrace.Application.DTOs.QualityPlans.PlanProduitFini;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanPfMapper
{
    public static PlanPfEnteteDto MapVersDto(PlanPfEntete entite)
    {
        return new PlanPfEnteteDto
        {
            Id = entite.Id,
            FamilleProduitFiniCode = entite.FamilleProduitFiniCode,
            FamilleProduitFiniLibelle = entite.FamilleProduitFiniCodeNavigation?.Designation ?? string.Empty,
            Version = entite.Version,
            Statut = entite.Statut,
            CreePar = entite.CreePar,
            CreeLe = entite.CreeLe,
            ModifiePar = string.Empty,
            ModifieLe = null,
            Remarques = string.Empty,
            LegendeMoyens = string.Empty,
            Sections = entite.PlanPfSections.OrderBy(s => s.OrdreAffiche).Select(MapSectionVersDto).ToList()
        };
    }

    private static PlanPfSectionDto MapSectionVersDto(PlanPfSection entite)
    {
        return new PlanPfSectionDto
        {
            Id = entite.Id,
            PlanEnteteId = entite.PlanEnteteId,
            TypeSectionId = entite.TypeSectionId,
            TypeSectionLibelle = entite.TypeSection?.Libelle ?? string.Empty,
            LibelleSection = entite.LibelleSection,
            RegleEchantillonnageId = entite.RegleEchantillonnageId,
            RegleEchantillonnageLibelle = entite.RegleEchantillonnage?.Libelle ?? string.Empty,
            Notes = entite.Notes ?? string.Empty,
            OrdreAffiche = entite.OrdreAffiche,
            Lignes = entite.PlanPfLignes.OrderBy(l => l.OrdreAffiche).Select(MapLigneVersDto).ToList()
        };
    }

    private static PlanPfLigneDto MapLigneVersDto(PlanPfLigne entite)
    {
        return new PlanPfLigneDto
        {
            Id = entite.Id,
            SectionId = entite.SectionId,
            OrdreAffiche = entite.OrdreAffiche,
            TypeCaracteristiqueId = entite.TypeCaracteristiqueId,
            TypeCaracteristiqueLibelle = entite.TypeCaracteristique?.Libelle ?? string.Empty,
            LibelleAffiche = entite.LibelleAffiche ?? string.Empty,
            TypeControleId = entite.TypeControleId,
            TypeControleLibelle = entite.TypeControle?.Libelle ?? string.Empty,
            MoyenControleId = entite.MoyenControleId,
            MoyenControleLibelle = entite.MoyenControle?.Libelle ?? string.Empty,
            InstrumentCode = entite.InstrumentCode ?? string.Empty,
            MoyenTexteLibre = entite.MoyenTexteLibre ?? string.Empty,
            LimiteSpecTexte = entite.LimiteSpecTexte ?? string.Empty,
            DefauthequeId = entite.DefauthequeId,
            DefauthequeLibelle = entite.Defautheque != null ? $"{entite.Defautheque.Code} - {entite.Defautheque.Description}" : string.Empty,
            Instruction = entite.Instruction ?? string.Empty,
            Observations = entite.Observations ?? string.Empty
            //EstCritique = entite.EstCritique
        };
    }

    public static void MettreAJourArchitectureComplete(PlanPfEntete entite, List<SectionPfEditDto> sectionsDto, bool forceNewIds = false)
    {
        var sectionsExistantes = forceNewIds ? new Dictionary<Guid, PlanPfSection>() : entite.PlanPfSections.ToDictionary(s => s.Id);
        var lignesExistantes = forceNewIds ? new Dictionary<Guid, PlanPfLigne>() : entite.PlanPfSections.SelectMany(s => s.PlanPfLignes).GroupBy(l => l.Id).ToDictionary(g => g.Key, g => g.First());

        entite.PlanPfSections.Clear();
        entite.PlanPfLignes.Clear();

        foreach (var secDto in sectionsDto.OrderBy(s => s.OrdreAffiche))
        {
            var sectionId = (forceNewIds || secDto.Id == null || secDto.Id == Guid.Empty) 
                ? Guid.NewGuid() 
                : secDto.Id.Value;

            PlanPfSection sectionEntity;

            // ✅ Ajouter le préfixe "Contrôle Produit Fini" si absent
            var libelleComplet = secDto.LibelleSection ?? string.Empty;
            if (!libelleComplet.StartsWith("Contrôle Produit Fini"))
            {
                libelleComplet = $"Contrôle Produit Fini {libelleComplet}".Trim();
            }

            if (!forceNewIds && sectionsExistantes.TryGetValue(sectionId, out var existingSec))
            {
                sectionEntity = existingSec;
                sectionEntity.TypeSectionId = MapperHelper.NullIfEmpty(secDto.TypeSectionId) ?? sectionEntity.TypeSectionId;
                sectionEntity.LibelleSection = libelleComplet;
                sectionEntity.RegleEchantillonnageId = secDto.RegleEchantillonnageId;
                sectionEntity.RegleEchantillonnageLibelle = secDto.RegleEchantillonnageLibelle;
                sectionEntity.OrdreAffiche = secDto.OrdreAffiche;
                sectionEntity.Notes = secDto.Notes;
                sectionEntity.PlanPfLignes.Clear();
            }
            else
            {
                sectionEntity = new PlanPfSection
                {
                    Id = sectionId,
                    PlanEnteteId = entite.Id,
                    TypeSectionId = MapperHelper.NullIfEmpty(secDto.TypeSectionId),
                    LibelleSection = libelleComplet,
                    RegleEchantillonnageId = secDto.RegleEchantillonnageId,
                    RegleEchantillonnageLibelle = secDto.RegleEchantillonnageLibelle,
                    OrdreAffiche = secDto.OrdreAffiche,
                    Notes = secDto.Notes,
                    PlanPfLignes = new List<PlanPfLigne>()
                };
            }

            foreach (var lDto in secDto.Lignes.OrderBy(l => l.OrdreAffiche))
            {
                var ligneId = (forceNewIds || lDto.Id == null || lDto.Id == Guid.Empty) 
                    ? Guid.NewGuid() 
                    : lDto.Id.Value;

                PlanPfLigne ligneEntity;
                var instrumentData = MapperHelper.NormalizeInstrumentCode(lDto.InstrumentCode, lDto.MoyenTexteLibre);

                if (!forceNewIds && lignesExistantes.TryGetValue(ligneId, out var existingLigne))
                {
                    ligneEntity = existingLigne;
                    ligneEntity.SectionId = sectionId;
                    ligneEntity.OrdreAffiche = lDto.OrdreAffiche;
                    ligneEntity.TypeCaracteristiqueId = MapperHelper.NullIfEmpty(lDto.TypeCaracteristiqueId) ?? Guid.Empty;
                    ligneEntity.LibelleAffiche = MapperHelper.NullIfEmpty(lDto.LibelleAffiche);
                    ligneEntity.TypeControleId = MapperHelper.NullIfEmpty(lDto.TypeControleId) ?? Guid.Empty;
                    ligneEntity.MoyenControleId = MapperHelper.NullIfEmpty(lDto.MoyenControleId);
                    ligneEntity.InstrumentCode = MapperHelper.NullIfEmpty(instrumentData.InstrumentCode);
                    ligneEntity.MoyenTexteLibre = MapperHelper.NullIfEmpty(instrumentData.MoyenTexteLibre);
                    ligneEntity.LimiteSpecTexte = MapperHelper.NullIfEmpty(lDto.LimiteSpecTexte);
                    ligneEntity.DefauthequeId = lDto.DefauthequeId;
                    ligneEntity.Instruction = MapperHelper.NullIfEmpty(lDto.Instruction);
                    ligneEntity.Observations = MapperHelper.NullIfEmpty(lDto.Observations);
                    //ligneEntity.EstCritique = lDto.EstCritique;
                }
                else
                {
                    ligneEntity = new PlanPfLigne
                    {
                        Id = ligneId,
                        PlanEnteteId = entite.Id,
                        SectionId = sectionId,
                        OrdreAffiche = lDto.OrdreAffiche,
                        TypeCaracteristiqueId = MapperHelper.NullIfEmpty(lDto.TypeCaracteristiqueId) ?? Guid.Empty,
                        LibelleAffiche = MapperHelper.NullIfEmpty(lDto.LibelleAffiche),
                        TypeControleId = MapperHelper.NullIfEmpty(lDto.TypeControleId) ?? Guid.Empty,
                        MoyenControleId = MapperHelper.NullIfEmpty(lDto.MoyenControleId),
                        InstrumentCode = MapperHelper.NullIfEmpty(instrumentData.InstrumentCode),
                        MoyenTexteLibre = MapperHelper.NullIfEmpty(instrumentData.MoyenTexteLibre),
                        LimiteSpecTexte = MapperHelper.NullIfEmpty(lDto.LimiteSpecTexte),
                        DefauthequeId = lDto.DefauthequeId,
                        Instruction = MapperHelper.NullIfEmpty(lDto.Instruction),
                        Observations = MapperHelper.NullIfEmpty(lDto.Observations)
                        //EstCritique = lDto.EstCritique
                    };
                }

                sectionEntity.PlanPfLignes.Add(ligneEntity);
                entite.PlanPfLignes.Add(ligneEntity);
            }

            entite.PlanPfSections.Add(sectionEntity);
        }
    }

    public static PlanPfEntete CreerNouvelleVersionEntite(PlanPfEntete ancienPlan, NouvelleVersionPfRequestDto request, string auteurSecure, int nouvelleVersion)
    {
        return new PlanPfEntete
        {
            Id = Guid.NewGuid(),
            FamilleProduitFiniCode = request.FamilleProduitFiniCode ?? ancienPlan.FamilleProduitFiniCode,
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            CreePar = auteurSecure,
            CreeLe = DateTime.UtcNow,
            //CommentaireVersion = request.MotifModification,
            //Remarques = request.Remarques,
            //LegendeMoyens = request.LegendeMoyens,
            PlanPfSections = new List<PlanPfSection>(),
            PlanPfLignes = new List<PlanPfLigne>()
        };
    }
}
