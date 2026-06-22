using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanFabricationLigne
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public Guid SectionId { get; set; }

    public Guid? ModeleLigneSourceId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid? CaracteristiqueId { get; set; }

    public Guid? TypeCaracteristiqueId { get; set; }

    public string? LibelleAffiche { get; set; }

    public Guid? TypeControleId { get; set; }

    public string? InstrumentCode { get; set; }

    public Guid? PeriodiciteId { get; set; }

    public string? LimiteSpecTexte { get; set; }

    public string? Observations { get; set; }

    public string? Instruction { get; set; }

    public bool EstCritique { get; set; }

    public Guid? MoyenControleId { get; set; }

    public string? MoyenTexteLibre { get; set; }

    public string? ImageBase64 { get; set; }

    public string? Libre1 { get; set; }

    public string? Libre2 { get; set; }

    public string? Libre3 { get; set; }

    public string? Libre4 { get; set; }

    public string? Libre5 { get; set; }

    public virtual RefCaracteristique? Caracteristique { get; set; }

    public virtual Instrument? InstrumentCodeNavigation { get; set; }

    public virtual ModeleFabricationLigne? ModeleLigneSource { get; set; }

    public virtual MoyenControle? MoyenControle { get; set; }

    public virtual Periodicite? Periodicite { get; set; }

    public virtual PlanFabricationEntete PlanEntete { get; set; } = null!;

    public virtual PlanFabricationSection Section { get; set; } = null!;

    public virtual TypeCaracteristique? TypeCaracteristique { get; set; }

    public virtual TypeControle? TypeControle { get; set; }
}
