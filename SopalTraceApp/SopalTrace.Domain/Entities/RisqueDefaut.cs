using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RisqueDefaut
{
    public Guid Id { get; set; }

    public string CodeDefaut { get; set; } = null!;

    public string LibelleDefaut { get; set; } = null!;

    public Guid? TypeControleId { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<PlanVerifMachineLigne> PlanVerifMachineLignes { get; set; } = new List<PlanVerifMachineLigne>();

    public virtual TypeControle? TypeControle { get; set; }
}
