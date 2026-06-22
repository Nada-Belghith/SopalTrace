using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class TypeControle
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<DocumentLigne> DocumentLignes { get; set; } = new List<DocumentLigne>();

    public virtual ICollection<ModeleFabricationLigne> ModeleFabricationLignes { get; set; } = new List<ModeleFabricationLigne>();

    public virtual ICollection<OutilControle> OutilControles { get; set; } = new List<OutilControle>();

    public virtual ICollection<PlanFabricationLigne> PlanFabricationLignes { get; set; } = new List<PlanFabricationLigne>();
}
