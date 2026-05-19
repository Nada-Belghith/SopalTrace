using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ProduitFini
{
    public string CodeArticle { get; set; } = null!;

    public string FamilleProduitFiniCode { get; set; } = null!;

    public string TypeRobinetCode { get; set; } = null!;

    public virtual Article CodeArticleNavigation { get; set; } = null!;

    public virtual FamilleProduitFini FamilleProduitFiniCodeNavigation { get; set; } = null!;

    public virtual TypeRobinet TypeRobinetCodeNavigation { get; set; } = null!;
}
