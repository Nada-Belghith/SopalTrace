using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanResultatControleCfSection
{
    public Guid Id { get; set; }

    public Guid PlanRccfenteteId { get; set; }

    public string SectionType { get; set; } = null!;

    public string LibelleAffiche { get; set; } = null!;

    public int OrdreAffiche { get; set; }

    public virtual PlanResultatControleCfEntete PlanRccfentete { get; set; } = null!;

    public virtual ICollection<PlanResultatControleCfLigne> PlanResultatControleCfLignes { get; set; } = new List<PlanResultatControleCfLigne>();
}
