using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class FamilleProduitFini
{
    public string Code { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public string? TypeRobinetCode { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<Itmmaster> Itmmasters { get; set; } = new List<Itmmaster>();

    public virtual ICollection<ModeleFabEntete> ModeleFabEntetes { get; set; } = new List<ModeleFabEntete>();

    public virtual ICollection<PlanAssEntete> PlanAssEntetes { get; set; } = new List<PlanAssEntete>();

    public virtual ICollection<PlanFabEntete> PlanFabEntetes { get; set; } = new List<PlanFabEntete>();

    public virtual PlanPfEntete? PlanPfEntete { get; set; }

    public virtual TypeRobinet? TypeRobinetCodeNavigation { get; set; }
}
