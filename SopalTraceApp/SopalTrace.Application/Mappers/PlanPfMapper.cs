#nullable enable

using SopalTrace.Application.DTOs.QualityPlans.PlanProduitFini;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SopalTrace.Application.Mappers;

public static class PlanPfMapper
{
    private static Guid? NullIfEmpty(Guid? value) => (value == null || value == Guid.Empty) ? null : value;
    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static (string? InstrumentCode, string? MoyenTexteLibre) NormalizeInstrumentCode(string? instrumentCode, string? moyenTexteLibre = null)
    {
        var normalizedInstrumentCode = string.IsNullOrWhiteSpace(instrumentCode) ? null : instrumentCode.Trim();
        var normalizedMoyenTexteLibre = string.IsNullOrWhiteSpace(moyenTexteLibre) ? null : moyenTexteLibre.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedMoyenTexteLibre))
        {
            return (normalizedInstrumentCode, normalizedMoyenTexteLibre);
        }

        // Si le code ressemble à du texte libre (caractères spéciaux), on le bascule en texte libre
        if (normalizedInstrumentCode != null && normalizedInstrumentCode.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-' && ch != '_' && ch != '/'))
        {
            return (null, normalizedInstrumentCode);
        }

        return (normalizedInstrumentCode, null);
    }

    public static PlanPfEnteteDto MapVersDto(PlanPfEntete entite)
    {
        return new PlanPfEnteteDto
        {
            Id = entite.Id,
            TypeRobinetCode = entite.TypeRobinetCode,
            Designation = entite.Designation ?? string.Empty,
            Version = entite.Version,
            Statut = entite.Statut,
            DateApplication = entite.DateApplication,
            CreePar = entite.CreePar,
            CreeLe = entite.CreeLe,
            ModifiePar = entite.ModifiePar ?? string.Empty,
            ModifieLe = entite.ModifieLe,
            CommentaireVersion = entite.CommentaireVersion ?? string.Empty,
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
            ToleranceSuperieure = entite.ToleranceSuperieure,
            ToleranceInferieure = entite.ToleranceInferieure,
            LimiteSpecTexte = entite.LimiteSpecTexte ?? string.Empty,
            DefauthequeId = entite.DefauthequeId,
            DefauthequeLibelle = entite.Defautheque != null ? $"{entite.Defautheque.Code} - {entite.Defautheque.Description}" : string.Empty,
            Instruction = entite.Instruction ?? string.Empty,
            Observations = entite.Observations ?? string.Empty
        };
    }

    public static void MettreAJourArchitectureComplete(PlanPfEntete entite, List<SectionPfEditDto> sectionsDto, bool forceNewIds = false)
    {
        // 1. On indexe les entités existantes pour les réutiliser (évite les conflits de Tracking EF Core)
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
            if (!forceNewIds && sectionsExistantes.TryGetValue(sectionId, out var existingSec))
            {
                sectionEntity = existingSec;
                sectionEntity.TypeSectionId = secDto.TypeSectionId;
                sectionEntity.LibelleSection = secDto.LibelleSection;
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
                    TypeSectionId = secDto.TypeSectionId,
                    LibelleSection = secDto.LibelleSection,
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
                var instrumentData = NormalizeInstrumentCode(lDto.InstrumentCode, lDto.MoyenTexteLibre);

                if (!forceNewIds && lignesExistantes.TryGetValue(ligneId, out var existingLigne))
                {
                    ligneEntity = existingLigne;
                    ligneEntity.SectionId = sectionId;
                    ligneEntity.OrdreAffiche = lDto.OrdreAffiche;
                    ligneEntity.TypeCaracteristiqueId = NullIfEmpty(lDto.TypeCaracteristiqueId);
                    ligneEntity.LibelleAffiche = NullIfEmpty(lDto.LibelleAffiche);
                    ligneEntity.TypeControleId = NullIfEmpty(lDto.TypeControleId);
                    ligneEntity.MoyenControleId = NullIfEmpty(lDto.MoyenControleId);
                    ligneEntity.InstrumentCode = NullIfEmpty(instrumentData.InstrumentCode);
                    ligneEntity.MoyenTexteLibre = NullIfEmpty(instrumentData.MoyenTexteLibre);
                    ligneEntity.ToleranceSuperieure = lDto.ToleranceSuperieure;
                    ligneEntity.ToleranceInferieure = lDto.ToleranceInferieure;
                    ligneEntity.LimiteSpecTexte = NullIfEmpty(lDto.LimiteSpecTexte);
                    ligneEntity.DefauthequeId = lDto.DefauthequeId;
                    ligneEntity.Instruction = NullIfEmpty(lDto.Instruction);
                    ligneEntity.Observations = NullIfEmpty(lDto.Observations);
                }
                else
                {
                    ligneEntity = new PlanPfLigne
                    {
                        Id = ligneId,
                        PlanEnteteId = entite.Id,
                        SectionId = sectionId,
                        OrdreAffiche = lDto.OrdreAffiche,
                        TypeCaracteristiqueId = NullIfEmpty(lDto.TypeCaracteristiqueId),
                        LibelleAffiche = NullIfEmpty(lDto.LibelleAffiche),
                        TypeControleId = NullIfEmpty(lDto.TypeControleId),
                        MoyenControleId = NullIfEmpty(lDto.MoyenControleId),
                        InstrumentCode = NullIfEmpty(instrumentData.InstrumentCode),
                        MoyenTexteLibre = NullIfEmpty(instrumentData.MoyenTexteLibre),
                        ToleranceSuperieure = lDto.ToleranceSuperieure,
                        ToleranceInferieure = lDto.ToleranceInferieure,
                        LimiteSpecTexte = NullIfEmpty(lDto.LimiteSpecTexte),
                        DefauthequeId = lDto.DefauthequeId,
                        Instruction = NullIfEmpty(lDto.Instruction),
                        Observations = NullIfEmpty(lDto.Observations)
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
            TypeRobinetCode = request.TypeRobinetCode ?? ancienPlan.TypeRobinetCode,
            Designation = request.Designation ?? ancienPlan.Designation,
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            DateApplication = DateOnly.FromDateTime(DateTime.UtcNow),
            CreePar = auteurSecure,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = request.MotifModification,
            PlanPfSections = new List<PlanPfSection>(),
            PlanPfLignes = new List<PlanPfLigne>()
        };
    }
}
