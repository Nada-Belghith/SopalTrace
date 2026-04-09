using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ModeleFabLigne
{
    public Guid Id { get; set; }

    public Guid ModeleEnteteId { get; set; }

    public Guid SectionId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid? OutilSourceId { get; set; }

    public Guid TypeCaracteristiqueId { get; set; }

    public string? LibelleAffiche { get; set; }

    public Guid TypeControleId { get; set; }

    public Guid? MoyenControleId { get; set; }

    public Guid? GroupeInstrumentId { get; set; }

    public Guid? PeriodiciteId { get; set; }

    public string? Instruction { get; set; }

    public bool EstCritique { get; set; }

    public virtual GroupeInstrument? GroupeInstrument { get; set; }

    public virtual ModeleFabEntete ModeleEntete { get; set; } = null!;

    public virtual MoyenControle? MoyenControle { get; set; }

    public virtual OutilControle? OutilSource { get; set; }

    public virtual Periodicite? Periodicite { get; set; }

    public virtual ICollection<PlanFabLigne> PlanFabLignes { get; set; } = new List<PlanFabLigne>();

    public virtual ModeleFabSection Section { get; set; } = null!;

    public virtual TypeCaracteristique TypeCaracteristique { get; set; } = null!;

    public virtual TypeControle TypeControle { get; set; } = null!;
}
