using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanVerifMachineLigne
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string? TypeLigne { get; set; }

    public string LibelleRisque { get; set; } = null!;

    public string? LibelleMethode { get; set; }

    public virtual PlanVerifMachineEntete PlanEntete { get; set; } = null!;

    public virtual ICollection<PlanVerifMachineEcheance> PlanVerifMachineEcheances { get; set; } = new List<PlanVerifMachineEcheance>();
}
