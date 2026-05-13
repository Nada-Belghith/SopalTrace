#nullable enable

using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SopalTrace.Application.Mappers;

public static class ModeleFabricationMapper
{
    public static string IncrementerSuffixeVersion(string original, int nouvelleVersion)
    {
        if (string.IsNullOrWhiteSpace(original)) return original;
        var regex = new Regex(@"-[Vv]\d+$");

        if (nouvelleVersion == 0)
        {
            return regex.IsMatch(original) ? regex.Replace(original, "") : original;
        }

        if (regex.IsMatch(original)) return regex.Replace(original, $"-V{nouvelleVersion}");
        return $"{original}-V{nouvelleVersion}";
    }

    public static ModeleFabEntete ConstruireEntiteModeleAPartirDeDto(CreateModeleRequestDto dto)
    {
        var modele = new ModeleFabEntete
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Libelle = dto.Libelle,
            
            NatureComposantCode = dto.NatureComposantCode,
            
            // On le fait aussi pour l'opération au cas où
            OperationCode = MapperHelper.NullIfEmpty(dto.OperationCode),

            Version = 0,
            Statut = StatutsPlan.Actif,
            Notes = dto.Notes,
            FamilleProduitFiniCode = MapperHelper.NullIfEmpty(dto.FamilleProduitCode),
            LegendeMoyens = dto.LegendeMoyens,
            CreePar = "Admin",
            CreeLe = DateTime.UtcNow,
            ModeleFabSections = new List<ModeleFabSection>()
        };

        DupliquerSections(modele, dto.Sections);
        return modele;
    }

    public static ModeleFabEntete ConstruireNouvelleVersionModele(ModeleFabEntete ancienModele, NouvelleVersionModeleRequestDto request, string auteur, int nouvelleVersion)
    {
        string baseCode = string.IsNullOrWhiteSpace(request.Code) ? ancienModele.Code : request.Code;

        var nouveauModele = new ModeleFabEntete
        {
            Id = Guid.NewGuid(),
            Code = IncrementerSuffixeVersion(baseCode, nouvelleVersion),
            Libelle = IncrementerSuffixeVersion(string.IsNullOrWhiteSpace(request.Libelle) ? ancienModele.Libelle : request.Libelle, nouvelleVersion),
            
            NatureComposantCode = string.IsNullOrWhiteSpace(request.NatureComposantCode) ? ancienModele.NatureComposantCode : request.NatureComposantCode,
            
            OperationCode = string.IsNullOrWhiteSpace(request.OperationCode) ? ancienModele.OperationCode : MapperHelper.NullIfEmpty(request.OperationCode),
            
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            Notes = request.Notes ?? ancienModele.Notes,
            FamilleProduitFiniCode = MapperHelper.NullIfEmpty(request.FamilleProduitCode) ?? ancienModele.FamilleProduitFiniCode,
            LegendeMoyens = string.IsNullOrWhiteSpace(request.LegendeMoyens) ? ancienModele.LegendeMoyens : request.LegendeMoyens,
            CreePar = auteur,
            CreeLe = DateTime.UtcNow,
            ModeleFabSections = new List<ModeleFabSection>()
        };

        var sectionsSource = request.Sections?.Any() == true ? request.Sections : MapperSectionsExistantesEnEditables(ancienModele.ModeleFabSections);
        DupliquerSections(nouveauModele, sectionsSource);

        return nouveauModele;
    }

    public static ModeleFabEntete RestaurerEntiteModele(ModeleFabEntete modeleArchive, string auteur, string motif, int nouvelleVersion)
    {
        var nouveauModele = new ModeleFabEntete
        {
            Id = Guid.NewGuid(),
            Code = IncrementerSuffixeVersion(modeleArchive.Code, nouvelleVersion),
            Libelle = modeleArchive.Libelle,
            NatureComposantCode = modeleArchive.NatureComposantCode,
            OperationCode = modeleArchive.OperationCode,
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            Notes = string.IsNullOrWhiteSpace(motif) ? modeleArchive.Notes : $"{motif}\n{modeleArchive.Notes}",
            FamilleProduitFiniCode = modeleArchive.FamilleProduitFiniCode,
            LegendeMoyens = modeleArchive.LegendeMoyens,
            CreePar = auteur,
            CreeLe = DateTime.UtcNow,
            ModeleFabSections = new List<ModeleFabSection>()
        };

        var sectionsSource = MapperSectionsExistantesEnEditables(modeleArchive.ModeleFabSections);
        DupliquerSections(nouveauModele, sectionsSource);

        return nouveauModele;
    }

    private static IEnumerable<SectionModeleEditDto> MapperSectionsExistantesEnEditables(IEnumerable<ModeleFabSection>? existantes)
    {
        return (existantes ?? Enumerable.Empty<ModeleFabSection>()).Select(s => new SectionModeleEditDto
        {
            OrdreAffiche = s.OrdreAffiche,
            LibelleSection = s.LibelleSection,
            TypeSectionId = s.TypeSectionId,
            PeriodiciteId = s.PeriodiciteId,
            FrequenceLibelle = s.FrequenceLibelle,
                Lignes = (s.ModeleFabLignes ?? Enumerable.Empty<ModeleFabLigne>()).Select(l => new LigneModeleEditDto
            {
                OrdreAffiche = l.OrdreAffiche,
                TypeCaracteristiqueId = l.TypeCaracteristiqueId ?? Guid.Empty,
                LibelleAffiche = l.LibelleAffiche,
                TypeControleId = l.TypeControleId ?? Guid.Empty,
                MoyenControleId = l.MoyenControleId,
                InstrumentCode = l.InstrumentCode,
                MoyenTexteLibre = l.MoyenTexteLibre,
                PeriodiciteId = l.PeriodiciteId,
                Instruction = l.Instruction,
                EstCritique = l.EstCritique,
                LimiteSpecTexte = l.LimiteSpecTexte
            }).ToList()
        });
    }

    /// <summary>
    /// Duplique les sections du DTO vers l'entité ModeleFabSection.
    /// 
    /// LOGIQUE : Les sections des modèles sont toujours stockées par LibelleSection (texte libre).
    /// Le choix de TypeSectionId (référence) se fait au moment de la création du PLAN,
    /// pas du modèle.
    /// 
    /// Dans un modèle, on garde toujours la flexibilité du texte libre.
    /// </summary>
    public static void DupliquerSections(ModeleFabEntete modele, IEnumerable<SectionModeleEditDto> sectionsSource)
    {
        foreach (var secDto in sectionsSource)
        {
            var sectionId = Guid.NewGuid();
            var section = new ModeleFabSection
            {
                Id = sectionId,
                ModeleEnteteId = modele.Id,
                OrdreAffiche = secDto.OrdreAffiche,

                // Toujours stocker le LibelleSection (texte libre du modèle)
                LibelleSection = secDto.LibelleSection,
                TypeSectionId = secDto.TypeSectionId,
                PeriodiciteId = MapperHelper.NullIfEmpty(secDto.PeriodiciteId),
                FrequenceLibelle = string.IsNullOrWhiteSpace(secDto.FrequenceLibelle) ? null : secDto.FrequenceLibelle,
                ModeleFabLignes = new List<ModeleFabLigne>()
            };

            foreach (var lignDto in secDto.Lignes)
            {
                var instrumentData = MapperHelper.NormalizeInstrumentCode(lignDto.InstrumentCode);

                // DupliquerSections: création ModeleFabLigne
                section.ModeleFabLignes.Add(new ModeleFabLigne
                {
                    Id = Guid.NewGuid(),
                    ModeleEnteteId = modele.Id,
                    SectionId = sectionId,
                    OrdreAffiche = lignDto.OrdreAffiche,
                    TypeCaracteristiqueId = lignDto.TypeCaracteristiqueId,
                    LibelleAffiche = lignDto.LibelleAffiche,
                    TypeControleId = lignDto.TypeControleId,
                    MoyenControleId = MapperHelper.NullIfEmpty(lignDto.MoyenControleId),
                    InstrumentCode = instrumentData.InstrumentCode,
                    MoyenTexteLibre = instrumentData.MoyenTexteLibre,
                    PeriodiciteId = MapperHelper.NullIfEmpty(lignDto.PeriodiciteId),
                    Instruction = lignDto.Instruction,
                    EstCritique = lignDto.EstCritique,
                    LimiteSpecTexte = string.IsNullOrWhiteSpace(lignDto.LimiteSpecTexte) ? null : lignDto.LimiteSpecTexte
                });
            }
            modele.ModeleFabSections.Add(section);
        }
    }

    public static ModeleResponseDto MapperEntiteModeleVersDto(ModeleFabEntete modele)
    {
        return new ModeleResponseDto
        {
            Id = modele.Id, 
            Code = modele.Code, 
            Libelle = modele.Libelle, 
            TypeRobinetCode = string.Empty, // Supprimé de l'entité
            NatureComposantCode = modele.NatureComposantCode, 
            OperationCode = modele.OperationCode ?? string.Empty, 
            FamilleProduitCode = modele.FamilleProduitFiniCode,
            Version = modele.Version,
            Statut = modele.Statut, Notes = modele.Notes ?? string.Empty,
            LegendeMoyens = modele.LegendeMoyens ?? string.Empty, CreePar = modele.CreePar, CreeLe = modele.CreeLe,
            ArchiveLe = modele.ArchiveLe, ArchivePar = modele.ArchivePar ?? string.Empty,
            Sections = modele.ModeleFabSections?.Select(s => new ModeleSectionResponseDto
            {
                Id = s.Id, OrdreAffiche = s.OrdreAffiche, LibelleSection = s.LibelleSection, 
                TypeSectionId = s.TypeSectionId, PeriodiciteId = s.PeriodiciteId,
                FrequenceLibelle = s.FrequenceLibelle ?? string.Empty,
                Lignes = s.ModeleFabLignes?.Select(l => new ModeleLigneResponseDto
                {
                    Id = l.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId ?? Guid.Empty,
                    TypeControleId = l.TypeControleId ?? Guid.Empty,
                    LibelleAffiche = l.LibelleAffiche,
                    MoyenControleId = l.MoyenControleId,
                    InstrumentCode = l.InstrumentCode,
                    MoyenTexteLibre = l.MoyenTexteLibre,
                    PeriodiciteId = l.PeriodiciteId,
                    Instruction = l.Instruction ?? string.Empty,
                    EstCritique = l.EstCritique,
                    LimiteSpecTexte = l.LimiteSpecTexte ?? string.Empty
                }).ToList() ?? new List<ModeleLigneResponseDto>()
            }).ToList() ?? new List<ModeleSectionResponseDto>()
        };
    }
}
