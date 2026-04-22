using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanPfLigne
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public Guid SectionId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid? TypeCaracteristiqueId { get; set; }

    public string? LibelleAffiche { get; set; }

    public Guid? TypeControleId { get; set; }

    public Guid? MoyenControleId { get; set; }

    public string? InstrumentCode { get; set; }

    public string? MoyenTexteLibre { get; set; }

    public double? ToleranceSuperieure { get; set; }

    public double? ToleranceInferieure { get; set; }

    public string? LimiteSpecTexte { get; set; }

    public Guid? DefauthequeId { get; set; }

    public string? Instruction { get; set; }

    public string? Observations { get; set; }

    public virtual Defautheque? Defautheque { get; set; }

    public virtual Instrument? InstrumentCodeNavigation { get; set; }

    public virtual MoyenControle? MoyenControle { get; set; }

    public virtual PlanPfEntete PlanEntete { get; set; } = null!;

    public virtual PlanPfSection Section { get; set; } = null!;

    public virtual TypeCaracteristique? TypeCaracteristique { get; set; }

    public virtual TypeControle? TypeControle { get; set; }
}
