using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Nqa
{
    public int Id { get; set; }

    public decimal ValeurNqa { get; set; }

    public virtual ICollection<DocumentSection> DocumentSections { get; set; } = new List<DocumentSection>();

    public virtual ICollection<SamplingPlan> SamplingPlans { get; set; } = new List<SamplingPlan>();
}
