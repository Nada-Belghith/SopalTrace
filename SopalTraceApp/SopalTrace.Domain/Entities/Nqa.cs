using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Nqa
{
    public int Id { get; set; }

    public double ValeurNqa { get; set; }

    public virtual ICollection<PlanAssSection> PlanAssSections { get; set; } = new List<PlanAssSection>();

    public virtual ICollection<PlanEchantillonnageEntete> PlanEchantillonnageEntetes { get; set; } = new List<PlanEchantillonnageEntete>();

    public virtual ICollection<PlanPfSection> PlanPfSections { get; set; } = new List<PlanPfSection>();
}
