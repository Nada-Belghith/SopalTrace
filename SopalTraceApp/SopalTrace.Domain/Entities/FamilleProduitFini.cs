using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class FamilleProduitFini
{
    public string Code { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public string? TypeRobinetCode { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<PlanAssEntete> PlanAssEntetes { get; set; } = new List<PlanAssEntete>();

    public virtual ICollection<PlanPfEntete> PlanPfEntetes { get; set; } = new List<PlanPfEntete>();

    public virtual ICollection<ProduitFini> ProduitFinis { get; set; } = new List<ProduitFini>();

    public virtual TypeRobinet? TypeRobinetCodeNavigation { get; set; }
}
