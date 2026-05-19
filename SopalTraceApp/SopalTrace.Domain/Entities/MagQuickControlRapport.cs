using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class MagQuickControlRapport
{
    public Guid Id { get; set; }

    public string NumeroOf { get; set; } = null!;

    public string CodeArticle { get; set; } = null!;

    public string NumeroRapportQc { get; set; } = null!;

    public DateTime? DateScan { get; set; }

    public virtual Article CodeArticleNavigation { get; set; } = null!;

    public virtual MfgheadOrdreFabrication NumeroOfNavigation { get; set; } = null!;
}
