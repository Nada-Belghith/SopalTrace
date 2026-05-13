using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Itmmaster
{
    public string CodeArticle { get; set; } = null!;

    public string? Designation { get; set; }

    public string? Designation2 { get; set; }

    public string? FamilleProduitFini { get; set; }

    public string? FamilleCorpsCode { get; set; }

    public string? Statut { get; set; }

    public string? TypeRobinetCode { get; set; }

    public string? NatureComposantCode { get; set; }

    public virtual RefFamilleCorp? FamilleCorpsCodeNavigation { get; set; }

    public virtual FamilleProduitFini? FamilleProduitFiniNavigation { get; set; }

    public virtual ICollection<Mfghead> Mfgheads { get; set; } = new List<Mfghead>();

    public virtual ICollection<Mfgmat> Mfgmats { get; set; } = new List<Mfgmat>();

    public virtual NatureComposant? NatureComposantCodeNavigation { get; set; }

    public virtual TypeRobinet? TypeRobinetCodeNavigation { get; set; }
}
