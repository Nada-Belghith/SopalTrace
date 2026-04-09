using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Mfgmat
{
    public int Id { get; set; }

    public string NumeroOf { get; set; } = null!;

    public string CodeArticle { get; set; } = null!;

    public double QuantiteRequise { get; set; }

    public double QuantiteSortie { get; set; }

    public virtual Itmmaster CodeArticleNavigation { get; set; } = null!;

    public virtual Mfghead NumeroOfNavigation { get; set; } = null!;
}
