using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefMoyenDetection
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<DocumentVerifMachineEcheance> DocumentVerifMachineEcheances { get; set; } = new List<DocumentVerifMachineEcheance>();
}
