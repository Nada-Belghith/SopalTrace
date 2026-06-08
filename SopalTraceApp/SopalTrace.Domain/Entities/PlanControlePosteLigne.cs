using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanControlePosteLigne
{
    public Guid Id { get; set; }

    public Guid ControlePosteEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string MachineCode { get; set; } = null!;

    public Guid RisqueDefautId { get; set; }

    public virtual PlanControlePosteEntete ControlePosteEntete { get; set; } = null!;

    public virtual Machine MachineCodeNavigation { get; set; } = null!;

    public virtual RisqueDefaut RisqueDefaut { get; set; } = null!;
}
