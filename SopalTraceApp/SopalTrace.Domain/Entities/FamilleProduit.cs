using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class FamilleProduit
{
    public string Code { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public string? TypeRobinetCode { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<Itmmaster> Itmmasters { get; set; } = new List<Itmmaster>();

    public virtual ICollection<PlanAssemblageEntete> PlanAssemblageEntetes { get; set; } = new List<PlanAssemblageEntete>();

    public virtual TypeRobinet? TypeRobinetCodeNavigation { get; set; }
}
