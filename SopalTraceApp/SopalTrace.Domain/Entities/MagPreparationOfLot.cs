using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class MagPreparationOfLot
{
    public Guid Id { get; set; }

    public Guid PreparationOfid { get; set; }

    public string CodeComposant { get; set; } = null!;

    public string NumeroLotScanne { get; set; } = null!;

    public double Quantite { get; set; }

    public DateTime? DateScan { get; set; }

    public virtual MagPreparationOf PreparationOf { get; set; } = null!;
}
