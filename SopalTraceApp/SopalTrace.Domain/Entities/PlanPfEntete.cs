using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanPfEntete
{
    public Guid Id { get; set; }

    public string TypeRobinetCode { get; set; } = null!;

    public string? CodeArticleSage { get; set; }

    public string? Designation { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public bool VisibleOperateur { get; set; }

    public DateOnly? DateApplication { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public string? Remarques { get; set; }

    public string? LegendeMoyens { get; set; }

    public virtual ICollection<PlanPfLigne> PlanPfLignes { get; set; } = new List<PlanPfLigne>();

    public virtual ICollection<PlanPfSection> PlanPfSections { get; set; } = new List<PlanPfSection>();

    public virtual TypeRobinet TypeRobinetCodeNavigation { get; set; } = null!;
}
