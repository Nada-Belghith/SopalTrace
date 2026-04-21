using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ModeleFabLigne
{
    public Guid Id { get; set; }

    public Guid ModeleEnteteId { get; set; }

    public Guid SectionId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid? TypeCaracteristiqueId { get; set; }

    public string? LibelleAffiche { get; set; }

    public double? ValeurNominale { get; set; }

    public double? ToleranceSuperieure { get; set; }

    public double? ToleranceInferieure { get; set; }

    public string? LimiteSpecTexte { get; set; }

    public string? Unite { get; set; }

    public Guid? TypeControleId { get; set; }

    public Guid? MoyenControleId { get; set; }

    public string? MoyenTexteLibre { get; set; }

    public string? InstrumentCode { get; set; }

    public Guid? PeriodiciteId { get; set; }

    public string? Instruction { get; set; }

    public bool EstCritique { get; set; }

    public virtual Instrument? InstrumentCodeNavigation { get; set; }

    public virtual ModeleFabEntete ModeleEntete { get; set; } = null!;

    public virtual MoyenControle? MoyenControle { get; set; }

    public virtual Periodicite? Periodicite { get; set; }

    public virtual ICollection<PlanFabLigne> PlanFabLignes { get; set; } = new List<PlanFabLigne>();

    public virtual ModeleFabSection Section { get; set; } = null!;

    public virtual TypeCaracteristique? TypeCaracteristique { get; set; }

    public virtual TypeControle? TypeControle { get; set; }
}
