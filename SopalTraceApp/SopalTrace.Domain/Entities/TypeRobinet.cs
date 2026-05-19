using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class TypeRobinet
{
    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<FamilleProduitFini> FamilleProduitFinis { get; set; } = new List<FamilleProduitFini>();

    public virtual ICollection<ProduitFini> ProduitFinis { get; set; } = new List<ProduitFini>();
}
