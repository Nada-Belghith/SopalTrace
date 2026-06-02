using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class MfgheadOrdreFabrication
{
    public string NumeroOf { get; set; } = null!;

    public string CodeArticle { get; set; } = null!;

    public double QuantitePrevue { get; set; }

    public double QuantiteLancee { get; set; }

    public double QuantiteReelle { get; set; }

    public string StatutOf { get; set; } = null!;

    public DateTime? DateDebut { get; set; }

    public DateTime? DateFin { get; set; }

    public virtual Article CodeArticleNavigation { get; set; } = null!;

    public virtual ICollection<ExecControleOf> ExecControleOfs { get; set; } = new List<ExecControleOf>();

    public virtual ICollection<MagExpeditionBlScanOf> MagExpeditionBlScanOfs { get; set; } = new List<MagExpeditionBlScanOf>();

    public virtual ICollection<MagPreparationOf> MagPreparationOfs { get; set; } = new List<MagPreparationOf>();

    public virtual ICollection<MagQuickControlRapport> MagQuickControlRapports { get; set; } = new List<MagQuickControlRapport>();

    public virtual ICollection<MfgmatBesoinOf> MfgmatBesoinOfs { get; set; } = new List<MfgmatBesoinOf>();
}
