using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class FamilleProduitFini
{
    public string Code { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public string TypeRobinetCode { get; set; } = null!;

    public bool Actif { get; set; }

    public bool PlanParPoste { get; set; }

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<ProduitFini> ProduitFinis { get; set; } = new List<ProduitFini>();

    public virtual ICollection<RefFormulaire> RefFormulaires { get; set; } = new List<RefFormulaire>();

    public virtual TypeRobinet TypeRobinetCodeNavigation { get; set; } = null!;
}
