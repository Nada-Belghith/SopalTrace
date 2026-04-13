using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class GroupeInstrument
{
    public Guid Id { get; set; }

    public string CodeAlias { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<GroupeInstrumentDetail> GroupeInstrumentDetails { get; set; } = new List<GroupeInstrumentDetail>();

    public virtual ICollection<ModeleFabLigne> ModeleFabLignes { get; set; } = new List<ModeleFabLigne>();

    public virtual ICollection<PlanAssLigne> PlanAssLignes { get; set; } = new List<PlanAssLigne>();

    public virtual ICollection<PlanFabLigne> PlanFabLignes { get; set; } = new List<PlanFabLigne>();
}
