using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class InspectionLevel
{
    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual ICollection<RefIso2859LettresCode> RefIso2859LettresCodes { get; set; } = new List<RefIso2859LettresCode>();

    public virtual ICollection<SamplingPlan> SamplingPlans { get; set; } = new List<SamplingPlan>();
}
