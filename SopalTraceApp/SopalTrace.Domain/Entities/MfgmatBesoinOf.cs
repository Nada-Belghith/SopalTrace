using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class MfgmatBesoinOf
{
    public int Id { get; set; }

    public string NumeroOf { get; set; } = null!;

    public string CodeArticle { get; set; } = null!;

    public decimal QuantiteRequise { get; set; }

    public decimal QuantiteSortie { get; set; }

    public virtual Article CodeArticleNavigation { get; set; } = null!;

    public virtual MfgheadOrdreFabrication NumeroOfNavigation { get; set; } = null!;
}
