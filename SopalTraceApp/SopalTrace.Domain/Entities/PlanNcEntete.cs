using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanNcEntete
{
    public Guid Id { get; set; }

    public string PosteCode { get; set; } = null!;

    public string Nom { get; set; } = null!;

    public int? Version { get; set; }

    public string? Statut { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime? CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public virtual ICollection<PlanNcLigne> PlanNcLignes { get; set; } = new List<PlanNcLigne>();

    public virtual PosteTravail PosteCodeNavigation { get; set; } = null!;
}
