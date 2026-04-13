using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanNcColonne
{
    public Guid Id { get; set; }

    public Guid PlanNcenteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string MachineCode { get; set; } = null!;

    public string LibelleDefaut { get; set; } = null!;

    public virtual Machine MachineCodeNavigation { get; set; } = null!;

    public virtual PlanNcEntete PlanNcentete { get; set; } = null!;
}
