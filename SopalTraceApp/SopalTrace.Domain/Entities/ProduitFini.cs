using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ProduitFini
{
    public string CodeArticle { get; set; } = null!;

    public string FamilleProduitFiniCode { get; set; } = null!;

    public string ArticleType { get; set; } = null!;

    public virtual Article Article { get; set; } = null!;

    public virtual FamilleProduitFini FamilleProduitFiniCodeNavigation { get; set; } = null!;
}
