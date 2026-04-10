using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanAssLigne
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public Guid SectionId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid TypeCaracteristiqueId { get; set; }

    public string LibelleAffiche { get; set; } = null!;

    public Guid TypeControleId { get; set; }

    public Guid? MoyenControleId { get; set; }

    public Guid? GroupeInstrumentId { get; set; }

    public string? MachineCode { get; set; }

    public double? ValeurNominale { get; set; }

    public double? ToleranceSuperieure { get; set; }

    public double? ToleranceInferieure { get; set; }

    public string? Unite { get; set; }

    public string? LimiteSpecTexte { get; set; }

    public string? Instruction { get; set; }

    public string? Observations { get; set; }

    public bool EstCritique { get; set; }

    public virtual GroupeInstrument? GroupeInstrument { get; set; }

    public virtual Machine? MachineCodeNavigation { get; set; }

    public virtual MoyenControle? MoyenControle { get; set; }

    public virtual PlanAssEntete PlanEntete { get; set; } = null!;

    public virtual PlanAssSection Section { get; set; } = null!;

    public virtual TypeCaracteristique TypeCaracteristique { get; set; } = null!;

    public virtual TypeControle TypeControle { get; set; } = null!;
}
