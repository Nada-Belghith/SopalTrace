using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class BomdNomenclature
{
    public string ArticleParent { get; set; } = null!;

    public string CodeComposant { get; set; } = null!;

    public int CodeAlternative { get; set; }

    public decimal QuantiteRequise { get; set; }

    public string ParentOrigin { get; set; } = null!;

    public virtual Article Article { get; set; } = null!;

    public virtual Article CodeComposantNavigation { get; set; } = null!;
}
