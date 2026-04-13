using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Mfghead
{
    public string NumeroOf { get; set; } = null!;

    public string CodeArticle { get; set; } = null!;

    public double? QuantitePrevue { get; set; }

    public string? StatutOf { get; set; }

    public virtual Itmmaster CodeArticleNavigation { get; set; } = null!;

    public virtual ICollection<Mfgmat> Mfgmats { get; set; } = new List<Mfgmat>();
}
