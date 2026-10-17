using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class FormCodeChange
{
    public long Id { get; set; }

    public Guid PreviousFormId { get; set; }

    public Guid NewFormId { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual Autili? ChangedByNavigation { get; set; }

    public virtual RefFormulaire NewForm { get; set; } = null!;

    public virtual RefFormulaire PreviousForm { get; set; } = null!;
}
