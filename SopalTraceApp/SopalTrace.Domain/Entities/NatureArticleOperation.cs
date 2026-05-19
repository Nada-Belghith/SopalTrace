using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class NatureArticleOperation
{
    public string NatureArticleCode { get; set; } = null!;

    public string OperationCode { get; set; } = null!;

    public int OrdreGamme { get; set; }

    public bool EstObligatoire { get; set; }

    public virtual NatureArticle NatureArticleCodeNavigation { get; set; } = null!;

    public virtual Operation OperationCodeNavigation { get; set; } = null!;
}
