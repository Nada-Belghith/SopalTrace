using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanAssSection
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid? TypeSectionId { get; set; }

    public Guid? PeriodiciteId { get; set; }

    public string LibelleSection { get; set; } = null!;

    public string? NormeReference { get; set; }

    public int? NqaId { get; set; }

    public string? Notes { get; set; }

    public virtual Nqa? Nqa { get; set; }

    public virtual Periodicite? Periodicite { get; set; }

    public virtual ICollection<PlanAssLigne> PlanAssLignes { get; set; } = new List<PlanAssLigne>();

    public virtual PlanAssEntete PlanEntete { get; set; } = null!;

    public virtual TypeSection? TypeSection { get; set; }
}
