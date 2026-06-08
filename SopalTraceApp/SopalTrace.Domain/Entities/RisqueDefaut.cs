using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RisqueDefaut
{
    public Guid Id { get; set; }

    public string CodeDefaut { get; set; } = null!;

    public string LibelleDefaut { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<PlanControlePosteLigne> PlanControlePosteLignes { get; set; } = new List<PlanControlePosteLigne>();
}
