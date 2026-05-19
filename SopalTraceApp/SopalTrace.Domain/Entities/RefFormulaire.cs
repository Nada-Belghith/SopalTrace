using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefFormulaire
{
    public Guid Id { get; set; }

    public string CodeReference { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public int Version { get; set; }

    public bool Statut { get; set; }

    public DateTime CreeLe { get; set; }

    public virtual ICollection<ModeleFabEntete> ModeleFabEntetes { get; set; } = new List<ModeleFabEntete>();

    public virtual ICollection<PlanAssEntete> PlanAssEntetes { get; set; } = new List<PlanAssEntete>();

    public virtual ICollection<PlanFabEntete> PlanFabEntetes { get; set; } = new List<PlanFabEntete>();
}
