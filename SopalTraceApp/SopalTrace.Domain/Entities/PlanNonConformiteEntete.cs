using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanNonConformiteEntete
{
    public Guid Id { get; set; }

    public string PosteCode { get; set; } = null!;

    public string Nom { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public virtual ICollection<PlanNonConformiteLigne> PlanNonConformiteLignes { get; set; } = new List<PlanNonConformiteLigne>();

    public virtual PosteTravail PosteCodeNavigation { get; set; } = null!;
}
