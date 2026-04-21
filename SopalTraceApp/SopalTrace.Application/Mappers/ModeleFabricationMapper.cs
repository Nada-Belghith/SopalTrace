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
    private static Guid? NullIfEmpty(Guid? value) => value == Guid.Empty ? null : value;

    // Fonction utilitaire pour transformer "" en null afin d'éviter les erreurs de clé étrangère
    private static string? NullIfEmptyString(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool IsCustomInstrumentCode(string? instrumentCode)
        => !string.IsNullOrWhiteSpace(instrumentCode)
           && instrumentCode.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-' && ch != '_' && ch != '/');

    private static (string? InstrumentCode, string? MoyenTexteLibre) NormalizeInstrumentCode(string? instrumentCode, string? moyenTexteLibre = null)
    {
        var normalizedInstrumentCode = NullIfEmptyString(instrumentCode);
        var normalizedMoyenTexteLibre = NullIfEmptyString(moyenTexteLibre);

        if (!string.IsNullOrWhiteSpace(normalizedMoyenTexteLibre))
        {
            return (normalizedInstrumentCode, normalizedMoyenTexteLibre);
        }

        if (IsCustomInstrumentCode(normalizedInstrumentCode))
        {
            return (null, normalizedInstrumentCode);
        }

        return (normalizedInstrumentCode, null);
    }

    public static string IncrementerSuffixeVersion(string original, int nouvelleVersion)
    {
        if (string.IsNullOrWhiteSpace(original)) return original;
        var regex = new Regex(@"-[Vv]\d+$");
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
            
            // CORRECTION ICI : Si le front envoie "", on insère NULL en base de données.
            TypeRobinetCode = NullIfEmptyString(dto.TypeRobinetCode),
            
            NatureComposantCode = dto.NatureComposantCode,
            
            // On le fait aussi pour l'opération au cas où
            OperationCode = NullIfEmptyString(dto.OperationCode),
            
            Version = 1,
            Statut = StatutsPlan.Actif,
            Notes = dto.Notes,
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
            Libelle = string.IsNullOrWhiteSpace(request.Libelle) ? ancienModele.Libelle : request.Libelle,
            
            // CORRECTION ICI AUSSI : Transformation des "" en NULL
            TypeRobinetCode = string.IsNullOrWhiteSpace(request.TypeRobinetCode) ? ancienModele.TypeRobinetCode : NullIfEmptyString(request.TypeRobinetCode),
            
            NatureComposantCode = string.IsNullOrWhiteSpace(request.NatureComposantCode) ? ancienModele.NatureComposantCode : request.NatureComposantCode,
            
            OperationCode = string.IsNullOrWhiteSpace(request.OperationCode) ? ancienModele.OperationCode : NullIfEmptyString(request.OperationCode),
            
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            Notes = request.Notes ?? ancienModele.Notes,
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
            TypeRobinetCode = modeleArchive.TypeRobinetCode,
            NatureComposantCode = modeleArchive.NatureComposantCode,
            OperationCode = modeleArchive.OperationCode,
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            Notes = $"[Restauré depuis V{modeleArchive.Version}] {motif}\n{modeleArchive.Notes}",
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
            FrequenceLibelle = s.FrequenceLibelle,
                Lignes = (s.ModeleFabLignes ?? Enumerable.Empty<ModeleFabLigne>()).Select(l => new LigneModeleEditDto
            {
                OrdreAffiche = l.OrdreAffiche,
                TypeCaracteristiqueId = l.TypeCaracteristiqueId ?? Guid.Empty,
                LibelleAffiche = l.LibelleAffiche,
                TypeControleId = l.TypeControleId ?? Guid.Empty,
                MoyenControleId = l.MoyenControleId,
                InstrumentCode = l.InstrumentCode ?? l.MoyenTexteLibre,
                PeriodiciteId = l.PeriodiciteId,
                Instruction = l.Instruction,
                EstCritique = l.EstCritique,
                LimiteSpecTexte = l.LimiteSpecTexte,
                ToleranceInferieure = l.ToleranceInferieure,
                ToleranceSuperieure = l.ToleranceSuperieure
            }).ToList()
        });
    }

    private static void DupliquerSections(ModeleFabEntete modele, IEnumerable<SectionModeleEditDto> sectionsSource)
    {
        foreach (var secDto in sectionsSource)
        {
            var sectionId = Guid.NewGuid();
            var section = new ModeleFabSection
            {
                Id = sectionId, ModeleEnteteId = modele.Id, OrdreAffiche = secDto.OrdreAffiche,
                LibelleSection = secDto.LibelleSection, FrequenceLibelle = string.IsNullOrWhiteSpace(secDto.FrequenceLibelle) ? null : secDto.FrequenceLibelle, ModeleFabLignes = new List<ModeleFabLigne>()
            };

            foreach (var lignDto in secDto.Lignes)
            {
                var instrumentData = NormalizeInstrumentCode(lignDto.InstrumentCode);

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
                    MoyenControleId = NullIfEmpty(lignDto.MoyenControleId),
                    InstrumentCode = instrumentData.InstrumentCode,
                    MoyenTexteLibre = instrumentData.MoyenTexteLibre,
                    PeriodiciteId = NullIfEmpty(lignDto.PeriodiciteId),
                    Instruction = lignDto.Instruction,
                    EstCritique = lignDto.EstCritique,
                    ToleranceInferieure = lignDto.ToleranceInferieure,
                    ToleranceSuperieure = lignDto.ToleranceSuperieure,
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
            Id = modele.Id, Code = modele.Code, Libelle = modele.Libelle, TypeRobinetCode = modele.TypeRobinetCode ?? string.Empty,
            NatureComposantCode = modele.NatureComposantCode, OperationCode = modele.OperationCode ?? string.Empty, Version = modele.Version,
            Statut = modele.Statut, Notes = modele.Notes ?? string.Empty,
            LegendeMoyens = modele.LegendeMoyens ?? string.Empty, CreePar = modele.CreePar, CreeLe = modele.CreeLe,
            ArchiveLe = modele.ArchiveLe, ArchivePar = modele.ArchivePar ?? string.Empty,
            Sections = modele.ModeleFabSections?.Select(s => new ModeleSectionResponseDto
            {
                Id = s.Id, OrdreAffiche = s.OrdreAffiche, LibelleSection = s.LibelleSection, FrequenceLibelle = s.FrequenceLibelle ?? string.Empty,
                Lignes = s.ModeleFabLignes?.Select(l => new ModeleLigneResponseDto
                {
                    Id = l.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId ?? Guid.Empty,
                    TypeControleId = l.TypeControleId ?? Guid.Empty,
                    MoyenControleId = l.MoyenControleId,
                    InstrumentCode = l.InstrumentCode ?? l.MoyenTexteLibre,
                    PeriodiciteId = l.PeriodiciteId,
                    Instruction = l.Instruction ?? string.Empty,
                    EstCritique = l.EstCritique,
                    LimiteSpecTexte = l.LimiteSpecTexte ?? string.Empty,
                    ToleranceInferieure = l.ToleranceInferieure,
                    ToleranceSuperieure = l.ToleranceSuperieure
                }).ToList() ?? new List<ModeleLigneResponseDto>()
            }).ToList() ?? new List<ModeleSectionResponseDto>()
        };
    }
}
