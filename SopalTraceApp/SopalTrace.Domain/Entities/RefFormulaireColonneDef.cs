using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefFormulaireColonneDef
{
    public Guid Id { get; set; }

    public string CodeReference { get; set; } = null!;

    public string CleColonne { get; set; } = null!;

    public string LabelAffiche { get; set; } = null!;

    public string TypeValeur { get; set; } = null!;

    public string? InsertAfter { get; set; }

    public string? TargetTable { get; set; }

    public bool Actif { get; set; }
}
