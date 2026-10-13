using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ColumnValueType
{
    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    public bool Active { get; set; }

    public virtual ICollection<RefFormulaireColonneDef> RefFormulaireColonneDefs { get; set; } = new List<RefFormulaireColonneDef>();
}
