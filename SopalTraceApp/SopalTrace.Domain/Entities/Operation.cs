using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Operation
{
    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public int OrdreProcess { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<Machine> Machines { get; set; } = new List<Machine>();

    public virtual ICollection<ModeleFabEntete> ModeleFabEntetes { get; set; } = new List<ModeleFabEntete>();

    public virtual ICollection<NatureComposantOperation> NatureComposantOperations { get; set; } = new List<NatureComposantOperation>();

    public virtual ICollection<PlanAssEntete> PlanAssEntetes { get; set; } = new List<PlanAssEntete>();

    public virtual ICollection<PlanNcEntete> PlanNcEntetes { get; set; } = new List<PlanNcEntete>();

    public virtual ICollection<RefFormulaire> RefFormulaires { get; set; } = new List<RefFormulaire>();
}
