using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class OutilControle
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public Guid TypeControleId { get; set; }

    public Guid? TypeCaracteristiqueId { get; set; }

    public Guid? MoyenControleId { get; set; }

    public Guid? GroupeInstrumentId { get; set; }

    public Guid? PeriodiciteDefautId { get; set; }

    public string? LimiteSpecTexteDefaut { get; set; }

    public string? InstructionDefaut { get; set; }

    public bool Actif { get; set; }

    public virtual GroupeInstrument? GroupeInstrument { get; set; }

    public virtual ICollection<ModeleFabLigne> ModeleFabLignes { get; set; } = new List<ModeleFabLigne>();

    public virtual MoyenControle? MoyenControle { get; set; }

    public virtual Periodicite? PeriodiciteDefaut { get; set; }

    public virtual ICollection<PlanFabLigne> PlanFabLignes { get; set; } = new List<PlanFabLigne>();

    public virtual TypeCaracteristique? TypeCaracteristique { get; set; }

    public virtual TypeControle TypeControle { get; set; } = null!;
}
