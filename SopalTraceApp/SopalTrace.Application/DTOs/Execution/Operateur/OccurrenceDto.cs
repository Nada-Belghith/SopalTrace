using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class OccurrenceDto
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public string TrancheHoraire { get; set; } = string.Empty;
    public int NumeroOccurrence { get; set; }
    public string TitreCombine { get; set; } = string.Empty;
    public DateTime HeureNotifPrevue { get; set; }
    public DateTime? HeureSimulee { get; set; }
    public bool EstEnRetard { get; set; }
    public string? Resultat { get; set; }
    
    public List<Guid> AssociatedIds { get; set; } = new();
    
    public List<CaracteristiqueARepondreDto> Caracteristiques { get; set; } = new();
}

public class CaracteristiqueExtraColonneDto
{
    public string CleColonne { get; set; } = string.Empty;
    public string? LabelAffiche { get; set; }
    public string? ValeurColonne { get; set; }
}

public class CaracteristiqueARepondreDto
{
    public Guid SectionId { get; set; }
    public string SectionLibelle { get; set; } = "";
    public Guid LignePlanId { get; set; }
    public string Libelle { get; set; } = null!;
    public string? LimiteSpecTexte { get; set; }
    public string? Observations { get; set; }
    public string TypeControle { get; set; } = null!; // "Mesure", "Visuel", etc.
    public string? MoyenControle { get; set; }
    public string? Instrument { get; set; }
    public string? ImageBase64 { get; set; }
    public List<CaracteristiqueExtraColonneDto> ExtraColonnes { get; set; } = new();
}

public class TrancheAlertesDto
{
    public string TrancheHoraire { get; set; } = null!;
    public string? ResultatFinal { get; set; }
    public string? Remarques { get; set; }
    public string? DetailsNc { get; set; }
    public List<OccurrenceDto> Occurrences { get; set; } = new();
}

public class RepondreOccurrenceRequest
{
    [Required, RegularExpression("^(C|NC|REGLAGE|IGNORE)$", ErrorMessage = "Résultat invalide.")]
    public string Resultat { get; set; } = null!;

    /// <summary>Renseigné côté serveur à partir du jeton JWT, jamais lu depuis la requête.</summary>
    [JsonIgnore]
    public string? MatriculeOperateur { get; set; }

    /// <summary>Motif d'ignorance ou de pause.</summary>
    [StringLength(500)]
    public string? Raison { get; set; }

    [MaxLength(500)]
    public List<RepondreOccurrenceLigneDto> Lignes { get; set; } = new();

    [MaxLength(500)]
    public List<Guid>? AssociatedIds { get; set; }
}

public class RepondreOccurrenceLigneDto
{
    public Guid LignePlanId { get; set; }

    [Required, StringLength(20)]
    public string Resultat { get; set; } = null!;

    public double? ValeurMesuree { get; set; }

    /// <summary>Détail de la non-conformité.</summary>
    [StringLength(500)]
    public string? Remarque { get; set; }

    [StringLength(500)]
    public string? ActionCorrective { get; set; }
}
