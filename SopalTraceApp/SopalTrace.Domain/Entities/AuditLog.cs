using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class AuditLog
{
    public long Id { get; set; }

    public string TableName { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string Changes { get; set; } = null!;

    public string? UserMatricule { get; set; }

    public DateTime OccurredAt { get; set; }

    public virtual Autili? UserMatriculeNavigation { get; set; }
}
