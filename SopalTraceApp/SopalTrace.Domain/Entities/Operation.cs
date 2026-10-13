using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Operation
{
    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public bool AvecMachine { get; set; }

    public bool AvecCotesTronconnage { get; set; }

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<ExecControleOf> ExecControleOfs { get; set; } = new List<ExecControleOf>();

    public virtual ICollection<Machine> Machines { get; set; } = new List<Machine>();

    public virtual ICollection<NatureArticleOperation> NatureArticleOperations { get; set; } = new List<NatureArticleOperation>();
}
