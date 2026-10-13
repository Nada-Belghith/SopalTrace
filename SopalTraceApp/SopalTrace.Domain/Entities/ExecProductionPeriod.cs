using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecProductionPeriod
{
    public Guid Id { get; set; }

    public Guid ExecControleOfId { get; set; }

    public Guid? SessionId { get; set; }

    public string? PosteCode { get; set; }

    public Guid? SectionId { get; set; }

    public DateTime StartedAt { get; set; }

    public double? IntervalMinutes { get; set; }

    public int PlannedControlCount { get; set; }

    public DateTime? EndedAt { get; set; }

    public string? EndReason { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual ICollection<ExecPrelevementIntermediaire> ExecPrelevementIntermediaires { get; set; } = new List<ExecPrelevementIntermediaire>();

    public virtual PosteTravail? PosteCodeNavigation { get; set; }

    public virtual DocumentSection? Section { get; set; }

    public virtual ExecControleOfSession? Session { get; set; }
}
