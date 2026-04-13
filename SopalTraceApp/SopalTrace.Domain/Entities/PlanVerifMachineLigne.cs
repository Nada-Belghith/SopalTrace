using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanVerifMachineLigne
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string LibelleRisque { get; set; } = null!;

    public Guid? RisqueDefautId { get; set; }

    public string? LibelleMethode { get; set; }

    public string TypeSaisie { get; set; } = null!;

    public double? ValeurNominale { get; set; }

    public double? ToleranceSup { get; set; }

    public double? ToleranceInf { get; set; }

    public string? Unite { get; set; }

    public bool EstCritique { get; set; }

    public string? Instruction { get; set; }

    public virtual PlanVerifMachineEntete PlanEntete { get; set; } = null!;

    public virtual ICollection<PlanVerifMachineEcheance> PlanVerifMachineEcheances { get; set; } = new List<PlanVerifMachineEcheance>();

    public virtual RisqueDefaut? RisqueDefaut { get; set; }
}
