using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanPfEntete
{
    public Guid Id { get; set; }

    public string FamilleProduitFiniCode { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public virtual FamilleProduitFini FamilleProduitFiniCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanPfLigne> PlanPfLignes { get; set; } = new List<PlanPfLigne>();

    public virtual ICollection<PlanPfSection> PlanPfSections { get; set; } = new List<PlanPfSection>();
}
