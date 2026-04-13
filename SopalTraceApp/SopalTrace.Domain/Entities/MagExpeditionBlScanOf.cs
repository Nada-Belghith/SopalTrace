using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class MagExpeditionBlScanOf
{
    public Guid Id { get; set; }

    public Guid ExpeditionBlid { get; set; }

    public string NumeroOfscanne { get; set; } = null!;

    public DateTime? DateScan { get; set; }

    public virtual MagExpeditionBl ExpeditionBl { get; set; } = null!;
}
