using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecSessionPost
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public Guid ExecControleOfId { get; set; }

    public string PosteCode { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public string? StartedBy { get; set; }

    public DateTime? ClosingRequestedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public string? EndedBy { get; set; }

    public virtual Autili? EndedByNavigation { get; set; }

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual PosteTravail PosteCodeNavigation { get; set; } = null!;

    public virtual ExecControleOfSession Session { get; set; } = null!;

    public virtual Autili? StartedByNavigation { get; set; }
}
