using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefFamilleCorp
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<PlanVerifMachineFamille> PlanVerifMachineFamilles { get; set; } = new List<PlanVerifMachineFamille>();
}
