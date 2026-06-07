using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanResultatControleCfLigne
{
    public Guid Id { get; set; }

    public Guid SectionId { get; set; }

    public string Caracteristique { get; set; } = null!;

    public string? LimiteSpecTexte { get; set; }

    public Guid? TypeControleId { get; set; }

    public Guid? MoyenControleId { get; set; }

    public string? InstrumentCode { get; set; }

    public string? Observations { get; set; }

    public int OrdreAffiche { get; set; }

    public virtual Instrument? InstrumentCodeNavigation { get; set; }

    public virtual MoyenControle? MoyenControle { get; set; }

    public virtual PlanResultatControleCfSection Section { get; set; } = null!;

    public virtual TypeControle? TypeControle { get; set; }
}
