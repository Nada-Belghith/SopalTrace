using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class FamilleProduitFini
{
    public string Code { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public string? TypeRobinetCode { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<ModeleFabricationEntete> ModeleFabricationEntetes { get; set; } = new List<ModeleFabricationEntete>();

    public virtual ICollection<PlanAssemblageEntete> PlanAssemblageEntetes { get; set; } = new List<PlanAssemblageEntete>();

    public virtual ICollection<PlanProduitFiniEntete> PlanProduitFiniEntetes { get; set; } = new List<PlanProduitFiniEntete>();

    public virtual ICollection<ProduitFini> ProduitFinis { get; set; } = new List<ProduitFini>();

    public virtual TypeRobinet? TypeRobinetCodeNavigation { get; set; }
}
