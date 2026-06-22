using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Operation
{
    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public int OrdreProcess { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<ExecControleOf> ExecControleOfs { get; set; } = new List<ExecControleOf>();

    public virtual ICollection<Machine> Machines { get; set; } = new List<Machine>();

    public virtual ICollection<ModeleFabricationEntete> ModeleFabricationEntetes { get; set; } = new List<ModeleFabricationEntete>();

    public virtual ICollection<NatureArticleOperation> NatureArticleOperations { get; set; } = new List<NatureArticleOperation>();

    public virtual ICollection<PlanFabricationEntete> PlanFabricationEntetes { get; set; } = new List<PlanFabricationEntete>();
}
