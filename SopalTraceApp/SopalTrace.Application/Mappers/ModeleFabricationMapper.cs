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

    public static ModeleFabricationEntete ConstruireEntiteModeleAPartirDeDto(CreateModeleRequestDto dto)
    {
        var modele = new ModeleFabricationEntete
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Libelle = dto.Libelle,
            
            NatureArticleCode = dto.NatureComposantCode,
            
            // On le fait aussi pour l'opération au cas où
            OperationCode = MapperHelper.NullIfEmpty(dto.OperationCode),

            Version = 0,
            Statut = StatutsPlan.Actif,
            Notes = dto.Notes,
            //FamilleProduitFiniCode = MapperHelper.NullIfEmpty(dto.FamilleProduitCode),
            LegendeMoyens = dto.LegendeMoyens,
            CreePar = "Admin",
            CreeLe = DateTime.UtcNow,
            ModeleFabricationSections = new List<ModeleFabricationSection>()
        };

        DupliquerSections(modele, dto.Sections);
        return modele;
    }

    public static ModeleFabricationEntete ConstruireNouvelleVersionModele(ModeleFabricationEntete ancienModele, NouvelleVersionModeleRequestDto request, string auteur, int nouvelleVersion)
    {
        string baseCode = string.IsNullOrWhiteSpace(request.Code) ? ancienModele.Code : request.Code;

        var nouveauModele = new ModeleFabricationEntete
        {
            Id = Guid.NewGuid(),
            Code = IncrementerSuffixeVersion(baseCode, nouvelleVersion),
            Libelle = IncrementerSuffixeVersion(string.IsNullOrWhiteSpace(request.Libelle) ? ancienModele.Libelle : request.Libelle, nouvelleVersion),
            
            NatureArticleCode = string.IsNullOrWhiteSpace(request.NatureComposantCode) ? ancienModele.NatureArticleCode : request.NatureComposantCode,
            
            OperationCode = string.IsNullOrWhiteSpace(request.OperationCode) ? ancienModele.OperationCode : MapperHelper.NullIfEmpty(request.OperationCode),
            
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            Notes = request.Notes ?? ancienModele.Notes,
            //FamilleProduitFiniCode = MapperHelper.NullIfEmpty(request.FamilleProduitCode) ?? ancienModele.FamilleProduitFiniCode,
            LegendeMoyens = string.IsNullOrWhiteSpace(request.LegendeMoyens) ? ancienModele.LegendeMoyens : request.LegendeMoyens,
            CreePar = auteur,
            CreeLe = DateTime.UtcNow,
            ModeleFabricationSections = new List<ModeleFabricationSection>()
        };

        var sectionsSource = request.Sections?.Any() == true ? request.Sections : MapperSectionsExistantesEnEditables(ancienModele.ModeleFabricationSections);
        DupliquerSections(nouveauModele, sectionsSource);

        return nouveauModele;
    }

    public static ModeleFabricationEntete RestaurerEntiteModele(ModeleFabricationEntete modeleArchive, string auteur, string motif, int nouvelleVersion)
    {
        var nouveauModele = new ModeleFabricationEntete
        {
            Id = Guid.NewGuid(),
            Code = IncrementerSuffixeVersion(modeleArchive.Code, nouvelleVersion),
            Libelle = modeleArchive.Libelle,
            NatureArticleCode = modeleArchive.NatureArticleCode,
            OperationCode = modeleArchive.OperationCode,
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            Notes = string.IsNullOrWhiteSpace(motif) ? modeleArchive.Notes : $"{motif}\n{modeleArchive.Notes}",
            //FamilleProduitFiniCode = modeleArchive.FamilleProduitFiniCode,
            LegendeMoyens = modeleArchive.LegendeMoyens,
            CreePar = auteur,
            CreeLe = DateTime.UtcNow,
            ModeleFabricationSections = new List<ModeleFabricationSection>()
        };

        var sectionsSource = MapperSectionsExistantesEnEditables(modeleArchive.ModeleFabricationSections);
        DupliquerSections(nouveauModele, sectionsSource);

        return nouveauModele;
    }

    private static IEnumerable<SectionModeleEditDto> MapperSectionsExistantesEnEditables(IEnumerable<ModeleFabricationSection>? existantes)
    {
        return (existantes ?? Enumerable.Empty<ModeleFabricationSection>()).Select(s => new SectionModeleEditDto
        {
            OrdreAffiche = s.OrdreAffiche,
            LibelleSection = s.LibelleSection,
            TypeSectionId = s.TypeSectionId,
            PeriodiciteId = s.PeriodiciteId,
            FrequenceLibelle = s.Periodicite?.Libelle,
                Lignes = (s.ModeleFabricationLignes ?? Enumerable.Empty<ModeleFabricationLigne>()).Select(l => new LigneModeleEditDto
            {
                OrdreAffiche = l.OrdreAffiche,
                TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                LibelleAffiche = l.LibelleAffiche,
                TypeControleId = l.TypeControleId,
                MoyenControleId = l.MoyenControleId,
                InstrumentCode = l.InstrumentCode,
                MoyenTexteLibre = l.MoyenTexteLibre,
                PeriodiciteId = l.PeriodiciteId,
                Instruction = l.Instruction,
                EstCritique = l.EstCritique,
                LimiteSpecTexte = l.LimiteSpecTexte,
                ColonnesSupplementaires = l.ColonnesSupplementaires
            }).ToList()
        });
    }

    /// <summary>
    /// Duplique les sections du DTO vers l'entité ModeleFabricationSection.
    /// 
    /// LOGIQUE : Les sections des modèles sont toujours stockées par LibelleSection (texte libre).
    /// Le choix de TypeSectionId (référence) se fait au moment de la création du PLAN,
    /// pas du modèle.
    /// 
    /// Dans un modèle, on garde toujours la flexibilité du texte libre.
    /// </summary>
    public static void DupliquerSections(ModeleFabricationEntete modele, IEnumerable<SectionModeleEditDto> sectionsSource)
    {
        foreach (var secDto in sectionsSource)
        {
            var sectionId = Guid.NewGuid();
            var section = new ModeleFabricationSection
            {
                Id = sectionId,
                ModeleEnteteId = modele.Id,
                OrdreAffiche = secDto.OrdreAffiche,

                // Toujours stocker le LibelleSection (texte libre du modèle)
                LibelleSection = secDto.LibelleSection,
                TypeSectionId = secDto.TypeSectionId,
                PeriodiciteId = MapperHelper.NullIfEmpty(secDto.PeriodiciteId),
                //FrequenceLibelle = string.IsNullOrWhiteSpace(secDto.FrequenceLibelle) ? null : secDto.FrequenceLibelle,
                ModeleFabricationLignes = new List<ModeleFabricationLigne>()
            };

            foreach (var lignDto in secDto.Lignes)
            {
                var instrumentData = MapperHelper.NormalizeInstrumentCode(lignDto.InstrumentCode);

                // DupliquerSections: création ModeleFabricationLigne
                section.ModeleFabricationLignes.Add(new ModeleFabricationLigne
                {
                    Id = Guid.NewGuid(),
                    //ModeleEnteteId = modele.Id,
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
                    LimiteSpecTexte = string.IsNullOrWhiteSpace(lignDto.LimiteSpecTexte) ? null : lignDto.LimiteSpecTexte,
                    ColonnesSupplementaires = string.IsNullOrWhiteSpace(lignDto.ColonnesSupplementaires) ? null : lignDto.ColonnesSupplementaires
                });
            }
            modele.ModeleFabricationSections.Add(section);
        }
    }

    public static ModeleResponseDto MapperEntiteModeleVersDto(ModeleFabricationEntete modele)
    {
        return new ModeleResponseDto
        {
            Id = modele.Id, 
            Code = modele.Code, 
            Libelle = modele.Libelle, 
            TypeRobinetCode = string.Empty, // Supprimé de l'entité
            NatureComposantCode = modele.NatureArticleCode, 
            OperationCode = modele.OperationCode ?? string.Empty, 
            FamilleProduitCode = string.Empty, // FamilleProduitFiniCode supprimé
            Version = modele.Version,
            Statut = modele.Statut, Notes = modele.Notes ?? string.Empty,
            LegendeMoyens = modele.LegendeMoyens ?? string.Empty, CreePar = modele.CreePar, CreeLe = modele.CreeLe,
            //ArchiveLe = modele.ArchiveLe, ArchivePar = modele.ArchivePar ?? string.Empty,
            ConfigurationColonnesJson = modele.Formulaire?.ConfigurationStructureJson,
            Sections = modele.ModeleFabricationSections?.Select(s => new ModeleSectionResponseDto
            {
                Id = s.Id, OrdreAffiche = s.OrdreAffiche, LibelleSection = s.LibelleSection, 
                TypeSectionId = s.TypeSectionId, PeriodiciteId = s.PeriodiciteId,
                FrequenceLibelle = s.Periodicite?.Libelle ?? string.Empty,
                Lignes = s.ModeleFabricationLignes?.Select(l => new ModeleLigneResponseDto
                {
                    Id = l.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                    TypeControleId = l.TypeControleId,
                    LibelleAffiche = l.LibelleAffiche,
                    MoyenControleId = l.MoyenControleId,
                    InstrumentCode = l.InstrumentCode,
                    MoyenTexteLibre = l.MoyenTexteLibre,
                    PeriodiciteId = l.PeriodiciteId,
                    Instruction = l.Instruction ?? string.Empty,
                    EstCritique = l.EstCritique,
                    LimiteSpecTexte = l.LimiteSpecTexte ?? string.Empty,
                    ColonnesSupplementaires = l.ColonnesSupplementaires
                }).ToList() ?? new List<ModeleLigneResponseDto>()
            }).ToList() ?? new List<ModeleSectionResponseDto>()
        };
    }
}
