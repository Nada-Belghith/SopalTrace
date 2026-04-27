using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class TypeRobinet
{
    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<Itmmaster> Itmmasters { get; set; } = new List<Itmmaster>();

    public virtual ICollection<Machine> Machines { get; set; } = new List<Machine>();

    public virtual ICollection<ModeleFabEntete> ModeleFabEntetes { get; set; } = new List<ModeleFabEntete>();

    public virtual ICollection<PlanAssEntete> PlanAssEntetes { get; set; } = new List<PlanAssEntete>();

    public virtual ICollection<PlanPfEntete> PlanPfEntetes { get; set; } = new List<PlanPfEntete>();
}
