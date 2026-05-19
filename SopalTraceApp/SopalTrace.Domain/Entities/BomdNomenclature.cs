using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class BomdNomenclature
{
    public string ArticleParent { get; set; } = null!;

    public string CodeComposant { get; set; } = null!;

    public int CodeAlternative { get; set; }

    public double QuantiteRequise { get; set; }

    public virtual Article ArticleParentNavigation { get; set; } = null!;

    public virtual Article CodeComposantNavigation { get; set; } = null!;
}
