using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class SamplingPlan
{
    public Guid DocumentId { get; set; }

    public string InspectionLevelCode { get; set; } = null!;

    public string SamplingType { get; set; } = null!;

    public string InspectionSeverity { get; set; } = null!;

    public int AcceptableQualityLevelId { get; set; }

    public int AcceptanceNumber { get; set; }

    public int RejectionNumber { get; set; }

    public virtual Nqa AcceptableQualityLevel { get; set; } = null!;

    public virtual DocumentEntete Document { get; set; } = null!;

    public virtual InspectionLevel InspectionLevelCodeNavigation { get; set; } = null!;
}
