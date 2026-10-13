using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ColumnAllowedValue
{
    public Guid Id { get; set; }

    public Guid ColumnDefinitionId { get; set; }

    public string Value { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool Active { get; set; }

    public virtual RefFormulaireColonneDef ColumnDefinition { get; set; } = null!;
}
