using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Nqa
{
    public int Id { get; set; }

    public double ValeurNqa { get; set; }

    public virtual ICollection<PlanAssemblageSection> PlanAssemblageSections { get; set; } = new List<PlanAssemblageSection>();

    public virtual ICollection<PlanEchantillonnageEntete> PlanEchantillonnageEntetes { get; set; } = new List<PlanEchantillonnageEntete>();
}
