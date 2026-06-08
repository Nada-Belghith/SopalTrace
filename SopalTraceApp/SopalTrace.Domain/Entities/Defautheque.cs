using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Defautheque
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<PlanAssemblageLigne> PlanAssemblageLignes { get; set; } = new List<PlanAssemblageLigne>();

    public virtual ICollection<PlanProduitFiniLigne> PlanProduitFiniLignes { get; set; } = new List<PlanProduitFiniLigne>();
}
