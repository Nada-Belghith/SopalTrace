using System;
using System.Collections.Generic;

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
    public List<OccurrenceDto> Occurrences { get; set; } = new();
}

public class RepondreOccurrenceRequest
{
    public string Resultat { get; set; } = null!; // "C", "NC", ou "REGLAGE"
    public string? MatriculeOperateur { get; set; } // Opérateur qui a fait le contrôle
    public string? Raison { get; set; } // Motif d'ignorance ou de pause
    public List<RepondreOccurrenceLigneDto> Lignes { get; set; } = new();
    public List<Guid>? AssociatedIds { get; set; }
}

public class IgnorerOccurrencesRequest
{
    public List<Guid> OccurrenceIds { get; set; } = new();
    public string MatriculeOperateur { get; set; } = null!;
    public string Raison { get; set; } = null!;
}

public class RepondreOccurrenceLigneDto
{
    public Guid LignePlanId { get; set; }
    public string Resultat { get; set; } = null!; // "C" ou "NC"
    public double? ValeurMesuree { get; set; }
    public string? Remarque { get; set; } // Utilisé pour les détails de Non-Conformité
    public string? ActionCorrective { get; set; }
}
