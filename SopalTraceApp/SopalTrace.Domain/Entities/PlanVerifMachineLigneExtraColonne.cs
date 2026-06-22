using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanVerifMachineLigneExtraColonne
{
    public Guid Id { get; set; }

    public Guid LigneId { get; set; }

    public string CleColonne { get; set; } = null!;

    public string? ValeurColonne { get; set; }

    public int OrdreAffiche { get; set; }

    public virtual PlanVerifMachineLigne Ligne { get; set; } = null!;
}
