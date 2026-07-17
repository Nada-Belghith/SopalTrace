using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecRcPosteHeure
{
    public Guid Id { get; set; }

    public Guid LigneBilanId { get; set; }

    public string TrancheHoraire { get; set; } = null!;

    public double NbDefauts { get; set; }

    public virtual ExecRcPosteLigneBilan LigneBilan { get; set; } = null!;
}
