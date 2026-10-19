using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ReportTemplateAssignment
{
    public long Id { get; set; }

    public Guid ReportTemplateVersionId { get; set; }

    public string Kind { get; set; } = null!;

    public Guid? FormId { get; set; }

    public long CreationOrder { get; set; }

    public string? Comment { get; set; }

    public string? AssignedBy { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Autili? AssignedByNavigation { get; set; }

    public virtual RefFormulaire? Form { get; set; }

    public virtual ReportTemplateVersion ReportTemplateVersion { get; set; } = null!;
}
