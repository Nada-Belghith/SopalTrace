using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanNcLigne
{
    public Guid Id { get; set; }

    public Guid PlanNcenteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string MachineCode { get; set; } = null!;

    public Guid RisqueDefautId { get; set; }

    public virtual Machine MachineCodeNavigation { get; set; } = null!;

    public virtual PlanNcEntete PlanNcentete { get; set; } = null!;

    public virtual RisqueDefaut RisqueDefaut { get; set; } = null!;
}
