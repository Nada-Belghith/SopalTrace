using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PosteTravail
{
    public string CodePoste { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<PlanNcEntete> PlanNcEntetes { get; set; } = new List<PlanNcEntete>();

    public virtual ICollection<RefFormulaire> RefFormulaires { get; set; } = new List<RefFormulaire>();

    public virtual ICollection<Machine> CodeMachines { get; set; } = new List<Machine>();
}
