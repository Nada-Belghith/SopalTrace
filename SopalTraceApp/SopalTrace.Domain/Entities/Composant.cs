using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Composant
{
    public string CodeArticle { get; set; } = null!;

    public virtual Article CodeArticleNavigation { get; set; } = null!;
}
