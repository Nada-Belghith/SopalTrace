using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class TypeCaracteristique
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<ModeleFabricationLigne> ModeleFabricationLignes { get; set; } = new List<ModeleFabricationLigne>();

    public virtual ICollection<OutilControle> OutilControles { get; set; } = new List<OutilControle>();

    public virtual ICollection<PlanAssemblageLigne> PlanAssemblageLignes { get; set; } = new List<PlanAssemblageLigne>();

    public virtual ICollection<PlanFabricationLigne> PlanFabricationLignes { get; set; } = new List<PlanFabricationLigne>();

    public virtual ICollection<PlanProduitFiniLigne> PlanProduitFiniLignes { get; set; } = new List<PlanProduitFiniLigne>();
}
