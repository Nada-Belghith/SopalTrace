using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanPfSection
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid? TypeSectionId { get; set; }

    public string LibelleSection { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual PlanPfEntete PlanEntete { get; set; } = null!;

    public virtual ICollection<PlanPfLigne> PlanPfLignes { get; set; } = new List<PlanPfLigne>();

    public virtual TypeSection? TypeSection { get; set; }
}
