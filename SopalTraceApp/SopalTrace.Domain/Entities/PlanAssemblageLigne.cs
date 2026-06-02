using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanAssemblageLigne
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public Guid SectionId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid? TypeCaracteristiqueId { get; set; }

    public string? LibelleAffiche { get; set; }

    public Guid? TypeControleId { get; set; }

    public string? MachineCode { get; set; }

    public string? InstrumentCode { get; set; }

    public Guid? PeriodiciteId { get; set; }

    public string? LimiteSpecTexte { get; set; }

    public bool EstVerifPresence { get; set; }

    public Guid? DefauthequeId { get; set; }

    public string? RefPlanProduit { get; set; }

    public string? Instruction { get; set; }

    public string? Observations { get; set; }

    public bool EstCritique { get; set; }

    public Guid? MoyenControleId { get; set; }

    public string? MoyenTexteLibre { get; set; }

    public string? ColonnesSupplementaires { get; set; }

    public string? ImageBase64 { get; set; }

    public virtual Defautheque? Defautheque { get; set; }

    public virtual Instrument? InstrumentCodeNavigation { get; set; }

    public virtual Machine? MachineCodeNavigation { get; set; }

    public virtual MoyenControle? MoyenControle { get; set; }

    public virtual Periodicite? Periodicite { get; set; }

    public virtual PlanAssemblageEntete PlanEntete { get; set; } = null!;

    public virtual PlanAssemblageSection Section { get; set; } = null!;

    public virtual TypeCaracteristique? TypeCaracteristique { get; set; }

    public virtual TypeControle? TypeControle { get; set; }
}
