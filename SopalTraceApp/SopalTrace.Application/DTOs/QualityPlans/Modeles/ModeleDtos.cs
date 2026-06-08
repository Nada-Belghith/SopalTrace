#nullable enable
using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Modeles;

// --- CRÉATION ---
public record CreateModeleRequestDto
{
    public required string Code { get; init; }
    public required string Libelle { get; init; }
    public required string? TypeRobinetCode { get; init; }
    public required string NatureComposantCode { get; init; }
    public required string OperationCode { get; init; }
    public string? PosteCode { get; init; } = null;
    public string? FamilleProduitCode { get; init; } = null;
    public string? Notes { get; init; } = null;
    public string? LegendeMoyens { get; init; } = null;
    public string? CreePar { get; init; } = "SYSTEM";
    public int? VersionInitiale { get; init; }
    public string? ConfigurationColonnesJson { get; init; } = null;
    /// <summary>Code du formulaire référence sélectionné par l'utilisateur (ex: FE-ASS-PISTON). Permet de versionner le bon formulaire.</summary>
    public string? RefFormulaireCodeReference { get; init; } = null;
    public List<SectionModeleEditDto> Sections { get; init; } = new();
}

// --- ÉDITION ---
/// <summary>
/// DTO pour éditer les sections d'un modèle.
/// 
/// LOGIQUE DE PERSISTANCE (Import complexe sans fréquence) :
/// 
/// 1. Si TypeSectionId est SET (admin a choisi une section existante) :
///    → Utiliser section.TypeSectionId (référence)
///    → Ignorer LibelleSection
///    → La section est liée à une référence admin-managed
/// 
/// 2. Si TypeSectionId est NULL ET LibelleSection est rempli (phrase complexe) :
///    → Stocker LibelleSection texte brut en base
///    → Pas de création automatique de TypeSections
///    → Admin peut créer/lier later si nécessaire
/// 
/// 3. Si les deux sont NULL :
///    → Erreur ou traitement par défaut
/// 
/// COMPORTEMENT À L'IMPORT (aperçu seulement) :
/// - Les sections complexes SANS fréquence restent en LibelleSection
/// - TypeSectionId reste NULL (pas de création auto)
/// - RegleEchantillonnageId reste NULL (cherche seulement si existe)
/// - L'admin décide lors de la création du modèle/plan
/// </summary>
public record SectionModeleEditDto
{
    public Guid? Id { get; set; }
    public required int OrdreAffiche { get; init; }
    public required string LibelleSection { get; init; }
    public Guid? TypeSectionId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public string? FrequenceLibelle { get; init; } = null;
    public Guid? RegleEchantillonnageId { get; init; }
    public string? RegleEchantillonnageLibelle { get; init; }
    public string? Notes { get; init; } = null;
    public List<LigneModeleEditDto> Lignes { get; init; } = new();
}

public record LigneModeleEditDto
{
    public Guid? Id { get; set; }
    public required int OrdreAffiche { get; init; }
    public Guid? TypeCaracteristiqueId { get; init; }
    public string? LibelleAffiche { get; init; }
    public Guid? TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public string? InstrumentCode { get; init; } = null;
    public Guid? PeriodiciteId { get; init; }
    public string? Instruction { get; init; } = null;
    public string? Observations { get; init; } = null;
    public required bool EstCritique { get; init; }
    public string? LimiteSpecTexte { get; init; } = null;
    public string? MoyenTexteLibre { get; init; } = null;
    public string? ColonnesSupplementaires { get; init; } = null;
}

public record ChangeModeleStatusRequestDto
{
    public required string NouveauStatut { get; init; }
    public string? Motif { get; init; }
}

public record NouvelleVersionModeleRequestDto
{
    public required Guid AncienId { get; init; }
    public string? CreePar { get; init; }
    public string? ModifiePar { get; init; }
    public string? MotifModification { get; init; }
    public string? Code { get; init; }
    public string? Libelle { get; init; }
    public string? TypeRobinetCode { get; init; }
    public string? NatureComposantCode { get; init; } = null;
    public string? OperationCode { get; init; } = null;
    public string? PosteCode { get; init; } = null;
    public string? FamilleProduitCode { get; init; } = null;
    public string? Notes { get; init; } = null;
    public string? LegendeMoyens { get; init; } = null;
    public int? VersionInitiale { get; init; }
    public List<SectionModeleEditDto> Sections { get; init; } = new();
    public string? ConfigurationColonnesJson { get; init; } = null;
    /// <summary>Code du formulaire référence sélectionné (ex: FE-ASS-PISTON). Permet de versionner le bon formulaire spécifiquement.</summary>
    public string? RefFormulaireCodeReference { get; init; } = null;
}

// --- RÉPONSES ---
public record ModeleResponseDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Libelle { get; init; }
    public required string TypeRobinetCode { get; init; }
    public required string NatureComposantCode { get; init; }
    public required string OperationCode { get; init; }
    public string? PosteCode { get; init; }
    public string? FamilleProduitCode { get; init; }
    public required int Version { get; init; }
    public required string Statut { get; init; }
    public string? Notes { get; init; }
    public string? LegendeMoyens { get; init; } 
    public required string CreePar { get; init; }
    public required DateTime CreeLe { get; init; }
    public string? ModifiePar { get; init; }
    public DateTime? ModifieLe { get; init; }
    public string? ArchivePar { get; init; }
    public DateTime? ArchiveLe { get; init; }
    public string? ConfigurationColonnesJson { get; init; }
    public List<ModeleSectionResponseDto> Sections { get; init; } = new();
}

public record ModeleSectionResponseDto
{
    public required Guid Id { get; init; }
    public required int OrdreAffiche { get; init; }
    public required string LibelleSection { get; init; }
    public Guid? TypeSectionId { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public string? FrequenceLibelle { get; init; }
    public Guid? RegleEchantillonnageId { get; init; }
    public string? RegleEchantillonnageLibelle { get; init; }
    public List<ModeleLigneResponseDto> Lignes { get; init; } = new();
}

public record ModeleLigneResponseDto
{
    public required Guid Id { get; init; }
    public required int OrdreAffiche { get; init; }
    public Guid? TypeCaracteristiqueId { get; init; }
    public string? LibelleAffiche { get; init; }
    public Guid? TypeControleId { get; init; }
    public Guid? MoyenControleId { get; init; }
    public string? InstrumentCode { get; init; }
    public Guid? PeriodiciteId { get; init; }
    public string? Instruction { get; init; }
    public string? Observations { get; init; }
    public required bool EstCritique { get; init; }

    public string? LimiteSpecTexte { get; init; }
    public string? MoyenTexteLibre { get; init; }
    public string? ColonnesSupplementaires { get; init; }
}

public record RestaurerModeleRequestDto(
    Guid ModeleArchiveId, 
    string RestaurePar,
    string MotifRestoration
);


