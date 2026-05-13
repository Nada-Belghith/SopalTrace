using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanPfEntete
{
    public Guid Id { get; set; }

    public string? FamilleProduitFiniCode { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public string? Remarques { get; set; }

    public string? LegendeMoyens { get; set; }

    public virtual FamilleProduitFini? FamilleProduitFiniCodeNavigation { get; set; }

    public virtual ICollection<PlanPfLigne> PlanPfLignes { get; set; } = new List<PlanPfLigne>();

    public virtual ICollection<PlanPfSection> PlanPfSections { get; set; } = new List<PlanPfSection>();
}
