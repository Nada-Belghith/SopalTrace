using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PosteTravail
{
    public string CodePoste { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<ExecControleOf> ExecControleOfPosteCodeNavigations { get; set; } = new List<ExecControleOf>();

    public virtual ICollection<ExecControleOf> ExecControleOfPosteCodePrevuNavigations { get; set; } = new List<ExecControleOf>();

    public virtual ICollection<Machine> CodeMachines { get; set; } = new List<Machine>();
}
