using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class TypeSection
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<ModeleFabSection> ModeleFabSections { get; set; } = new List<ModeleFabSection>();

    public virtual ICollection<PlanAssSection> PlanAssSections { get; set; } = new List<PlanAssSection>();

    public virtual ICollection<PlanFabSection> PlanFabSections { get; set; } = new List<PlanFabSection>();

    public virtual ICollection<PlanPfSection> PlanPfSections { get; set; } = new List<PlanPfSection>();
}
