using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PeriodiciteMachine
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public int OrdreAffichage { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<DocumentVerifMachineEcheance> DocumentVerifMachineEcheances { get; set; } = new List<DocumentVerifMachineEcheance>();
}
