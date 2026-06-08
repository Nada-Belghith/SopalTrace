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
    public static PlanPfEnteteDto MapVersDto(PlanProduitFiniEntete entite)
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
            ConfigurationColonnesJson = entite.Formulaire?.ConfigurationStructureJson,
            Sections = entite.PlanProduitFiniSections.OrderBy(s => s.OrdreAffiche).Select(MapSectionVersDto).ToList()
        };
    }

    private static PlanPfSectionDto MapSectionVersDto(PlanProduitFiniSection entite)
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
            Lignes = entite.PlanProduitFiniLignes.OrderBy(l => l.OrdreAffiche).Select(MapLigneVersDto).ToList()
        };
    }

    private static PlanPfLigneDto MapLigneVersDto(PlanProduitFiniLigne entite)
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
            Observations = entite.Observations ?? string.Empty,
            ColonnesSupplementaires = entite.ColonnesSupplementaires
            //EstCritique = entite.EstCritique
        };
    }

    public static void MettreAJourArchitectureComplete(PlanProduitFiniEntete entite, List<SectionPfEditDto> sectionsDto, bool forceNewIds = false)
    {
        var sectionsExistantes = forceNewIds ? new Dictionary<Guid, PlanProduitFiniSection>() : entite.PlanProduitFiniSections.ToDictionary(s => s.Id);
        var lignesExistantes = forceNewIds ? new Dictionary<Guid, PlanProduitFiniLigne>() : entite.PlanProduitFiniSections.SelectMany(s => s.PlanProduitFiniLignes).GroupBy(l => l.Id).ToDictionary(g => g.Key, g => g.First());

        entite.PlanProduitFiniSections.Clear();
        entite.PlanProduitFiniLignes.Clear();

        foreach (var secDto in sectionsDto.OrderBy(s => s.OrdreAffiche))
        {
            var sectionId = (forceNewIds || secDto.Id == null || secDto.Id == Guid.Empty) 
                ? Guid.NewGuid() 
                : secDto.Id.Value;

            PlanProduitFiniSection sectionEntity;

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
                sectionEntity.PlanProduitFiniLignes.Clear();
            }
            else
            {
                sectionEntity = new PlanProduitFiniSection
                {
                    Id = sectionId,
                    PlanEnteteId = entite.Id,
                    TypeSectionId = MapperHelper.NullIfEmpty(secDto.TypeSectionId),
                    LibelleSection = libelleComplet,
                    RegleEchantillonnageId = secDto.RegleEchantillonnageId,
                    RegleEchantillonnageLibelle = secDto.RegleEchantillonnageLibelle,
                    OrdreAffiche = secDto.OrdreAffiche,
                    Notes = secDto.Notes,
                    PlanProduitFiniLignes = new List<PlanProduitFiniLigne>()
                };
            }

            foreach (var lDto in secDto.Lignes.OrderBy(l => l.OrdreAffiche))
            {
                var ligneId = (forceNewIds || lDto.Id == null || lDto.Id == Guid.Empty) 
                    ? Guid.NewGuid() 
                    : lDto.Id.Value;

                PlanProduitFiniLigne ligneEntity;
                var instrumentData = MapperHelper.NormalizeInstrumentCode(lDto.InstrumentCode, lDto.MoyenTexteLibre);

                if (!forceNewIds && lignesExistantes.TryGetValue(ligneId, out var existingLigne))
                {
                    ligneEntity = existingLigne;
                    ligneEntity.SectionId = sectionId;
                    ligneEntity.OrdreAffiche = lDto.OrdreAffiche;
                    ligneEntity.TypeCaracteristiqueId = MapperHelper.NullIfEmpty(lDto.TypeCaracteristiqueId);
                    ligneEntity.LibelleAffiche = MapperHelper.NullIfEmpty(lDto.LibelleAffiche);
                    ligneEntity.TypeControleId = MapperHelper.NullIfEmpty(lDto.TypeControleId);
                    ligneEntity.MoyenControleId = MapperHelper.NullIfEmpty(lDto.MoyenControleId);
                    ligneEntity.InstrumentCode = MapperHelper.NullIfEmpty(instrumentData.InstrumentCode);
                    ligneEntity.MoyenTexteLibre = MapperHelper.NullIfEmpty(instrumentData.MoyenTexteLibre);
                    ligneEntity.LimiteSpecTexte = MapperHelper.NullIfEmpty(lDto.LimiteSpecTexte);
                    ligneEntity.DefauthequeId = lDto.DefauthequeId;
                    ligneEntity.Instruction = MapperHelper.NullIfEmpty(lDto.Instruction);
                    ligneEntity.Observations = MapperHelper.NullIfEmpty(lDto.Observations);
                    ligneEntity.ColonnesSupplementaires = lDto.ColonnesSupplementaires;
                    //ligneEntity.EstCritique = lDto.EstCritique;
                }
                else
                {
                    ligneEntity = new PlanProduitFiniLigne
                    {
                        Id = ligneId,
                        PlanEnteteId = entite.Id,
                        SectionId = sectionId,
                        OrdreAffiche = lDto.OrdreAffiche,
                        TypeCaracteristiqueId = MapperHelper.NullIfEmpty(lDto.TypeCaracteristiqueId),
                        LibelleAffiche = MapperHelper.NullIfEmpty(lDto.LibelleAffiche),
                        TypeControleId = MapperHelper.NullIfEmpty(lDto.TypeControleId),
                        MoyenControleId = MapperHelper.NullIfEmpty(lDto.MoyenControleId),
                        InstrumentCode = MapperHelper.NullIfEmpty(instrumentData.InstrumentCode),
                        MoyenTexteLibre = MapperHelper.NullIfEmpty(instrumentData.MoyenTexteLibre),
                        LimiteSpecTexte = MapperHelper.NullIfEmpty(lDto.LimiteSpecTexte),
                        DefauthequeId = lDto.DefauthequeId,
                        Instruction = MapperHelper.NullIfEmpty(lDto.Instruction),
                        Observations = MapperHelper.NullIfEmpty(lDto.Observations),
                        ColonnesSupplementaires = lDto.ColonnesSupplementaires
                        //EstCritique = lDto.EstCritique
                    };
                }

                sectionEntity.PlanProduitFiniLignes.Add(ligneEntity);
                entite.PlanProduitFiniLignes.Add(ligneEntity);
            }

            entite.PlanProduitFiniSections.Add(sectionEntity);
        }
    }

    public static PlanProduitFiniEntete CreerNouvelleVersionEntite(PlanProduitFiniEntete ancienPlan, NouvelleVersionPfRequestDto request, string auteurSecure, int nouvelleVersion)
    {
        return new PlanProduitFiniEntete
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
            PlanProduitFiniSections = new List<PlanProduitFiniSection>(),
            PlanProduitFiniLignes = new List<PlanProduitFiniLigne>()
        };
    }
}

